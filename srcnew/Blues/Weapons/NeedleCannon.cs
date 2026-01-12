using System;

namespace MMXOnline;

public class NeedleCannon : Weapon {
	public static NeedleCannon netWeapon = new();
	public float bloomLevel = 4;
	public float bloomCooldown = 0;
	public float bloomSpeed = 0;

	public NeedleCannon() : base() {
		// Tecnical data.
		index = (int)BluesWeaponIds.NeedleCannon;
		fireRate = 6;
		defaultAmmoUse = 0.35f;

		// Display data.
		displayName = "NEEDLE CANNON";
		descriptionV2 = [
			[ "Rapid fire cannon that deals fast damage\nbut has high heat generation." ],
		];

		// Auto-calculation for ammo per second text.
		decimal ammoUseDec = Decimal.Parse(defaultAmmoUse.ToString());
		decimal chps = ammoUseDec * (60m / (decimal)fireRate);
		string chpsString = chps.ToString("#.####");

		// Ammo use text.
		ammoUseText = chpsString + " per second";
	}

	public override void charLinkedUpdate(Character character, bool isAlwaysOn) {
		base.charLinkedUpdate(character, isAlwaysOn);

		if (shootCooldown <= 0) {
			if (bloomCooldown <= 0 && bloomLevel > 4) {
				bloomLevel -= bloomSpeed;
				bloomSpeed++;
				if (bloomLevel < 4) {
					bloomLevel = 4;
				}
			}
			Helpers.decrementFrames(ref bloomCooldown);
		}
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Blues blues = character as Blues ?? throw new NullReferenceException();
		Point shootPos = blues.getShootPos();
		int xDir = blues.getShootXDir();
		Player player = blues.player;
		float shootAngle = 0;

		shootAngle = Helpers.randomRange(-bloomLevel, bloomLevel);
		bloomLevel += 1.5f;
		bloomCooldown = 4;
		bloomSpeed = 1;
		if (bloomLevel > 24) {
			bloomLevel = 24;
		}
		if (xDir == -1) {
			shootAngle = -shootAngle + 128;
		}

		new NeedleCannonProj(blues, shootPos, shootAngle, player.getNextActorNetId(), true) {
			ownerActor = blues
		};
		blues.playSound("bassbuster");
		blues.xPushVel = -xDir;
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
		maxTime = 18 / 60f;
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
