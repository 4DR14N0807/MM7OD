using System;

namespace MMXOnline;

public class NeedleCannon : Weapon {
	public static NeedleCannon netWeapon = new();

	public NeedleCannon() : base() {
		// Tecnical data.
		index = (int)BluesWeaponIds.NeedleCannon;
		fireRate = 6;
		defaultAmmoUse = 0.65f;

		// Display data.
		displayName = "NEEDLE CANNON";
		descriptionV2 = "Rapid fire cannon that deals fast damage\nbut has high heat generation.";

		// Auto-calculation for ammo per second text.
		decimal ammoUseDec = Decimal.Parse(defaultAmmoUse.ToString());
		decimal chps = ammoUseDec * (60m / (decimal)fireRate);
		string chpsString = chps.ToString("#.#");

		// Ammo use text.
		ammoUseText = chpsString + " per second";
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Blues blues = character as Blues ?? throw new NullReferenceException();
		Point shootPos = blues.getShootPos();
		int xDir = blues.getShootXDir();
		Player player = blues.player;
		float shootAngle = 0;
		if (blues.grounded) {
			shootAngle = Helpers.randomRange(-30, 20);
		} else {
			shootAngle = Helpers.randomRange(-25, 25);
		}
		if (xDir == -1) {
			shootAngle = -shootAngle + 128;
		}

		new NeedleCannonProj(blues, shootPos, shootAngle, player.getNextActorNetId(), true) {
			ownerActor = blues
		};
		blues.playSound("buster");
		blues.xPushVel = 60 * -xDir;
	}

	public override float getAmmoUsage(int chargeLevel) {
		return defaultAmmoUse;
	}
}

public class NeedleCannonProj : Projectile {
	public NeedleCannonProj(
		Actor owner, Point pos, float byteAngle, ushort? netId, bool rpc = false, Player? altPlayer = null
	) : base(
		pos, 1, owner, "needle_cannon_proj", netId, altPlayer
	) {
		byteAngle = MathF.Round(byteAngle);
		maxTime = 0.25f;
		fadeSprite = "needle_cannon_proj_fade";
		projId = (int)BluesProjIds.NeedleCannon;
		this.byteAngle = byteAngle;
		vel = Point.createFromByteAngle(byteAngle) * 400;
		damager.damage = 0.5f;

		if (rpc) {
			rpcCreateByteAngle(pos, ownerPlayer, netId, byteAngle);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new NeedleCannonProj(
			args.owner, args.pos, args.byteAngle, args.netId, altPlayer: args.player
		);
	}
}
