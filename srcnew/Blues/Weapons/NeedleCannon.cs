using System;

namespace MMXOnline;

public class NeedleCannon : Weapon {
	public static NeedleCannon netWeapon = new();
	public float baseBloom = 4;
	public float bloomLevel = 0;
	public float maxBloom = 20;
	public float bloomCooldown = 0;
	public float bloomSpeed = 0;

	public NeedleCannon() : base() {
		// Tecnical data.
		index = (int)BluesWeaponIds.NeedleCannon;
		fireRate = 4;
		defaultAmmoUse = 0.2f;

		// Display data.
		displayName = "NEEDLE CANNON";
		descriptionV2 = [
			[
				"Rapid fire cannon that deals fast damage\n" +
				"but has high heat generation.\n" + 
				"Has less spread and more range the more is held."
			],
		];

		// Auto-calculation for ammo per second text.
		decimal ammoUseDec = decimal.Parse(defaultAmmoUse.ToString());
		decimal chps = ammoUseDec * (60m / (decimal)fireRate);
		decimal chps2 = ammoUseDec * (60m / 7m);
		string chpsString = chps.ToString("#.#");
		string chpsString2 = chps2.ToString("#.#");

		// Ammo use text.
		ammoUseText = $"{chpsString2}/{chpsString} per second";
	}

	public override void charLinkedUpdate(Character character, bool isAlwaysOn) {
		base.charLinkedUpdate(character, isAlwaysOn);

		if (shootCooldown <= 0 && bloomCooldown <= 0 && bloomLevel > 0) {
			bloomLevel -= bloomSpeed;
			bloomSpeed += 0.025f;
			if (bloomLevel < 0) {
				bloomLevel = 0;
			}
		}
		Helpers.decrementFrames(ref bloomCooldown);
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Blues blues = character as Blues ?? throw new NullReferenceException();
		Point shootPos = blues.getShootPos();
		int xDir = blues.getShootXDir();
		Player player = blues.player;
		int projType = 0;

		float spread = maxBloom - bloomLevel + baseBloom;
		int currentBloom = MathInt.Round(bloomLevel);
		float shootAngle = Helpers.randomRange(-spread, spread);
		bloomLevel += 0.75f;
		bloomCooldown = 15;
		bloomSpeed = 0.25f;

		if (bloomLevel > maxBloom) {
			bloomLevel = maxBloom;
		}
		if (xDir == -1) {
			shootAngle = -shootAngle + 128;
		}
		blues.playSound("bassbuster");
		blues.xPushVel = -xDir;
		if (blues.altLemonCooldown < 6) {
			blues.altLemonCooldown = 6;
		}
		fireRate = MathF.Round(7 - (bloomLevel / 20f) * 3);

		// Sprite effects.
		if (blues.sprite.name == blues.getSprite("shoot") || 
			blues.sprite.name == blues.getSprite("shoot_shield")
		) {
			blues.sprite.frameSpeed = 1 + (bloomLevel / 40f);
		}
		// Buster smoke effect.
		int randRange =  bloomLevel >= maxBloom - 4 ? 8 : 2;
		if (Helpers.randomRange(0, 10) <= randRange) {
			projType = 1;
		}
		if (Helpers.randomRange(0, 10) <= 4) {
			Anim tempAnim = new Anim(shootPos.addRand(2, 2), "dust", 1, null, true);
			tempAnim.vel.y = -Helpers.randomRange(90, 120);
			tempAnim.vel.x = -Helpers.randomRange(60, 60);
			if (projType != 0) {
				tempAnim.addRenderEffect(RenderEffectType.ChargeOrange, 3, 120, 5);
			}
		}

		// Create proj.
		new NeedleCannonProj(
			blues, shootPos, shootAngle,
			currentBloom, projType,
			player.getNextActorNetId(), sendRpc: true
		);
	}
	public override float getAmmoUsage(int chargeLevel) {
		return defaultAmmoUse;
	}
}

public class NeedleCannonProj : Projectile {
	public int type;

	public NeedleCannonProj(
		Actor owner, Point pos, float byteAngle, int bloom, int type,
		ushort? netId, bool sendRpc = false, Player? altPlayer = null
	) : base(
		pos, 1, owner, "needle_cannon_proj", netId, altPlayer
	) {
		byteAngle = MathF.Round(byteAngle);
		maxTime = (14 + MathF.Round(bloom / 2.5f)) / 60f;
		fadeSprite = "needle_cannon_proj_fade";
		projId = (int)BluesProjIds.NeedleCannon;
		this.byteAngle = byteAngle;
		vel = Point.createFromByteAngle(byteAngle) * 400;
		damager.damage = 0.5f;

		// Visual only orange-ish effect.
		if (type == 1) {
			addRenderEffect(RenderEffectType.ChargeOrange, 3, 120, 5);
			addRenderEffect(RenderEffectType.Trail, time: 120);
		}

		if (sendRpc) {
			rpcCreateByteAngle(pos, ownerPlayer, netId, byteAngle, (byte)bloom, (byte)type);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new NeedleCannonProj(
			args.owner, args.pos, args.byteAngle, args.extraData[0], args.extraData[1],
			args.netId, altPlayer: args.player
		);
	}
}
