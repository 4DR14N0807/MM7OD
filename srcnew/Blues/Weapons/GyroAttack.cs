using System;
using System.Collections.Generic;

namespace MMXOnline;

public class GyroAttack : Weapon {
	public static GyroAttack netWeapon = new();

	public GyroAttack() : base() {
		displayName = "GYRO ATTACK";
		descriptionV2 = "A propeller weapon that can\nbe aimed in 2 diferent directions.";
		defaultAmmoUse = 2;

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

		new GyroAttackProj(blues, shootPos, xDir, player.getNextActorNetId(), true, player);
	}
}

public class GyroAttackProj : Projectile {

	bool changedDir;
	const float projSpeed = 180;

	public GyroAttackProj(
		Actor owner, Point pos, int xDir,ushort? netId, 
		bool rpc = false, Player? altPlayer = null
	) :
	base(
		pos, xDir, owner, "gyro_attack_proj", netId, altPlayer
	) {
		maxTime = 0.625f;
		projId = (int)BluesProjIds.GyroAttack;
		netOwner = altPlayer;
		canBeLocal = false;

		vel.x = projSpeed * xDir;
		damager.damage = 2;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new GyroAttackProj(
			args.owner, args.pos, args.xDir, args.netId, altPlayer: args.player
		);
	}

	public override void update() {
		base.update();

		if (!changedDir && netOwner != null) {
			if (netOwner.input.isPressed(Control.Up, netOwner)) {
				base.vel = new Point(0, -projSpeed);
				time = 0;
				changedDir = true;
			} else if (netOwner.input.isPressed(Control.Down, netOwner)) {
				base.vel = new Point(0, projSpeed);
				time = 0;
				changedDir = true;
			}
		}
	}
	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		if (damagable is Actor enemyActor && enemyActor.netId is not null and >= Level.firstNormalNetId) {
			string keyName = enemyActor.GetType().ToString() + "_" + enemyActor.netId;
			//i think it can be optimised if we separate fade sprite from hit sprite
			new Anim(pos, "rock_buster_fade", xDir, netId, true, true);
		}
	}
	public override void onDestroy() {
		base.onDestroy();
		Anim.createGibEffect("gyro_attack_pieces", pos, null!, zIndex: zIndex);
	}
}
