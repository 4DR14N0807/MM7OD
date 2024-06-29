using System;
using System.Collections.Generic;

namespace MMXOnline;

public class ShieldDash : CharState {
	bool soundPlayed;
	int initialXDir;
	float dustTimer;
	Blues blues = null!;

	public ShieldDash() : base("dash") {
		accuracy = 10;
		useGravity = false;
	}

	public override void update() {
		base.update();
		if (stateFrames >= 40) {
			character.changeToIdleOrFall();
			return;
		}
		var move = new Point(0, 0);
		move.x = blues.getShieldDashSpeed() * initialXDir;
		if (character.frameIndex >= 1) {
			if (!soundPlayed) {
				character.playSound("slide", sendRpc: true);
				soundPlayed = true;
			}
			character.move(move);
		}
		if (character.grounded) {
			dustTimer += Global.speedMul;
		} else {
			dustTimer = 0;
		}
		if (dustTimer >= 4) {
			new Anim(
				character.getDashDustEffectPos(initialXDir).addxy(0, 4),
				"dust", initialXDir, player.getNextActorNetId(), true,
				sendRpc: true
			) { vel = new Point(0, -40) };
			dustTimer = 0;
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		blues = character as Blues ?? throw new NullReferenceException();
		initialXDir = character.xDir;
		character.isDashing = true;
		character.vel.y = 0;
	}

	public override void onExit(CharState newState) {
		if (!character.grounded) {
			character.dashedInAir++;
		}
		base.onExit(newState);
	}
}

public class BluesSlide : CharState {
	public float slideTime = 0;
	public int initialSlideDir;
	public int particles = 3;
	Blues blues = null!;
	Anim? dust;

	public BluesSlide() : base("slide") {
		enterSound = "slide";
		accuracy = 10;
		exitOnAirborne = true;
		attackCtrl = true;
		normalCtrl = true;
	}

	public override void update() {
		base.update();
		if (player.input.getXDir(player) == -initialSlideDir ||
			slideTime >= Global.spf * 30 ||
			Global.level.checkCollisionActor(character, 24 * character.xDir, 0) != null
		) {
			character.changeToIdleOrFall();
			return;
		}
		Point move = new(blues.getSlideSpeed() * initialSlideDir, 0);
		character.move(move);

		slideTime += Global.spf;
		if (stateTime >= Global.spf * 3 && particles > 0) {
			stateTime = 0;
			particles--;
			dust = new Anim(
				character.getDashDustEffectPos(initialSlideDir),
				"dust", initialSlideDir, player.getNextActorNetId(), true,
				sendRpc: true
			);
			dust.vel.y = (-particles - 1) * 20;
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		initialSlideDir = character.xDir;
		blues = character as Blues ?? throw new NullReferenceException();
	}
}

public class BluesSpreadShoot : CharState {
	int shotAngle = 64;
	int shotLastFrame = 10;
	Blues blues = null!;

	public BluesSpreadShoot() : base("spreadshoot_air") {
		airMove = true;
		exitOnLanding = true;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		blues = character as Blues ?? throw new NullReferenceException();
	}

	public override void update() {
		base.update();
		if (character.frameIndex != shotLastFrame) {
			int angleOffset = 1;
			int shootDir = blues.getShootXDir();
			if (shootDir == -1) {
				angleOffset = 128;
			}
			new ProtoBusterAngledProj(
				character.getShootPos(), (shotAngle + angleOffset) * shootDir,
				player, player.getNextActorNetId(), rpc: true
			);
			blues.playSound("buster", sendRpc: true);
			blues.addCoreAmmo(0.5f);

			shotAngle -= 16;
			shotLastFrame = character.frameIndex;
		}
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}
}

public class ProtoChargeShotState : CharState {
	bool fired;
	Blues blues = null!;

	public ProtoChargeShotState() : base("chargeshot") {
		airMove = true;
		canStopJump = true;
		canJump = true;
		//landSprite = "chargeshot";
		//airSprite = "jump_chargeshot";
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		blues = character as Blues ?? throw new NullReferenceException();
	}


	public override void update() {
		base.update();

		if (!fired && character.frameIndex >= 3) {
			new ProtoBusterLv3Proj(
				character.getShootPos(), character.getShootXDir(),
				player, player.getNextActorNetId(), true
			);
			fired = true;
			character.playSound("buster3", sendRpc: true);
			character.stopCharge();
		}
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}
}

public class ProtoGenericShotState : CharState {
	bool fired;
	Blues blues = null!;
	Weapon weapon;

	public ProtoGenericShotState(Weapon weapon) : base("chargeshot") {
		airMove = true;
		canStopJump = true;
		canJump = true;
		this.weapon = weapon;
		landSprite = "chargeshot";
		airSprite = "jump_chargeshot";
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		blues = character as Blues ?? throw new NullReferenceException();
	}

	public override void update() {
		base.update();

		if (!fired && character.frameIndex == 3) {
			weapon.shoot(blues, 0);
		}
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}
}

public class ProtoStrike : CharState {
	Blues blues = null!;
	bool isUsingPStrike;
	bool shot;
	float coreCooldown;
	bool didUseAmmo;
	int chargeLv;

	public ProtoStrike(int chargeLv) : base("protostrike") {
		this.chargeLv = chargeLv;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		blues = character as Blues ?? throw new NullReferenceException();
	}


	public override void update() {
		base.update();

		bool isShooting = player.input.isHeld(Control.Shoot, player);
		if (chargeLv >= 3) blues.overridePSDamage = true;

		if (character.isAnimOver()) {
			if (isShooting) {
				character.frameIndex = 3;
			} else character.changeState(new ProtoStrikeEnd(), true);
			shot = false;
			blues.overridePSDamage = false;
		}

		if (character.frameIndex >= 3) isUsingPStrike = true;
		else isUsingPStrike = false;

		if (isUsingPStrike) {
			if (!shot) {
				var shootPos = character.getShootPos();
				/*if (!didUseAmmo) {
					blues.addCoreAmmo(-3);
					didUseAmmo = true;
				}*/
				shot = true;
			}
			coreCooldown += Global.spf;
		}

		if (coreCooldown >= Global.spf * 15) {
			coreCooldown = 0;
			blues.addCoreAmmo(1);
		}
	}
}

public class ProtoStrikeEnd : CharState {
	public ProtoStrikeEnd() : base("protostrike_end") {

	}

	public override void update() {
		base.update();
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}
}

public class OverheatShutdown : CharState {
	Blues blues = null!;

	public OverheatShutdown() : base("shutdown") {
		superArmor = true;
	}

	public override bool canExit(Character character, CharState newState) {
		return !blues.overheating;
	}
	public override void update() {
		base.update();
		if (!blues.overheating) {
			character.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		blues = character as Blues ?? throw new NullReferenceException();
		blues.coreAmmo = blues.coreMaxAmmo;
		blues.coreAmmoDecreaseCooldown = 10;
		blues.playSound("danger_wrap_explosion", sendRpc: true);
	}
}
