using System;
using System.Collections.Generic;

namespace MMXOnline;

public class GyroAttack : Weapon {
	public static GyroAttack netWeapon = new();

	public GyroAttack() : base() {
		displayName = "GYRO ATTACK";
		descriptionV2 = [
			[ "A propeller weapon that can\nbe aimed in 2 diferent directions." ],
		];
		defaultAmmoUse = 3;

		index = (int)BluesWeaponIds.GyroAttack;
		fireRate = 35;
	}

	public override float getAmmoUsage(int chargeLevel) {
		return defaultAmmoUse;
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Blues blues = character as Blues ?? throw new NullReferenceException();
		Point shootPos = blues.getShootPos();
		int xDir = blues.getShootXDir();
		Player player = blues.player;

		new GyroAttackProj(blues, shootPos, xDir, !blues.grounded, player.getNextActorNetId(), true, player);
		blues.playSound("buster2");
	}
}

public class GyroAttackProj : Projectile {
	int defaultDir = -1;
	bool changedDir;
	const float projSpeed = 180;

	public GyroAttackProj(
		Actor owner, Point pos, int xDir, bool air, ushort? netId, 
		bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "gyro_attack_proj", netId, altPlayer
	) {
		maxTime = 1;
		projId = (int)BluesProjIds.GyroAttack;
		fadeOnAutoDestroy = true;
		canBeLocal = false;

		vel.x = projSpeed * xDir;
		damager.damage = 2;
		damager.flinch = Global.miniFlinch;

		if (air) {
			defaultDir = 1;
		}

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)(air ? 1 : 0));
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new GyroAttackProj(
			args.owner, args.pos, args.xDir, args.extraData[0] == 1, args.netId, altPlayer: args.player
		);
	}

	public override void update() {
		base.update();
		int iyDir = ownerPlayer.input.getYDir(ownerPlayer);

		if (iyDir != 0 && !changedDir || time >= 35/60f && !changedDir) {
			if (iyDir == 0) {
				iyDir = defaultDir;
			}
			if (iyDir == 1) {
				vel = new Point(0, projSpeed);
			} else {
				vel = new Point(0, -projSpeed);
			}
			time = 0;
			maxTime = 25 / 60f;

			changedDir = true;
		}
	}

	public override void onDamageEX(IDamagable damagable) {
		base.onDamageEX(damagable);
		if (!ownedByLocalPlayer) return;
		new Anim(pos, "rock_buster_fade", xDir, damager.owner.getNextActorNetId(), true, true);
	}

	public override void onDestroy() {
		base.onDestroy();
		Anim.createGibEffect("gyro_attack_pieces", pos, null!, zIndex: zIndex);
	}
}
