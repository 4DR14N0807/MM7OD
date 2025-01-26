using System;

namespace MMXOnline;

public class NeedleCannon : Weapon {
	public static NeedleCannon netWeapon = new();

	public NeedleCannon() : base() {
		// Tecnical data.
		index = (int)BluesWeaponIds.NeedleCannon;
		fireRate = 6;
		defaultAmmoUse = 1.2f;

		// Display data.
		displayName = "NEEDLE CANNON";
		descriptionV2 = "Rapid fire cannon that deals fast damage\nbut has high heat generation.";

		// Auto-calculation for ammo per second text.
		decimal ammoUseDec = Decimal.Parse(defaultAmmoUse.ToString());
		decimal chps = ammoUseDec * (60m / (decimal)fireRate);
		string chpsString = Math.Ceiling(chps).ToString("#");

		// Ammo use text.
		ammoUseText = chpsString + " per second";
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Point shootPos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;
		float shootAngle = 0;
		if (character.grounded) {
			shootAngle = Helpers.randomRange(-30, 20);
		} else {
			shootAngle = Helpers.randomRange(-25, 25);
		}
		if (xDir == -1) {
			shootAngle = -shootAngle + 128;
		}

		new NeedleCannonProj(shootPos, shootAngle, player, player.getNextActorNetId(), true) {
			owningActor = character
		};
		character.playSound("buster");
		character.xPushVel = 60 * -xDir;
	}

	public override float getAmmoUsage(int chargeLevel) {
		return defaultAmmoUse;
	}
}

public class NeedleCannonProj : Projectile {
	public NeedleCannonProj(
		Point pos, float byteAngle, Player player, ushort? netId, bool rpc = false
	) : base(
		NeedleCannon.netWeapon, pos, 1, 0, 0.5f, player, "needle_cannon_proj",
		0, 0, netId, player.ownedByLocalPlayer
	) {
		byteAngle = MathF.Round(byteAngle);
		maxTime = 0.25f;
		fadeSprite = "needle_cannon_proj_fade";
		projId = (int)BluesProjIds.NeedleCannon;
		this.byteAngle = byteAngle;
		vel = Point.createFromByteAngle(byteAngle) * 400;

		if (rpc) {
			rpcCreateByteAngle(pos, player, netId, byteAngle);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new NeedleCannonProj(
			args.pos, args.byteAngle, args.player, args.netId
		);
	}
}
