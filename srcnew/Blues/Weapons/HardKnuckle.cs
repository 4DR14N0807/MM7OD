using System;
using System.Collections.Generic;

namespace MMXOnline;

public class HardKnuckle : Weapon {
	public static HardKnuckle netWeapon = new();

	public HardKnuckle() : base() {
		displayName = "HARD KNUCKLE";
		descriptionV2 = [
			[ "Fires out a large missile shaped like a fist,\n can damage more than one target." ],
		];
		defaultAmmoUse = 4;

		index = (int)BluesWeaponIds.HardKnuckle;
		fireRate = 45;
		hasCustomAnim = true;
	}

	public override float getAmmoUsage(int chargeLevel) {
		return defaultAmmoUse;
	}

	public override bool canShoot(int chargeLevel, Character character) {
		return ((character as Blues)?.hardKnuckleProj?.destroyed != false);
	}

	public override void shoot(Character character, params int[] args) {
		character.changeState(new HardKnuckleShoot(), true);
		character.playSound("super_adaptor_punch", sendRpc: true);
	}
}

public class HardKnuckleProj : Projectile {
	bool deflected;
	bool bouncing;
	float spawnPointX;
	bool canControl = true;
	Dictionary<string, float> bounceCooldowns = new();

	public HardKnuckleProj(
		Actor owner, Point pos, int xDir, ushort? netId, bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "hard_knuckle_proj", netId, altPlayer
	) {
		projId = (int)BluesProjIds.HardKnuckle;
		if (owner is Blues blu && blu.isBreakMan) {
			changeSprite("hard_knuckle_proj_bman", true);
		}
		fadeSprite = "generic_explosion";
		fadeOnAutoDestroy = true;
		destroyOnHit = false;
		spawnPointX = pos.x;
		canBeLocal = false;
		netOwner = altPlayer;

		vel.x = 0.25f * 60 * xDir;
		damager.damage = 2;
		damager.flinch = Global.halfFlinch;
		damager.hitCooldown = 60;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new HardKnuckleProj(
			args.owner, args.pos, args.xDir, args.netId, altPlayer: args.player
		);
	}

	public override void update() {
		base.update();
		// Max distance check.
		if (MathF.Abs(pos.x - spawnPointX) > 16 * 7) {
			destroySelf();
		}
		// Aceleration,
		float maxSpeed = 4f * 60;
		if (!deflected && vel.x * xDir < maxSpeed) {
			vel.x += Global.speedMul * xDir * (0.125f * 60f);
			if (vel.x * xDir >= maxSpeed) {
				vel.x = (float)xDir * maxSpeed;
				bouncing = false;
			}
		}
		// Bounce cooldown timers.
		foreach ((string key, float val) in bounceCooldowns) {
			bounceCooldowns[key] = Helpers.clampMin0(bounceCooldowns[key]) - Global.speedMul;
		}
		int inputYDir = 0;
		if (netOwner != null) {
			inputYDir = netOwner.input.getYDir(netOwner);
		}
		vel.y = 60 * inputYDir;
		if (ownedByLocalPlayer && (inputYDir != 0 || bouncing)) {
			forceNetUpdateNextFrame = true;
		}
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		if (damagable is Actor enemyActor && enemyActor.netId is not null and >= Level.firstNormalNetId) {
			string keyName = enemyActor.getActorTypeName() + "_" + enemyActor.netId;
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
		new Anim(pos, "hard_knuckle_proj_hit", xDir, damager.owner.getNextActorNetId(), true, false);

		vel.x = xDir * (-2 * 60);
		if (canControl) {
			vel.y = 0;
		}
		canControl = false;
		bouncing = true;
	}

	public override void onDeflect() {
		deflected = true;
		vel.x = xDir * 4 * 60;
		vel.y = 0;
		base.onDeflect();
	}

	public override void onReflect() {
		deflected = true;
		vel.x = xDir * 4 * 60;
		vel.y = 0;
		base.onReflect();
	}
}

public class HardKnuckleShoot : CharState {
	bool fired;
	bool effectCreated;
	Blues blues = null!;

	public HardKnuckleShoot() : base("knuckle") {
		airSprite = "knuckle_air";
		landSprite = "knuckle";
	}

	public override void update() {
		base.update();
		if (!effectCreated) {
			new Anim(
				character.getShootPos(),
				"generic_explosion", character.xDir, player.getNextActorNetId(), true,
				sendRpc: true, host: character, zIndex: ZIndex.Default + 1
			);
			effectCreated = true;
		}
		if (!fired && character.frameIndex == 1) {
			blues.hardKnuckleProj = new HardKnuckleProj(
				blues, blues.getShootPos().addxy(blues.xDir * 8, 0), blues.xDir,
				player.getNextActorNetId(), true, player
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

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		blues.inCustomShootAnim = false;
	}
}
