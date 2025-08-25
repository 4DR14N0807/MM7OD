using System;
using System.Collections.Generic;

namespace MMXOnline;

public class RockBuster : Weapon {
	public static RockBuster netWeapon = new();

	public List<RockBusterProj> lemonsOnField = new List<RockBusterProj>();

	public RockBuster() : base() {
		displayName = "MEGA BUSTER";
		index = (int)RockWeaponIds.MegaBuster;
		killFeedIndex = 0;
		weaponBarBaseIndex = (int)RockWeaponBarIds.MegaBuster;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = (int)RockWeaponSlotIds.MegaBuster;
		//shootSounds = new List<string>() {"buster", "buster2", "buster3", ""};
		fireRate = 9;
		drawAmmo = false;
		descriptionV2 = [
			[ "Rock's default weapon.\n" + "Can be charged to deal more damage." ]
		];
		
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
		return lemonsOnField.Count < 3;
	}

	public override void shootRock(Rock rock, params int[] args) {
		base.shootRock(rock, args);
		Point shootPos = rock.getShootPos();
		int xDir = rock.getShootXDir();
		Player player = rock.player;
		ushort netId = player.getNextActorNetId();
		int chargeLevel = args[0];

		if (chargeLevel >= 2) {
			if (rock.grounded && rock.charState is not Run) {
				rock.changeState(new RockChargeShotState(rock.grounded), true);
			} else {
				new RockBusterChargedProj(rock, shootPos, xDir, player.getNextActorNetId(), 0, true);
				rock.playSound("buster3", sendRpc: true);
			} 
		} else if (chargeLevel == 1) {
			new RockBusterMidChargeProj(rock, shootPos, xDir, player.getNextActorNetId(), 0, true);
			rock.playSound("buster2", sendRpc: true);
		} else {
			var proj = new RockBusterProj(rock, shootPos, xDir, netId, true);
			lemonsOnField.Add(proj);
			rock.lemons++;
			rock.playSound("buster", sendRpc: true);

			rock.timeSinceLastShoot = 0;
			rock.lemonTime += 20f * rock.lemons;
			if (rock.lemonTime >= 60f) {
				rock.lemonTime = 0;
				rock.weaponCooldown = 30;
			}
		}
	}
}


public class RockBusterProj : Projectile {

	public RockBusterProj(
		Actor owner, Point pos, int xDir, ushort? netProjId,
		bool rpc = false, Player? altPlayer = null
	) : base(
			pos, xDir, owner, "rock_buster_proj", netProjId, altPlayer
	) {

		projId = (int)RockProjIds.RockBuster;
		maxTime = 0.6f;
		fadeSprite = "rock_buster_fade";

		damager.damage = 1;
		vel.x = 240 * xDir;
		reflectable = true;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new RockBusterProj(
			arg.owner, arg.pos, arg.xDir, arg.netId, altPlayer: arg.player
		);
	}
}

public class RockBusterMidChargeProj : Projectile {

	public int type;
	float projSpeed = 300;
	Actor ownChr = null!;

	public RockBusterMidChargeProj(
		Actor owner, Point pos, int xDir, ushort? netProjId,
		int type, bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "rock_buster1_start", netProjId, altPlayer
	) {

		projId = (int)RockProjIds.RockBusterMid;
		maxTime = 0.5f;
		fadeSprite = "rock_buster1_fade";
		damager.damage = 2;
		fadeOnAutoDestroy = true;
		ownChr = owner;

		if (type == 1) {
			changeSprite("rock_buster1_proj", false);
			reflectable = true;
			vel.x = projSpeed * xDir;
		}
		this.type = type;

		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type };

			rpcCreate(pos, owner, ownerPlayer, netProjId, xDir, extraArgs);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new RockBusterMidChargeProj(
			arg.owner, arg.pos, arg.xDir, arg.netId, 
			arg.extraData[0], altPlayer: arg.player
		);
	}

	public override void update() {
		base.update();

		if (!ownedByLocalPlayer) return;

		if (type == 0 && isAnimOver()) {
			time = 0;
			new RockBusterMidChargeProj(ownChr, pos, xDir, damager.owner.getNextActorNetId(), 1, true);
			destroySelfNoEffect();
		}
	}
}

public class RockBusterChargedProj : Projectile {

	public int type;
	float projSpeed = 340;
	Actor ownChr = null!;

	public RockBusterChargedProj(
		Actor owner, Point pos, int xDir, ushort? netProjId,
		int type, bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "rock_buster1_start", netProjId, altPlayer
	) {

		projId = (int)RockProjIds.RockBusterCharged;
		maxTime = 0.48f;
		fadeOnAutoDestroy = true;
		fadeSprite = "rock_buster2_fade";
		this.type = type;
		damager.damage = 3;
		damager.flinch = Global.halfFlinch;
		ownChr = owner;

		if (type == 1) {
			string? sprite = "rock_buster2_proj";
			changeSprite(sprite, false);
			reflectable = true;
			vel.x = projSpeed * xDir;
		}

		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type };

			rpcCreate(pos, owner, ownerPlayer, netProjId, xDir, extraArgs);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new RockBusterChargedProj(
			arg.owner, arg.pos, arg.xDir, arg.netId, 
			arg.extraData[0], altPlayer: arg.player
		);
	}

	public override void update() {
		base.update();

		if (!ownedByLocalPlayer) return;

		if (type == 0 && isAnimOver()) {
			time = 0;
			new RockBusterChargedProj(ownChr, pos, xDir, damager.owner.getNextActorNetId(true), 1, rpc: true);
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
			new RockBusterChargedProj(
				character, character.getShootPos(), character.getShootXDir(), 
				player.getNextActorNetId(), 0, true
			);
			character.playSound("buster3", sendRpc: true);
		} else if (!fired) {

			fired = true;
			new RockBusterChargedProj(
				character, character.getShootPos(), character.getShootXDir(), 
				player.getNextActorNetId(), 0, true
			);
			character.playSound("buster3", sendRpc: true);
		}


		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}
}
