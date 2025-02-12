using System;
using System.Collections.Generic;

namespace MMXOnline;

public class SparkShock : Weapon {
	public static SparkShock netWeapon = new();

	public SparkShock() : base() {
		displayName = "SPARK SHOCK";
		descriptionV2 = "Generates an energy ball that can" + "\n" +
						"temporarily paralyze enemies.";
		defaultAmmoUse = 3;

		index = (int)BluesWeaponIds.SparkShock;
		fireRate = 45;
	}

	public override float getAmmoUsage(int chargeLevel) {
		return defaultAmmoUse;
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Blues blues = character as Blues ?? throw new NullReferenceException();
		Point shootPos = blues.getShootPos();
		int xDir = blues.getShootXDir();
		new SparkShockProj(blues, shootPos, xDir, blues.player.getNextActorNetId(), true);
		blues.playSound("sparkShock", sendRpc: true);
	}
}

public class SparkShockProj : Projectile {
	public SparkShockProj(
		Actor owner, Point pos, int xDir, ushort? netId,
		bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "spark_shock_proj", netId, altPlayer
	) {
		//todo: improve sparkshock sprites
		maxTime = 0.75f;
		projId = (int)BluesProjIds.SparkShock;

		vel.x = 180 * xDir;
		damager.damage = 1;
		damager.flinch = Global.miniFlinch;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new SparkShockProj(
			args.owner, args.pos, args.xDir, args.netId, altPlayer: args.player
		);
	}
}
