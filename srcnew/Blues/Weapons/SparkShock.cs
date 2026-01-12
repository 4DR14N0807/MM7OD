using System;
using System.Collections.Generic;

namespace MMXOnline;

public class SparkShock : Weapon {
	public static SparkShock netWeapon = new();

	public SparkShock() : base() {
		displayName = "SPARK SHOCK";
		descriptionV2 = [
			[ "Generates an energy ball that can" + "\n" +
						"temporarily paralyze enemies." ],
		];
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
		maxTime = 45 / 60f;
		projId = (int)BluesProjIds.SparkShock;
		fadeSprite = "proto_chargeshot_yellow_proj_fade";
		fadeOnAutoDestroy = true;

		vel.x = (1 / 60f) * xDir;
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

	public override void update() {
		base.update();
		if (reflectCount == 0) {
			vel.x += Global.speedMul * xDir * 8;
		}
	}

	public override void onReflect() {
		vel.x = 200;
		base.onReflect();
	}

	public override void render(float x, float y) {
		float savedAlpha = alpha;
		long savedZindex = zIndex;
		zIndex = ZIndex.Background;			
		for (int i = 6; i >= 1; i--) {
			int j = (6 - i);
			alpha = j * 0.1f + 0.4f;
			xScale = j * 0.1f + 0.3f;
			yScale = j * 0.1f + 0.3f;
			base.render(x + (-moveDelta.x * i), y);
		}
		alpha = savedAlpha;
		zIndex = savedZindex;
		xScale = 1;
		yScale = 1;
		
		base.render(x, y);
	}
}
