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
		fireRate = 9;
		description = new string[] { "Rock's default weapon.", "Can be charged to deal more damage." };
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 0;
	}

	public override bool canShoot(int chargeLevel, Player player) {
		if (!base.canShoot(chargeLevel, player)) return false;
		Rock? rock = player.character as Rock;
		if (chargeLevel > 1) {
			return true;
		}
		for (int i = lemonsOnField.Count - 1; i >= 0; i--) {
			if (lemonsOnField[i].destroyed) {
				lemonsOnField.RemoveAt(i);
				continue;
			}
		}
		return lemonsOnField.Count < 3 && rock?.weaponCooldown <= 0;
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Point shootPos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;
		int chargeLevel = args[0];

		if (player.character is Rock rock) {
				if (chargeLevel >= 2) {
					if (character.grounded) {
						character.changeState(new RockChargeShotState(character.grounded), true);
					} else new RockBusterChargedProj(shootPos, xDir, player, 0, player.getNextActorNetId(), true);
					character.playSound("buster3", sendRpc: true);
				} else if (chargeLevel == 1) {
					new RockBusterMidChargeProj(shootPos, xDir, player, 0, player.getNextActorNetId(), true);
					character.playSound("buster2", sendRpc: true);
				} else {
					var proj = new RockBusterProj(shootPos, xDir, player, player.getNextActorNetId(), true);
					lemonsOnField.Add(proj);
					rock.lemons++;
					character.playSound("buster", sendRpc: true);

					rock.timeSinceLastShoot = 0;
					rock.lemonTime += 20f * rock.lemons;
					if (rock.lemonTime >= 60f) {
						rock.lemonTime = 0;
						rock.weaponCooldown = 30;
					}
				}
			}
	}
}


public class RockBusterProj : Projectile {

	public RockBusterProj(
		Point pos, int xDir, Player player, 
		ushort netProjId, bool rpc = false
	) : base(
			RockBuster.netWeapon, pos, xDir, 240, 1,
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
			arg.pos, arg.xDir, arg.player, arg.netId
		);
	}
}

public class RockBusterMidChargeProj : Projectile {

	public int type;
	float projSpeed = 300;

	public RockBusterMidChargeProj(
		Point pos, int xDir, Player player, 
		int type, ushort netProjId, bool rpc = false
	) : base(
		RockBuster.netWeapon, pos, xDir, 0, 2,
		player, "rock_buster1_start", 0, 0,
		netProjId, player.ownedByLocalPlayer
	) {

		projId = (int)RockProjIds.RockBusterMid;
		maxTime = 0.5f;
		fadeOnAutoDestroy = true;
		fadeSprite = "rock_buster1_fade";

		if (type == 1) {
			changeSprite("rock_buster1_proj", false);
			reflectable = true;
			base.vel.x = projSpeed * xDir;
		}
		this.type = type;

		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type };

			rpcCreate(pos, player, netProjId, xDir, extraArgs);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new RockBusterMidChargeProj(
			arg.pos, arg.xDir, arg.player, arg.extraData[0], arg.netId
		);
	}

	public override void update() {
		base.update();

		if (!ownedByLocalPlayer) return;

		if (type == 0 && isAnimOver()) {
			time = 0;
			new RockBusterMidChargeProj(pos, xDir, damager.owner, 1, damager.owner.getNextActorNetId(true), rpc: true);
			destroySelfNoEffect();
		}
	}
}

public class RockBusterChargedProj : Projectile {

	public int type;
	float projSpeed = 340;

	public RockBusterChargedProj(
		Point pos, int xDir, Player player,
		int type, ushort netProjId, bool rpc = false
	) : base(
		RockBuster.netWeapon, pos, xDir, 0, 3,
		player, "rock_buster2_start", Global.halfFlinch, 0,
		netProjId, player.ownedByLocalPlayer
	) {

		projId = (int)RockProjIds.RockBusterCharged;
		maxTime = 0.48f;
		fadeOnAutoDestroy = true;
		fadeSprite = "rock_buster2_fade";
		this.type = type;

		if (type == 1) {
			string? sprite = "rock_buster2_proj";
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
			arg.pos, arg.xDir, arg.player, arg.extraData[0], arg.netId
		);
	}

	public override void update() {
		base.update();

		if (!ownedByLocalPlayer) return;

		if (type == 0 && isAnimOver()) {
			time = 0;
			new RockBusterChargedProj(pos, xDir, damager.owner, 1, damager.owner.getNextActorNetId(true), rpc: true);
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

		if (!fired && grounded) {

			fired = true;
			var poi = character.currentFrame.POIs;
			new RockBusterChargedProj(character.getShootPos(), character.getShootXDir(), player, 0, player.getNextActorNetId(), rpc: true);
		} else if (!fired) {

			fired = true;
			new RockBusterChargedProj(character.getShootPos(), character.getShootXDir(), player, 0, player.getNextActorNetId(), rpc: true);
		}


		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}
}
