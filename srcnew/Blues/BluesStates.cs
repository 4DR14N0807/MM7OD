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
		blues.shieldCustomState = false;
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
			int type = (blues.overheating ? 0 : 1);
			if (shootDir == -1) {
				angleOffset = 128;
			}
			new ProtoBusterAngledProj(
				character.getShootPos(), (shotAngle + angleOffset) * shootDir, type,
				player, player.getNextActorNetId(), rpc: true
			);
			if (type == 1) {
				blues.addCoreAmmo(0.5f);
			}
			blues.playSound("buster", sendRpc: true);
			shotAngle -= 16;
			shotLastFrame = character.frameIndex;
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
	float startTime;
	bool fired;
	float coreCooldown = 60;

	public ProtoStrike() : base("strikeattack") {
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		blues = character as Blues ?? throw new NullReferenceException();
	}

	public override void update() {
		base.update();
		blues.resetCoreCooldown();
		bool isShooting = blues.chargeButtonHeld();

		if (!fired && character.frameIndex >= 3) {
			Point shootPos = character.getShootPos();
			new ProtoStrikeProj(
				shootPos, character.xDir, player, player.getNextActorNetId(), true
			);
			blues.playSound("danger_wrap_explosion", true, true);
			fired = true;
			startTime = stateFrames;
		}
		if (!fired) {
			return;
		}
		if (!isShooting && stateFrames >= startTime + 60 || stateFrames >= startTime + 180) {
			character.setHurt(-character.xDir, Global.halfFlinch, false);
			character.slideVel = 200 * -character.xDir;
			return;
		}
		coreCooldown -= Global.speedMul;
		if (coreCooldown <= 0) {
			coreCooldown = 20;
			blues.addCoreAmmo(1);
		}
	}
}


public class RedStrike : CharState {
	Blues blues = null!;
	float startTime;
	bool fired;

	public RedStrike() : base("strikeattack") {
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		blues = character as Blues ?? throw new NullReferenceException();
	}

	public override void update() {
		base.update();
		blues.overdriveAmmoDecreaseCooldown = blues.overdriveAmmoMaxCooldown;

		if (!fired && character.frameIndex >= 3) {
			Point shootPos = character.getShootPos();
			new RedStrikeProj(
				shootPos, character.xDir, player, player.getNextActorNetId(), true
			);
			blues.playSound("buster3", true, true);
			fired = true;
			startTime = stateFrames;
		}
		if (stateFrames >= startTime + 20) {
			blues.addCoreAmmo(4);
			character.setHurt(-character.xDir, Global.halfFlinch, false);
			character.slideVel = 200 * -character.xDir;
			return;
		}
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		blues.redStrikeCooldown = 4 * 60;
	}
}


public class OverheatShutdownStart : CharState {
	Blues blues = null!;

	public OverheatShutdownStart() : base("hurt") {
		superArmor = true;
	}

	public override void update() {
		base.update();
		if (stateFrames >= 30 && character.grounded) {
			if (!blues.overheating) {
				character.changeToIdleOrFall();
				return;
			}
			character.changeState(new OverheatShutdown(), true);
			return;
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		blues = character as Blues ?? throw new NullReferenceException();
		blues.coreAmmo = blues.coreMaxAmmo;
		blues.coreAmmoDecreaseCooldown = 10;
		blues.playSound("danger_wrap_explosion", sendRpc: true);
		character.vel.y = -4.25f * 60;
		character.slideVel = 1.75f * 60 * -character.xDir;
	}
}

public class OverheatShutdown : CharState {
	Blues blues = null!;

	public OverheatShutdown() : base("shutdown") {
		superArmor = true;
	}

	public override void update() {
		base.update();
		if (blues != null && !blues.overheating) {
			character.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		blues = character as Blues ?? throw new NullReferenceException();
	}
}


public class BluesRevive : CharState {
	float radius = 200;
	Blues? blues;
	public BluesRevive() : base("revive") {
		invincible = true;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		blues = character as Blues;
		player.health = 1;
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		character.useGravity = true;
		character.removeRenderEffect(RenderEffectType.Flash);
		//Global.level.delayedActions.Add(new DelayedAction(() => { character.destroyMusicSource(); }, 0.75f));
		blues.isBreakMan = true;

		if (character != null) {
			character.invulnTime = 0.5f;
		}
	}

	public override void update() {
		base.update();

		if (radius >= 0) {
			radius -= Global.spf * 150;
		}
		if (character.frameIndex < 2) {
			if (Global.frameCount % 4 < 2) {
				character.addRenderEffect(RenderEffectType.Flash);
			} else {
				character.removeRenderEffect(RenderEffectType.Flash);
			}
		} else {
			character.removeRenderEffect(RenderEffectType.Flash);
		}
		if (character.frameIndex == 3 && !once) {
			character.addHealth(player.maxHealth);
			once = true;
		}
		if (character.ownedByLocalPlayer) {
			if (character.isAnimOver()) {
				character.changeToIdleOrFall();
			}
		}
	}
}
