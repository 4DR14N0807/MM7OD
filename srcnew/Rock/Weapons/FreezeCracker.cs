using System;
using System.Collections.Generic;

namespace MMXOnline;

public class FreezeCracker : Weapon {

	public FreezeCracker() : base() {
		index = (int)RockWeaponIds.FreezeCracker;
		killFeedIndex = 0;
		weaponBarBaseIndex = (int)RockWeaponBarIds.FreezeCracker;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = (int)RockWeaponSlotIds.FreezeCracker;
		fireRate = 45;
		maxAmmo = 20;
		ammo = maxAmmo;
		description = new string[] { "Splits in 6 pieces when hitting a wall.", "Can be aimed up or down." };
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Point shootPos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;
		int input = player.input.getYDir(player);

		new FreezeCrackerProj(shootPos, xDir, player, 0, player.getNextActorNetId(), input);
		character.playSound("buster2", sendRpc: true);
	}
}


public class FreezeCrackerProj : Projectile {

	public int type;
	int input;
	public float sparkleTime = 0;
	Anim? sparkle;
	float projSpeed = 300;


	public FreezeCrackerProj(
		Point pos, int xDir, Player player, int type, 
		ushort netProjId, int input = 0, bool rpc = false
	) : base(
		FreezeCracker.netWeapon, pos, xDir, 0, 2,
		player, "freeze_cracker_start", 0, 0.1f,
		netProjId, player.ownedByLocalPlayer
	) {

		projId = (int)RockProjIds.FreezeCracker;
		maxTime = 0.6f;
		fadeSprite = "freeze_cracker_start";
		this.type = type;
		this.input = input;

		if (type == 1) {

			canBeLocal = false;
			changeSprite("freeze_cracker_proj", false);
			reflectable = true;
			int dir = input * 32;
			float ang = xDir > 0 ? dir : -dir + 128;
			base.vel = Point.createFromByteAngle(ang) * projSpeed;
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

			if (ownedByLocalPlayer) sparkle = new Anim(pos, "freeze_cracker_sparkles", 1, damager.owner.getNextActorNetId(), true)
			{ useGravity = true, gravityModifier = 0.5f };

		}

		if (type == 0 && isAnimOver() && ownedByLocalPlayer) {
			time = 0;
			new FreezeCrackerProj(
				pos, xDir, damager.owner, 1, 
				damager.owner.getNextActorNetId(true), input, rpc: true);
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
			destroySelf();

			for (int i = 0; i < 6; i++) {
				new FreezeCrackerPieceProj(
					pos, xDir, damager.owner, i, damager.owner.getNextActorNetId(true), rpc: true);
			}
		}
	}

	public override void onHitWall(CollideData other) {
		//base.onHitWall(other);
		//if (!ownedByLocalPlayer) return;
		if (other.gameObject.collider == null) return;
		if (!other.gameObject.collider.isClimbable) return;
		onHit();
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		destroySelf();
	}



	public static Projectile rpcInvoke(ProjParameters arg) {
		return new FreezeCrackerProj(
			arg.pos, arg.xDir, arg.player, arg.extraData[0], arg.netId
		);
	}
}


public class FreezeCrackerPieceProj : Projectile {


	public FreezeCrackerPieceProj(
		Point pos, int xDir, Player player, 
		int type, ushort netProjId, bool rpc = false
	) : base(
		FreezeCracker.netWeapon, pos, xDir, 240, 2,
		player, "freeze_cracker_piece", 0, 0.25f,
		netProjId, player.ownedByLocalPlayer
	) {

		projId = (int)RockProjIds.FreezeCrackerPiece;
		maxTime = 0.6f;
		reflectable = true;
		//canBeLocal = false;

		base.vel = Point.createFromByteAngle(type * 42.5f) * speed;

		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type };

			rpcCreate(pos, player, netProjId, xDir, extraArgs);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new FreezeCrackerPieceProj(
			arg.pos, arg.xDir, arg.player, arg.extraData[0], arg.netId
		);
	}
}
