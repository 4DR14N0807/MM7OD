using System;
using System.Collections.Generic;

namespace MMXOnline;

public class ShieldDash : CharState {
	bool soundPlayed;
	string initialSlideButton;
	int initialXDir;
	float dustTimer;

	public ShieldDash(string initialSlideButton) : base("shield_dash") {
		this.initialSlideButton = initialSlideButton;
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
		move.x = 3.5f * initialXDir;
		if (character.frameIndex >= 1) {
			if (!soundPlayed) {
				character.playSound("slide", sendRpc: true);
				soundPlayed = true;
			}
			character.move(move * 60);
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

public class ProtoAirShoot : CharState {
	int shotAngle = 64;
	int shotLastFrame = 10;
	Blues blues = null!;

	public ProtoAirShoot() : base("jump_shoot2") {
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
			new ProtoBusterChargedProj(
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
	GenericMeleeProj pStrike;
	bool didUseAmmo;

	public ProtoStrike() : base("protostrike") {

	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		blues = character as Blues ?? throw new NullReferenceException();
	}


	public override void update() {
		base.update();

		bool isShooting = player.input.isHeld(Control.Shoot, player);

		if (character.isAnimOver()) {
			if (isShooting) {
				character.frameIndex = 3;
			} else character.changeState(new ProtoStrikeEnd(), true);
			shot = false;
			if (pStrike != null) pStrike.destroySelf();
		}

		if (character.frameIndex >= 3) isUsingPStrike = true;
		else isUsingPStrike = false;

		if (isUsingPStrike) {
			if (!shot) {
				var shootPos = character.getShootPos();
				pStrike = new GenericMeleeProj(new Weapon(), shootPos, 0, player, 3, Global.halfFlinch, 1f);
				if (!didUseAmmo) {
					blues.addCoreAmmo(-3);
					didUseAmmo = true;
				}
				var rect = new Rect(0, 0, 32, 24);
				pStrike.globalCollider = new Collider(rect.getPoints(), false, pStrike, false, false, 0, new Point());
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


public class OverheatStunned : CharState {
	public OverheatStunned() : base("hurt") {

	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
	}

	public override void update() {
		base.update();
		if (stateFrames >= 26) {
			character.changeToIdleOrFall();
		}
	}
}
