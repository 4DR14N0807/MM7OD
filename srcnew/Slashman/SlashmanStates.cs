using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline;

public class SlashMRun : BaseRun {
	public Slashman slashman = null!;
	public int xDir = 1;

	public SlashMRun(bool skipInto = false) : base("run", transitionSprite: "run_start") {
		exitOnAirborne = true;
		attackCtrl = true;
		normalCtrl = true;
		if (skipInto) {
			sprite = defaultSprite;
		}
	}

	public override void update() {
		base.update();
		float runSpeed = character.getRunSpeed();
		float maxSpeed = SMPhysics.maxSprint - SMPhysics.minWalk;
		float animSpeed = ((runSpeed - SMPhysics.minWalk) / (maxSpeed * 1.25f)) + 1;
		if (character.sprite.name != character.getSprite("land")) {
			character.sprite.frameSpeed = animSpeed;
		}

		int ixDir = player.input.getXDir(player);
		if (runSpeed <= 0.5f && ixDir != character.xDir) {
			if (character.grounded && ixDir != 0) {
				int fi = character.frameIndex;
				character.changeState(character.getRunState(true));
				character.setFrameIndexSafe(fi);
				return;
			}
			character.changeToIdleOrFall();
			return;
		} else {
			character.moveXY(runSpeed * xDir, 0);
			if (runSpeed > 1 && ixDir != 0 && ixDir != xDir &&
				!character.groundedIce && !character.sprite.name.EndsWith("land")
			) {
				character.changeSpriteFromName("land", true);
			}
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		slashman = character as Slashman ?? throw new NullReferenceException();
		int ixDir = player.input.getXDir(player);
		if (ixDir != 0 && character.canTurn()) {
			character.xDir = player.input.getXDir(player);
		}
		xDir = character.xDir;
	}
}


public class SlashMSlash : CharState {

	bool throwProj;
	public int dirModifier = 1;
	int throwDir;
	Slashman? slashM;
	public SlashMSlash(bool throwProj) : base(throwProj ? "throw" : "attack") {
		normalCtrl = true;
		attackCtrl = true;
		airMove = true;
		this.throwProj = throwProj;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		throwDir = (-player.input.getYDir(player)) + 1;

		slashM = character as Slashman ?? throw new NullReferenceException();
		if (slashM.grounded) {
			slashM.slideVel = slashM.runSpeed * slashM.xDir;
		}
	}

	public override void update() {
		base.update();

		if (!once && character.currentFrame.getBusterOffset() != null) {
			spawnProj();
			once = true;
		}

		if (character.isAnimOver()) {
			exitState();
		}
	}

	void spawnProj() {
		if (throwProj) {
			new SlashManGooProj(
				character, character.getShootPos(), character.getShootXDir() * dirModifier, 
				throwDir, slashM?.onRage ?? false, player.getNextActorNetId(), true
			);
			character.playSound("SlashMTomatoToss", sendRpc: true);
		} else {
			new SlashManClawProj(
				character, character.getShootPos(), character.getShootXDir() * dirModifier, 
				slashM?.onRage ?? false, player.getNextActorNetId(), true
			);
			character.playSound("slash_claw", sendRpc: true);
		}
	}

	public virtual void exitState() {
		character.changeToIdleOrFall();
	}
}


public class SlashMWallSlash : SlashMSlash {

	CharState? oldState;
	public SlashMWallSlash(bool throwProj) : base(throwProj) {
		sprite = "attack_wall";
		normalCtrl = false;
		attackCtrl = true;
		airMove = false;
		useGravity = false;
		dirModifier = -1;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		this.oldState = oldState;
	}

	public override void exitState() {
		character.changeState(oldState ?? throw new NullReferenceException());
	}
}


public class SlashMLadderSlash : SlashMSlash {
	Ladder ladder;
	public SlashMLadderSlash(Ladder ladder, bool throwProj) : base(throwProj) {
		normalCtrl = false;
		attackCtrl = true;
		useGravity = false;
		airMove = false;
		this.ladder = ladder;
	}

	public override void exitState() {
		float midX = ladder.collider.shape.getRect().center().x;
		character.changeState(new LadderClimb(ladder, midX));
	}
}


public class SlashMSpinAttack : CharState {
	float vel;
	float time = 45;
	public SlashMSpinAttack(float vel) : base("spin", transitionSprite: "flip") {
		normalCtrl = true;
		attackCtrl = true;
		this.vel = vel;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.vel.y = -180;
		character.slideVel = character.getShootXDir() * 3;
		vel = SMPhysics.maxSprint * character.getShootXDir();
		if ((character as Slashman)?.onRage == true) {
			vel *= 1.5f;
			time = 35;
		}	
		character.xDir = MathF.Sign(vel);
	}

	public override void update() {
		base.update();

		if (inTransition()) {
			if (character.loopCount >= 1) {
				sprite = defaultSprite;
				character.changeSpriteFromName(defaultSprite, true);
			}
		} else {
			character.move(new Point(vel * 60 * 2, 0));

			if (stateFrames >= time) {
				character.changeToIdleOrFall();
			}
		}
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);

		character.slideVel = 2f * character.getShootXDir();
		if (newState is not Jump) {
			character.vel.y += -180;
		}
	}
}


public class SlashMDiveStart : CharState {
	public SlashMDiveStart(bool grounded) : base("flip") {
		useGravity = grounded;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);

		character.slideVel = 2 * character.getShootXDir();
		if (character.grounded) {
			character.vel.y = -300;
		} else {
			character.yPushVel = -2;
			character.stopMoving();
		}
	}

	public override void update() {
		base.update();

		if (character.loopCount >= 1) {
			character.changeState(new SlashMDive());
		}
	}
}

public class SlashMDive : CharState {
	float speed = 6;
	Point vel;
	float ang = 48;
	public SlashMDive() : base("dive2") {}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);

		character.stopMovingS();
		bool side = player.input.getXDir(player) == character.getShootXDir();

		if (side) {
			ang -= 20;
			defaultSprite = "dive";
			sprite = defaultSprite;
			character.changeSpriteFromName(sprite, true);
		}

		if (character.xDir < 0) {
			ang = -ang + 128;
		}

		vel = Point.createFromByteAngle(ang).times(speed);
		if (vel.x != 0) {
			character.xDir = MathF.Sign(vel.x);
		}
		if (oldState is SlashMWallSlide) {
			vel.x *= -1;
			character.xDir *= -1;
		}
	}

	public override void update() {
		base.update();

		if (!character.tryMoveExact(vel, out _) && stateFrames >= 6) {
			character.changeToIdleOrFall("land");
		}
	}

	public override void render(float x, float y) {
		base.render(x, y);

		/* if (stateFrames >= 1) {
			Point pos = character.pos.subtract(vel.times(1));
			
		}

		if (stateFrames >= 6) {
			Point pos = character.pos.subtract(vel.times(6));
			Global.sprites[character.sprite.name].draw(
				character.frameIndex, pos.x, pos.y,
				character.xDir, character.yDir, null, character.alpha,
				character.xScale, character.yScale, character.zIndex - 2, character.getShaders(),
				useFrameOffsets: true
			);
		}

		if (stateFrames >= 12) {
			Point pos = character.pos.subtract(vel.times(12));
			Global.sprites[character.sprite.name].draw(
				character.frameIndex, pos.x, pos.y,
				character.xDir, character.yDir, null, character.alpha,
				character.xScale, character.yScale, character.zIndex - 2, character.getShaders(),
				useFrameOffsets: true
			);
		} */
	}
}


public class SlashMFlip : CharState {
	public SlashMFlip() : base("flip") {
		normalCtrl = true;
		attackCtrl = false;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.move(new Point(0, -16));
		character.vel.y = -300;
	}

	public override void update() {
		base.update();

		if (stateFrames >= 6) {
			attackCtrl = true;
		}

		if (character.loopCount >= 1) {
			character.changeToIdleOrFall();
		}
	}
}


public class SlashMWallSlide : CharState {

	int wallDir;
	Collider wallCollider;
	Slashman? slashM;
	float dustTime;

	public SlashMWallSlide(
		int wallDir, Collider wallCollider
	) : base("wall_slide") 
	{
		this.wallDir = wallDir;
		this.wallCollider = wallCollider;
		normalCtrl = true;
		attackCtrl = true;
		enterSound = "wallLand";
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		if (player.isAI && character.ai != null) {
			character.ai.jumpTime = 0;
		}

		slashM = character as Slashman ?? throw new NullReferenceException();
	}

	public override void update() {
		base.update();
		if (character.grounded) {
			character.changeToIdleOrFall();
			return;
		}

		character.useGravity = false;
		character.vel.y = 0;

		bool downPressed = player.input.isHeld(Control.Down, player);

		if (stateFrames >= 9) {
			if (slashM != null) {
				var hit = character.getHitWall(wallDir, 0);
				var hitWall = hit?.gameObject as Wall;

				if (hitWall == null || !hitWall.collider.isClimbable) {
					var hitActor = hit?.gameObject as Actor;
					if (hitActor == null || !hitActor.isPlatform) {
						character.changeState(character.getFallState());
					}
				}
			}
			if (downPressed) character.move(new Point(0, 60));
		}

		if (downPressed) {
			dustTime += Global.speedMul;
		} else {
			dustTime = 0;
		}
		
		if (stateFrames > 12 && dustTime > 6) {
			dustTime = 0;
			generateDust(character);
		}
	}

	public override void onExit(CharState? newState) {
		character.useGravity = true;
		base.onExit(newState);
	}

	public void generateDust(Character character) {
		new Anim(character.pos.addxy(character.xDir * 10, -24), "dust", character.xDir, character.player.getNextActorNetId(), true, sendRpc: true);
		
	}
}

public class SlashMWallKick : CharState {
	public float dashThreshold = 0.2f;

	public SlashMWallKick() : base("jump", "wall_kick_shoot") {
		accuracy = 5;
		exitOnLanding = true;
		useDashJumpSpeed = true;
		airMove = true;
		canStopJump = true;
		attackCtrl = true;
		normalCtrl = true;
		enterSound = "jump";
		enterSoundArgs = "larmor";
	}

	public override void update() {
		base.update();
		if (character.vel.y > 0 || character.xDir == -player.input.getXDir(player)) {
			character.changeState(character.getFallState());
		}
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		if (newState is not SlashMWallSlide && player.input.getXDir(player) == -character.xDir) {
			character.xDir *= -1;
		}
	}
}


public class SlashMBlock : CharState {
	public SlashMBlock() : base("block") {
		normalCtrl = true;
		attackCtrl = false;
		airSprite = "block_air";
		fallSprite = airSprite;
		landSprite = defaultSprite;
	}

	public override void update() {
		base.update();

		character.turnToInput(player.input, player);
		airTrasition();

		if (!player.input.isHeld(Control.WeaponLeft, player)) {
			character.changeToIdleOrFall();
		}
	}
}