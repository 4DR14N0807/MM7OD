using System;
using System.Collections.Generic;

namespace MMXOnline;

public class FreezeCracker : Weapon {

	public static FreezeCracker netWeapon = new FreezeCracker();

	public FreezeCracker() : base() {
		index = (int)RockWeaponIds.FreezeCracker;
		killFeedIndex = 0;
		weaponBarBaseIndex = (int)RockWeaponBarIds.FreezeCracker;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = (int)RockWeaponSlotIds.FreezeCracker;
		//shootSounds = new List<string>() {"buster2", "buster2", "buster2", ""};
		rateOfFire = 0.75f;
		maxAmmo = 16;
		ammo = maxAmmo;
		description = new string[] { "Splits in 6 pieces when hitting a wall.", "Can be aimed up or down." };
	}

	public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId) {
		base.getProjectile(pos, xDir, player, chargeLevel, netProjId);

		if (player.character.ownedByLocalPlayer) {
			/*if (chargeLevel >= 2 && player.hasBusterLoadout()) {
                player.character.changeState(new RockChargeShotState(player.character.grounded), true);
            }
            else */
			new FreezeCrackerProj(this, pos, xDir, player, 0, netProjId);
			player.character.playSound("buster2", sendRpc: true);
		}
	}


	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Point shootPos = character.getShootPos();
		int xDir = character.getShootXDir();

		new FreezeCrackerProj(this, shootPos, xDir, character.player, 0, character.player.getNextActorNetId(), true);
		character.playSound("buster2", sendRpc: true);
	}
}


public class FreezeCrackerProj : Projectile {

	public int type;
	public float sparkleTime = 0;
	Anim? sparkle;
	float projSpeed = 300;


	public FreezeCrackerProj(
		Weapon weapon, Point pos, int xDir,
		Player player, int type, ushort netProjId,
		bool rpc = false
	) : base(
		weapon, pos, xDir, 0, 2,
		player, "freeze_cracker_start", 0, 0.1f,
		netProjId, player.ownedByLocalPlayer
	) {

		projId = (int)RockProjIds.FreezeCracker;
		maxTime = 0.6f;
		//fadeOnAutoDestroy = true;
		fadeSprite = "freeze_cracker_start";
		this.type = type;
		canBeLocal = false;
		float xSpeed = projSpeed * Helpers.cosd(45) * xDir;
		float ySpeed = projSpeed * Helpers.sind(45);

		if (type == 1) {

			changeSprite("freeze_cracker_proj", false);
			reflectable = true;

			if (player.input.isHeld(Control.Up, player)) {
				base.vel = new Point(xSpeed, -ySpeed);
			} else if (player.input.isHeld(Control.Down, player)) {
				base.vel = new Point(xSpeed, ySpeed);
			} else base.vel = new Point(projSpeed * xDir, 0);

		}

		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type };

			rpcCreate(pos, player, netProjId, xDir, extraArgs);
		}
	}


	public override void update() {
		base.update();

		sparkleTime += Global.spf;
		if (sparkleTime >= 0.06) {
			sparkleTime = 0;

			sparkle = new Anim(pos, "freeze_cracker_sparkles", 1, null, true);

		}

		if (sparkle != null) sparkle.vel.y += Global.spf * 1300;

		if (type == 0 && isAnimOver()) {
			time = 0;
			new FreezeCrackerProj(weapon, pos, xDir, damager.owner, 1, damager.owner.getNextActorNetId(true), rpc: true);
			destroySelfNoEffect();
		}
	}

	public void onHit() {
		if (!ownedByLocalPlayer) {
			destroySelf();
			//destroySelfNoEffect(disableRpc: true, true);
			return;
		}

		if (type == 1) {
			playSound("ding", true, true);
			destroySelf(disableRpc: true);

			for (int i = 0; i < 6; i++) {
				new FreezeCrackerPieceProj(weapon, pos, xDir, damager.owner, i, damager.owner.getNextActorNetId(true), rpc: true);
			}
		}
	}

	public override void onHitWall(CollideData other) {
		//base.onHitWall(other);
		//if (!ownedByLocalPlayer) return;
		if (!other.gameObject.collider.isClimbable) return;
		onHit();
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		destroySelf(disableRpc: true);
	}



	public static Projectile rpcInvoke(ProjParameters arg) {
		return new FreezeCrackerProj(
			FreezeCracker.netWeapon, arg.pos, arg.xDir, arg.player,
			arg.extraData[0], arg.netId
		);
	}
}


public class FreezeCrackerPieceProj : Projectile {


	public FreezeCrackerPieceProj(
		Weapon weapon, Point pos, int xDir,
		Player player, int type, ushort netProjId,
		bool rpc = false
	) : base(
		weapon, pos, xDir, 240, 2,
		player, "freeze_cracker_piece", 0, 0.25f,
		netProjId, player.ownedByLocalPlayer
	) {

		projId = (int)RockProjIds.FreezeCrackerPiece;
		maxTime = 0.6f;
		reflectable = true;
		canBeLocal = false;

		base.vel.x = Helpers.cosd(type * 60) * speed;
		base.vel.y = Helpers.sind(type * 60) * speed;

		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type };

			rpcCreate(pos, player, netProjId, xDir, extraArgs);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new FreezeCrackerPieceProj(
			FreezeCracker.netWeapon, arg.pos, arg.xDir, arg.player,
			arg.extraData[0], arg.netId
		);
	}
}
