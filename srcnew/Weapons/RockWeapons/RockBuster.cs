using System;
using System.Collections.Generic;

namespace MMXOnline;

public class RockBuster : Weapon {

	public List<RockBusterProj> lemonsOnField = new List<RockBusterProj>();
	public static RockBuster netWeapon = new RockBuster();

	public RockBuster() : base() {
		index = (int)RockWeaponIds.MegaBuster;
		killFeedIndex = 0;
		weaponBarBaseIndex = (int)RockWeaponBarIds.MegaBuster;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = (int)RockWeaponSlotIds.MegaBuster;
		//shootSounds = new List<string>() {"buster", "buster2", "buster3", ""};
		rateOfFire = 0.15f;
		description = new string[] { "Rock's default weapon.", "Can be charged to deal more damage." };
	}


	public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId) {
		if (player.character.ownedByLocalPlayer) {
			if (player.character is Rock rock) {

				if (chargeLevel >= 2) {
					if (player.character.grounded) {
						player.character.changeState(new RockChargeShotState(player.character.grounded), true);
					} else new RockBusterChargedProj(this, pos, xDir, player, 0, netProjId);
					player.character.playSound("buster3", sendRpc: true);
				} else if (chargeLevel == 1) {
					new RockBusterMidChargeProj(this, pos, xDir, player, 0, netProjId);
					player.character.playSound("buster2", sendRpc: true);
				} else {
					var proj = new RockBusterProj(this, pos, xDir, player, netProjId, true);
					lemonsOnField.Add(proj);
					rock.lemons++;
					player.character.playSound("buster", sendRpc: true);

					rock.lemonTime += 20f * rock.lemons / 60f;
					if (rock.lemonTime >= 1f) {
						rock.lemonTime = 0;
						rock.shootTime = 30f / 60f;
					}
				}
			}
		}
	}

	public override bool canShoot(int chargeLevel, Player player) {
		if (!base.canShoot(chargeLevel, player)) return false;
		if (chargeLevel > 1) {
			return true;
		}
		for (int i = lemonsOnField.Count - 1; i >= 0; i--) {
			if (lemonsOnField[i].destroyed) {
				lemonsOnField.RemoveAt(i);
				continue;
			}
		}
		return lemonsOnField.Count < 3;
	}
}


public class RockBusterProj : Projectile {

	public RockBusterProj(
		Weapon weapon, Point pos, int xDir,
		Player player, ushort netProjId,
		bool rpc = false
	) : base(
			weapon, pos, xDir, 240, 1,
			player, "rock_buster_proj", 0, 0,
			netProjId, player.ownedByLocalPlayer
	) {

		projId = (int)RockProjIds.RockBuster;
		maxTime = 0.6f;
		fadeSprite = "rock_buster_fade";
		reflectable = true;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new RockBusterProj(
			RockBuster.netWeapon, arg.pos, arg.xDir, arg.player,
			arg.netId
		);
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) {
			return;
		}
	}
}

public class RockBusterMidChargeProj : Projectile {

	public int type;
	float projSpeed = 300;

	public RockBusterMidChargeProj(
		Weapon weapon, Point pos, int xDir,
		Player player, int type, ushort netProjId,
		bool rpc = false
	) : base(
		weapon, pos, xDir, 0, 2,
		player, "rock_buster1_start", 0, 0,
		netProjId, player.ownedByLocalPlayer
	) {

		projId = (int)RockProjIds.RockBusterMid;
		maxTime = 0.66f;
		fadeOnAutoDestroy = true;
		fadeSprite = "rock_buster1_fade";

		if (type == 1) {
			changeSprite("rock_buster1_proj", false);
			reflectable = true;
			base.vel.x = projSpeed * xDir;
		}
		this.type = type;
		canBeLocal = true;

		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type };

			rpcCreate(pos, player, netProjId, xDir, extraArgs);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new RockBusterMidChargeProj(
			RockBuster.netWeapon, arg.pos, arg.xDir, arg.player,
			arg.extraData[0], arg.netId
		);
	}

	public override void update() {
		base.update();

		if (!ownedByLocalPlayer) return;

		if (type == 0 && isAnimOver()) {
			time = 0;
			new RockBusterMidChargeProj(weapon, pos, xDir, damager.owner, 1, damager.owner.getNextActorNetId(true), rpc: true);
			destroySelfNoEffect();
		}
	}
}

public class RockBusterChargedProj : Projectile {

	public int type;
	float projSpeed = 420;

	public RockBusterChargedProj(
		Weapon weapon, Point pos, int xDir,
		Player player, int type, ushort netProjId,
		bool rpc = false
	) : base(
		weapon, pos, xDir, 0, 3,
		player, "rock_buster2_start", Global.halfFlinch, 0,
		netProjId, player.ownedByLocalPlayer
	) {

		projId = (int)RockProjIds.RockBusterCharged;
		maxTime = 0.4f;
		fadeOnAutoDestroy = true;
		fadeSprite = "rock_buster2_fade";
		this.type = type;

		if (type == 1) {

			var sprite = "rock_buster2_proj";
			changeSprite(sprite, false);
			reflectable = true;
			base.vel.x = projSpeed * xDir;
		}

		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type };

			rpcCreate(pos, player, netProjId, xDir, extraArgs);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new RockBusterChargedProj(
			RockBuster.netWeapon, arg.pos, arg.xDir, arg.player,
			arg.extraData[0], arg.netId
		);
	}

	public override void update() {
		base.update();

		if (!ownedByLocalPlayer) return;

		if (type == 0 && isAnimOver()) {
			time = 0;
			new RockBusterChargedProj(weapon, pos, xDir, damager.owner, 1, damager.owner.getNextActorNetId(true), rpc: true);
			destroySelfNoEffect();
		}
	}
}


public class RockChargeShotState : CharState {

	bool fired;
	bool grounded;

	public RockChargeShotState(bool grounded) : base(grounded ? "chargeshot" : "", "", "", "") {
		this.grounded = grounded;
		normalCtrl = true;
		attackCtrl = true;
		airMove = true;
	}


	public override void update() {
		base.update();

		if (character.frameIndex == 0 && !fired && grounded) {

			fired = true;
			var poi = character.currentFrame.POIs;
			new RockBusterChargedProj(new RockBuster(), character.getShootPos(), character.getShootXDir(), player, 0, player.getNextActorNetId(), rpc: true);
		} else if (!fired) {

			fired = true;
			new RockBusterChargedProj(new RockBuster(), character.getShootPos(), character.getShootXDir(), player, 0, player.getNextActorNetId(), rpc: true);
		}


		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}
}
