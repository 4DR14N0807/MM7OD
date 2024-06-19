using System;
using System.Collections.Generic;

namespace MMXOnline;

public class HardKnuckle : Weapon {
	public static HardKnuckle netWeapon = new();

	public HardKnuckle() : base() {
		index = (int)RockWeaponIds.HardKnuckle;
		fireRateFrames = 40;
		hasCustomAnim = true;
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 4;
	}

	public override bool canShoot(int chargeLevel, Character character) {
		return ((character as Blues)?.hardKnuckleProj?.destroyed != false);
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Point shootPos = character.getShootPos();
		int xDir = character.getShootXDir();
		character.changeState(new HardKnuckleShoot(), true);
		character.playSound("super_adaptor_punch", sendRpc: true);
	}
}

public class HardKnuckleProj : Projectile {
	bool changedDir;
	bool deflected;
	float spawnPointX;
	bool canControl = true;

	Dictionary<string, float> bounceCooldowns = new();

	public HardKnuckleProj(
		Point pos, int xDir, Player player, ushort? netId, bool rpc = false
	) : base(
		HardKnuckle.netWeapon, pos, xDir, 0.25f * 60, 2, player, "hard_knuckle_proj",
		Global.halfFlinch, 1f, netId, player.ownedByLocalPlayer
	) {
		projId = (int)BluesProjIds.HardKnuckle;
		fadeSprite = "generic_explosion";
		fadeOnAutoDestroy = true;
		destroyOnHit = false;
		spawnPointX = pos.x;
	}

	public override void update() {
		base.update();
		// Max distance check.
		if (MathF.Abs(pos.x - spawnPointX) > 16 * 6) {
			destroySelf(disableRpc: true);
		}
		// Aceleration,
		float maxSpeed = 4f * 60;
		if (!deflected && vel.x * xDir < maxSpeed) {
			vel.x += Global.speedMul * xDir * (0.125f * 60f);
			if (vel.x * xDir >= maxSpeed) {
				vel.x = (float)xDir * maxSpeed;
			}
		}
		// Bounce cooldown timers.
		foreach ((string key, float val) in bounceCooldowns) {
			bounceCooldowns[key] = Helpers.clampMin0(bounceCooldowns[key]) - Global.speedMul;
		}
		// Local player ends here.
		if (!canControl || !owner.ownedByLocalPlayer) {
			return;
		}
		int inputYDir = owner.input.getYDir(owner);
		vel.y = 60 * inputYDir;
		if (inputYDir != 0) {
			forceNetUpdateNextFrame = true;
		}
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		if (damagable is Actor enemyActor && enemyActor.netId is not null or 0) {
			string keyName = enemyActor.GetType().ToString() + "_" + enemyActor.netId;
			if (bounceCooldowns.GetValueOrDefault(keyName) == 0) {
				bounceCooldowns[keyName] = 60;
				bounce();
			}
		}
	}

	public void bounce() {
		if (deflected) {
			return;
		}
		vel.x = xDir * (-2 * 60);
		if (canControl) {
			vel.y = 0;
		}
		canControl = false;
	}

	public override void onDeflect() {
		deflected = true;
		vel.x = xDir * 4 * 60;
		vel.y = 0;
		base.onDeflect();
	}
}

public class HardKnuckleShoot : CharState {
	bool fired;
	bool effectCreated;
	Blues blues = null!;

	public HardKnuckleShoot() : base("hardknuckle") {
		airSprite = "hardknuckle_air";
		landSprite = "hardknuckle";
	}

	public override void update() {
		base.update();
		if (!effectCreated) {
			new Anim(
				character.getShootPos().addxy((character.xDir * -6), 0),
				"generic_explosion", character.xDir, player.getNextActorNetId(), true,
				sendRpc: true, host: character, zIndex: ZIndex.Default + 1
			);
			effectCreated = true;
		}
		if (!fired && character.frameIndex == 1) {
			blues.hardKnuckleProj = new HardKnuckleProj(
				character.getShootPos(), character.xDir,
				player, player.getNextActorNetId(), true
			);
			fired = true;
		}
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		blues = character as Blues ?? throw new NullReferenceException();
		character.stopMovingWeak();
		if (!character.grounded) {
			character.changeSpriteFromName(airSprite, true);
			character.vel.y = -Physics.JumpSpeed * 0.6f;
			character.slideVel = -character.xDir * 2.5f * 60f;
		} else {
			character.slideVel = -character.xDir * 2f * 60f;
		}
	}
}
