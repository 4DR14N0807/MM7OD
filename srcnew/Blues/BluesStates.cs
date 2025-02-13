using System;
using System.Collections.Generic;

namespace MMXOnline;

public class BluesShootAlt : CharState {
	Weapon stateWeapon;
	bool fired;
	Blues blues = null!;

	public BluesShootAlt(Weapon wep) : base("shoot2") {
		normalCtrl = false;
		airMove = true;
		canStopJump = true;
		stateWeapon = wep;
	}

	public override void update() {
		base.update();
		if (!fired && blues.frameIndex >= 2) {
			stateWeapon.shoot(blues, 0, 2);
			fired = true;
		}
		if (blues.isAnimOver()) {
			blues.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		blues = character as Blues ?? throw new NullReferenceException();
		bool air = !blues.grounded || blues.vel.y < 0;

		defaultSprite = sprite;
		landSprite = "shoot2";
		if (air) {
			sprite = "shoot2_air";
			defaultSprite = sprite;
		}
		blues.shieldCustomState = blues.isShieldActive;
		blues.changeSpriteFromName(sprite, true);
	}

	public override void onExit(CharState newState) {
		blues.inCustomShootAnim = false;
		blues.shieldCustomState = null;
		base.onExit(newState);
	}
}


public class BluesShootAltLadder : CharState {
	Weapon stateWeapon;
	bool fired;
	Ladder ladder;
	float midX; 
	Blues blues = null!;

	public BluesShootAltLadder(Weapon wep, Ladder ladder) : base("ladder_shoot2") {
		normalCtrl = false;
		stateWeapon = wep;
		this.ladder = ladder;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		blues = character as Blues ?? throw new NullReferenceException();
		blues.stopMoving();
		blues.useGravity = false;
		midX = ladder.collider.shape.getRect().center().x;
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		blues.inCustomShootAnim = false;
	}

	public override void update() {
		base.update();

		if (!fired && blues.frameIndex >= 1) {
			stateWeapon.shoot(blues, 0, 2);
			fired = true;
		}

		if (blues.isAnimOver()) {
			blues.changeState(new LadderClimb(ladder, midX), true);
		}
	}
}

public class BluesShieldSwapAir : CharState {
	Blues blues = null!;

	public BluesShieldSwapAir() : base("fall") {
	}

	public override void update() {
		base.update();
		float inputDir = player.input.getXDir(player);
		if (inputDir != 0 && blues.canMove()) {
			blues.move(new Point(inputDir * blues.getRunSpeed() * blues.getRunDebuffs(), 0));
		}
		if (blues.grounded) {
			blues.shakeCamera();
			blues.changeState(new BluesShieldSwapLand());
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		blues = character as Blues ?? throw new NullReferenceException();
		blues.changeSpriteEX("blues_jump_shield", true);
		blues.shieldCustomState = true;
	}

	public override void onExit(CharState newState) {
		blues.shieldCustomState = null;
		base.onExit(newState);
	}
}

public class BluesShieldSwapLand : CharState {
	Blues blues = null!;

	public BluesShieldSwapLand() : base("idle_swap") {
	}

	public override void update() {
		base.update();
		if (blues.isAnimOver() && stateFrames >= 6) {
			blues.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		blues = character as Blues ?? throw new NullReferenceException();
		blues.shieldCustomState = true;
		new StrikeAttackPushProj(blues.pos, 3, blues.xDir, blues, player.getNextActorNetId(), true);
		Anim anim = new Anim(
			blues.pos, "generic_explosion", 1, player.getNextActorNetId(), true, zIndex: ZIndex.Actor - 10
		);
		blues.playSound("crash");
		if (blues.isShieldActive) {
			blues.changeSpriteFromName("land", true);
		}
	}

	public override void onExit(CharState newState) {
		blues.shieldCustomState = null;
		base.onExit(newState);
	}
}

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
		if (stateTime >= 4 && player.input.isPressed(Control.Jump, player)) {
			if (blues.grounded) {
				blues.vel.y = -blues.getJumpPower();
				blues.changeState(new Jump());
				return;
			}
			if (blues.canAirJump()) {
				blues.lastJumpPressedTime = 0;
				blues.dashedInAir++;
				new Anim(blues.pos, "double_jump_anim", blues.xDir, player.getNextActorNetId(), true, true);
				blues.vel.y = -blues.getJumpPower();
				blues.changeState(new Jump());
				return;
			}
			blues.changeToIdleOrFall();
			return;
		}
	}

	public override void onEnter(CharState oldState) {
		blues = character as Blues ?? throw new NullReferenceException();
		blues.shieldCustomState = blues.isShieldActive;
		base.onEnter(oldState);
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
		Point move = new(blues.getSlideSpeed() * initialSlideDir, 0);
		character.move(move);

		if (slideTime >= 3 && particles > 0) {
			slideTime = 0;
			particles--;
			dust = new Anim(
				character.getDashDustEffectPos(initialSlideDir),
				"dust", initialSlideDir, player.getNextActorNetId(), true,
				sendRpc: true
			);
			dust.vel.y = (-particles - 1) * 20;
		} else {
			slideTime += Global.speedMul;
		}

		CollideData? cellingCheck = Global.level.checkTerrainCollisionOnce(character, 0, -16);
		if (cellingCheck != null) {
			return;
		}
		if (
			player.input.getXDir(player) == -initialSlideDir ||
			stateFrames >= 30 || (
				stateFrames >= 8 && character.deltaPos.x == 0 &&
				Global.level.checkTerrainCollisionOnce(character, character.xDir, 0) != null
			)
		) {
			character.changeToIdleOrFall();
			return;
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		initialSlideDir = character.xDir;
		blues = character as Blues ?? throw new NullReferenceException();
		blues.shieldCustomState = false;
		blues.changeGlobalColliderOnSpriteChange(blues.sprite.name);
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		//character.stopMovingWeak();
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
			int type = blues.overdrive ? 2 : (blues.overheating ? 0 : 1);
			if (shootDir == -1) {
				angleOffset = 128;
			}
			new ProtoBusterAngledProj(
				blues, blues.getShootPos(), (shotAngle + angleOffset) * shootDir, 
				type, player.getNextActorNetId(), rpc: true
			);
			// This way lemons and mid charge shot sounds wont conflict.
			if (type == 0) {
				blues.playSound("buster", sendRpc: true);
			}
			if (type == 1){
				blues.addCoreAmmo(1f);
				blues.playSound("buster2", sendRpc: true);
			}
			if (type == 2){
				blues.addCoreAmmo(-0.5f);
				blues.playSound("buster3", sendRpc: true);
			}
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
			fired = true;
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
				shootPos, character.xDir, character, player.getNextActorNetId(), sendRpc: true
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
				shootPos, character.xDir, character, player.getNextActorNetId(), true
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
			character.changeState(new Recover(), true);
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		blues = character as Blues ?? throw new NullReferenceException();
	}
}
public class Recover : CharState {
	Blues blues = null!;

	public Recover() : base("recover") {
		superArmor = true;
	}

	public override void update() {
		base.update();
		if (character.isAnimOver()) {
			character.changeToLandingOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		blues = character as Blues ?? throw new NullReferenceException();
	}
}

public class BluesRevive : CharState {
	float radius = 200;
	float healTime = -50;
	bool fullHP;
	Blues blues = null!;

	public BluesRevive() : base("revive") {
		invincible = true;
		useGravity = false;
	}

	public override void update() {
		base.update();
		blues.healShieldHPCooldown = 15;
		healTime++;
		if (!fullHP) {
			character.addRenderEffect(RenderEffectType.Flash, 3, 5);
			character.move(new Point(0, -10));
		}
		if (!fullHP && healTime >= 4) {
			if (blues.health < blues.maxHealth) {
				blues.health = Helpers.clampMax(blues.health + 1, blues.maxHealth);
			}
			else {
				blues.shieldHP++;
				if (blues.shieldHP >= blues.shieldMaxHP) {
					blues.shieldHP = blues.shieldMaxHP;
					fullHP = true;
					blues.frameSpeed = 1;
				}
			}
			blues.playSound("heal", forcePlay: true, sendRpc: true);
			healTime = 0;
		}
		if (blues.frameIndex >= 1 && !once) {
			new GravityHoldProj(
				blues, blues.getCenterPos(), blues.xDir,
				player.getNextActorNetId(), true
			);
			once = true;
		}
		if (blues.isAnimOver() && fullHP) {
			blues.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		blues = character as Blues ?? throw new NullReferenceException();
		blues.isBreakMan = true;
		blues.playSound("whistle", true, true);
		blues.frameSpeed = 0;
		blues.overheating = true;
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		character.removeRenderEffect(RenderEffectType.Flash);
		blues.overheating = false;
	}
}
