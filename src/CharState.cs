using System;
using System.Collections.Generic;

namespace MMXOnline;

public class CharState {
	public string sprite;
	public string defaultSprite;
	public string attackSprite;
	public string shootSprite;
	public string transitionSprite;
	public string landSprite = "";
	public string airSprite = "";
	public string fallSprite = "";
	public bool airSpriteReset;
	public bool wasGrounded = true;
	public Point busterOffset;
	public Character character = null!;
	public Collider? lastLeftWallCollider;
	public Collider? lastRightWallCollider;
	public Wall? lastLeftWall;
	public Wall? lastRightWall;
	public Collider? wallKickLeftWall;
	public Collider? wallKickRightWall;
	public float stateTime;
	public float stateFrames;
	public string enterSound = "";
	public string enterSoundArgs = "";
	public float framesJumpNotHeld = 0;
	public bool once;
	public bool useGravity = true;

	public bool invincible;
	public bool stunResistant;
	public bool superArmor;
	public bool immuneToWind;
	public int accuracy;
	public bool isGrabbedState;

	public bool wasVileHovering;

	// For grab states (I am grabber)
	public bool isGrabbing;

	// For grabbed states (I am the grabbed)
	public float grabTime = 4;

	// Gacel notes.
	// This should be inside the character object to sync while online.
	public virtual void releaseGrab() {
		grabTime = 0;
	}

	// Control system.
	// This dictates if it can attack or land.
	public bool attackCtrl;
	public bool[] altCtrls = new bool[1];
	public bool normalCtrl;
	public bool airMove;
	public bool canJump;
	public bool canStopJump;
	public bool stoppedJump;
	public bool exitOnLanding;
	public bool exitOnAirborne;
	public bool useDashJumpSpeed;
	public SpecialStateIds specialId;

	public CharState(string sprite, string shootSprite = "", string attackSprite = "", string transitionSprite = "") {
		this.sprite = string.IsNullOrEmpty(transitionSprite) ? sprite : transitionSprite;
		this.transitionSprite = transitionSprite;
		defaultSprite = sprite;
		this.shootSprite = shootSprite;
		this.attackSprite = attackSprite;
		stateTime = 0;
	}

	public bool canUseShootAnim() {
		return !string.IsNullOrEmpty(shootSprite);
	}

	public Player player {
		get {
			return character.player;
		}
	}

	public virtual void onExit(CharState? newState) {
		if (!useGravity) {
			character.useGravity = true;
		}
		// Stop the dash speed on transition to any frame except jump/fall (dash lingers in air) or dash itself
		// TODO: Add a bool here to charstate.
		if (newState == null) {
			character.rideArmorPlatform = null;
			return;
		}
		if (newState is not Dash &&
			newState is not Jump &&
			newState is not Fall &&
			!(newState.useDashJumpSpeed && (!character.grounded || character.vel.y < 0))
		) {
			character.isDashing = false;
		}
		if (newState is Hurt || newState is Die ||
			newState is GenericStun
		) {
			character.onFlinchOrStun(newState);
		}
		if (character.rideArmorPlatform != null && (
			newState is Hurt || newState is Die ||
			newState is CallDownMech || newState.isGrabbedState == true
		)) {
			character.rideArmorPlatform = null;
		}
		if (invincible) {
			//player.delayWTank();
		}
		character.onExitState(this, newState);
	}

	public virtual void onEnter(CharState oldState) {
		if (!string.IsNullOrEmpty(enterSound)) {
			character.playAltSound(enterSound, sendRpc: true, altParams: enterSoundArgs);
		}
		if (oldState is VileHover) {
			wasVileHovering = true;
		}
		if (!useGravity || character.isDWrapped) {
			character.useGravity = false;
			character.stopMovingWeak();
		}
		if (this is not Run and not Idle and not Taunt) {
			player.delayETank();
		} 
		wasGrounded = character.grounded && character.vel.y >= 0;
		wasGrounded = character.grounded;
		if (this is not Jump and not WallKick and not TenguBladeState && (!oldState.canStopJump || oldState.stoppedJump)) {
			stoppedJump = true;
		}
		if (character is Blues) {
			character.changeGlobalColliderOnSpriteChange(character.sprite.name);
		}
	}

	public virtual bool canEnter(Character character) {
		if (character.charState is InRideArmor &&
			!(this is Die || this is Idle || this is Jump || this is Fall || this is StrikeChainHooked || this is ParasiteCarry || this is VileMK2Grabbed || this is DarkHoldState ||
			  this is NecroBurstAttack || this is UPGrabbed || this is WhirlpoolGrabbed || this is DeadLiftGrabbed || Helpers.isOfClass(this, typeof(GenericGrabbedState)))) {
			return false;
		}
		if (character.charState is DarkHoldState dhs && dhs.stunTime > 0) {
			if (this is not Die && this is not Hurt) {
				return false;
			}
		}
		if (character.charState is WarpOut && this is not WarpIn) {
			return false;
		}
		return true;
	}

	public virtual bool canExit(Character character, CharState newState) {
		return true;
	}

	public bool inTransition() {
		return (
			!string.IsNullOrEmpty(transitionSprite) &&
			sprite == transitionSprite &&
			character.sprite.name != null &&
			character.sprite.name.Contains(transitionSprite)
		);
	}

	public virtual void render(float x, float y) {
	}

	public virtual void update() {
		stateTime += Global.spf;
		if (!character.ownedByLocalPlayer) {
			return;
		}
		if (inTransition()) {
			character.frameSpeed = 1;
			if (character.isAnimOver() && !Global.level.gameMode.isOver) {
				sprite = defaultSprite;
				if (character.shootAnimTime > 0 && shootSprite != "") {
					character.changeSpriteFromName(shootSprite, true);
				} else {
					character.changeSpriteFromName(sprite, true);
				}
			}
		}
		var lastLeftWallData = character.getHitWall(-1, 0);
		lastLeftWallCollider = lastLeftWallData != null ? lastLeftWallData.otherCollider : null;
		if (lastLeftWallCollider != null && !lastLeftWallCollider.isClimbable) {
			lastLeftWallCollider = null;
		}
		lastLeftWall = lastLeftWallData?.gameObject as Wall;

		var lastRightWallData = character.getHitWall(1, 0);
		lastRightWallCollider = lastRightWallData != null ? lastRightWallData.otherCollider : null;
		if (lastRightWallCollider != null && !lastRightWallCollider.isClimbable) {
			lastRightWallCollider = null;
		}
		lastRightWall = lastRightWallData?.gameObject as Wall;

		var wallKickLeftData = character.getHitWall(-8, 0);
		if (wallKickLeftData?.otherCollider?.isClimbable == true && wallKickLeftData?.gameObject is Wall) {
			wallKickLeftWall = wallKickLeftData.otherCollider;
		} else {
			wallKickLeftWall = null;
		}
		var wallKickRightData = character.getHitWall(8, 0);
		if (wallKickRightData?.otherCollider?.isClimbable == true && wallKickRightData?.gameObject is Wall) {
			wallKickRightWall = wallKickRightData.otherCollider;
		} else {
			wallKickRightData = null;
		}


		// Moving platforms detection
		CollideData? leftWallPlat = character.getHitWall(-Global.spf * 300, 0);
		if (leftWallPlat?.gameObject is Wall leftWall && leftWall.isMoving) {
			character.move(leftWall.deltaMove, useDeltaTime: true);
			lastLeftWallCollider = leftWall.collider;
		} else if (leftWallPlat?.gameObject is Actor leftActor && leftActor.isPlatform && leftActor.pos.x < character.pos.x) {
			lastLeftWallCollider = leftActor.collider;
		}

		CollideData? rightWallPlat = character.getHitWall(Global.spf * 300, 0);
		if (rightWallPlat?.gameObject is Wall rightWall && rightWall.isMoving) {
			character.move(rightWall.deltaMove, useDeltaTime: true);
			lastRightWallCollider = rightWall.collider;
		} else if (rightWallPlat?.gameObject is Actor rightActor && rightActor.isPlatform && rightActor.pos.x > character.pos.x) {
			lastRightWallCollider = rightActor.collider;
		}

		airTrasition();
		wasGrounded = character.grounded && character.vel.y >= 0;
	}

	public virtual void airTrasition() {
		if (airSprite != "" && !character.grounded && wasGrounded && sprite == landSprite) {
			sprite = airSprite;
			if (character.vel.y >= 0 && fallSprite != "") {
				sprite = fallSprite;
			}
			int oldFrameIndex = character.sprite.frameIndex;
			float oldFrameTime = character.sprite.frameTime;
			character.changeSpriteFromName(sprite, false);
			if (oldFrameIndex < character.sprite.totalFrameNum) {
				character.sprite.frameIndex = oldFrameIndex;
				character.sprite.frameTime = oldFrameTime;
			} else {
				character.sprite.frameIndex = character.sprite.totalFrameNum - 1;
				character.sprite.frameTime = character.sprite.getCurrentFrame().duration;
			}
		} else if (landSprite != "" && character.grounded && !wasGrounded && sprite == airSprite) {
			character.playAltSound("land", sendRpc: true, altParams: "larmor");
			sprite = landSprite;
			int oldFrameIndex = character.frameIndex;
			float oldFrameTime = character.frameTime;
			character.changeSpriteFromName(sprite, false);
			if (oldFrameIndex < character.sprite.totalFrameNum) {
				character.frameIndex = oldFrameIndex;
				character.frameTime = oldFrameTime;
			} else {
				character.frameIndex = character.sprite.totalFrameNum - 1;
				character.frameTime = character.sprite.getCurrentFrame().duration;
			}
		}
	}

	public void landingCode() {
		character.playSound("land", sendRpc: true);
		character.dashedInAir = 0;
		changeToIdle();
		if (character.ai != null) {
			character.ai.jumpTime = 0;
		}
	}

	public void groundCodeWithMove() {
		if (character.canTurn()) {
			if (player.input.isHeld(Control.Left, player) || player.input.isHeld(Control.Right, player)) {
				if (player.input.isHeld(Control.Left, player)) character.xDir = -1;
				if (player.input.isHeld(Control.Right, player)) character.xDir = 1;
				if (character.canMove()) character.changeState(new Run());
			}
		}
	}

	public void changeToIdle(string ts = "") {
		if (character.grounded &&
			string.IsNullOrEmpty(ts) && (
			player.input.isHeld(Control.Left, player) ||
			player.input.isHeld(Control.Right, player))
		) {
			character.changeState(new Run());
		} else {
			character.changeToIdleOrFall(ts);
		}
	}

	public void checkLadder(bool isGround) {
		if (character.charState is LadderClimb) {
			return;
		}
		if (player.input.isHeld(Control.Up, player)) {
			List<CollideData> ladders = Global.level.getTerrainTriggerList(character, new Point(0, 0), typeof(Ladder));
			if (ladders != null && ladders.Count > 0 && ladders[0].gameObject is Ladder ladder) {
				var midX = ladders[0].otherCollider.shape.getRect().center().x;
				if (Math.Abs(character.pos.x - midX) < 12) {
					var rect = ladders[0].otherCollider.shape.getRect();
					var snapX = (rect.x1 + rect.x2) / 2;
					if (Global.level.checkTerrainCollisionOnce(character, snapX - character.pos.x, 0) == null) {
						float? incY = null;
						if (isGround) incY = -10;
						character.changeState(new LadderClimb(ladder, midX, incY));
					}
				}
			}
		}
		if (isGround && player.input.isPressed(Control.Down, player)) {
			character.checkLadderDown = true;
			var ladders = Global.level.getTerrainTriggerList(character, new Point(0, 1), typeof(Ladder));
			if (ladders.Count > 0 && ladders[0].gameObject is Ladder ladder) {
				var rect = ladders[0].otherCollider.shape.getRect();
				var snapX = (rect.x1 + rect.x2) / 2;
				float xDist = snapX - character.pos.x;
				if (MathF.Abs(xDist) < 10 && Global.level.checkTerrainCollisionOnce(character, xDist, 30) == null) {
					var midX = ladders[0].otherCollider.shape.getRect().center().x;
					character.changeState(new LadderClimb(ladder, midX, 30));
					character.stopCamUpdate = true;
				}
			}
			character.checkLadderDown = false;
		}
	}

	public void clampViralSigmaPos() {
		float w = 25;
		float h = 35;
		if (character.pos.y < h) {
			Point destPos = new Point(character.pos.x, h);
			Point lerpPos = Point.lerp(character.pos, destPos, Global.spf * 10);
			character.changePos(lerpPos);
		}
		if (character.pos.x < w) {
			Point destPos = new Point(w, character.pos.y);
			Point lerpPos = Point.lerp(character.pos, destPos, Global.spf * 10);
			character.changePos(lerpPos);
		}

		float rightBounds = Global.level.width - w;
		if (character.pos.x > rightBounds) {
			Point destPos = new Point(rightBounds, character.pos.y);
			Point lerpPos = Point.lerp(character.pos, destPos, Global.spf * 10);
			character.changePos(lerpPos);
		}
	}
 }

public class WarpIn : CharState {
	public bool warpSoundPlayed;
	public float destY;
	public float destX;
	public float startY;
	public Anim? warpAnim;
	bool warpAnimOnce;

	// Sigma-specific
	public bool isSigma { get { return player.isSigma; } }
	public int sigmaRounds;
	public const float yOffset = 200;
	public bool landOnce;
	public bool decloaked;
	public bool addInvulnFrames;
	public bool sigma2Once;
	public WarpIn(bool addInvulnFrames = true) : base("warp_in") {
		this.addInvulnFrames = addInvulnFrames;
		invincible = true;
	}

	public override void update() {
		if (!character.ownedByLocalPlayer) return;
		if (!Global.level.mainPlayer.readyTextOver) return;

		if (warpAnim == null && !warpAnimOnce) {
			warpAnimOnce = true;
			warpAnim = new Anim(character.pos.addxy(0, -yOffset), character.getSprite("warp_beam"), character.xDir, player.getNextActorNetId(), false, sendRpc: true);
			warpAnim.splashable = false;
		}

		if (warpAnim == null) {
			character.visible = true;
			character.frameSpeed = 1;
			if (character.isAnimOver()) {
				character.grounded = true;
				character.pos.y = destY;
				character.pos.x = destX;
				character.changeState(new WarpIdle(player.warpedInOnce || Global.level.joinedLate));
			}
			return;
		}

		if (character.player == Global.level.mainPlayer && !warpSoundPlayed) {
			warpSoundPlayed = true;
			character.playSound("warpin", sendRpc: true);
		}

		float yInc = Global.spf * 450;
		warpAnim.incPos(new Point(0, yInc));

		if ((isSigma || player.isVile) && !landOnce && warpAnim.pos.y >= destY - 1) {
			landOnce = true;
			warpAnim.changePos(new Point(warpAnim.pos.x, destY - 1));
		}

		if (warpAnim.pos.y >= destY) {
			if (!(isSigma || player.isVile) || sigmaRounds > 6) {
				warpAnim.destroySelf();
				warpAnim = null;
			} else {
				sigmaRounds++;
				landOnce = false;
				warpAnim.changePos(new Point(warpAnim.pos.x, destY));
			}
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.stopMoving();
		character.useGravity = false;
		character.visible = false;
		character.frameSpeed = 0;
		destY = character.pos.y;
		destX = character.pos.x;
		startY = character.pos.y;
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		character.visible = true;
		if (warpAnim != null) {
			warpAnim.destroySelf();
		}
		player.warpedInOnce = true;
	}
}


public class WarpIdle : CharState {
	public bool firstSpawn;

	public WarpIdle(bool firstSpawn = false) : base("win") {
		invincible = true;
		this.firstSpawn = firstSpawn;
	}

	public override void update() {
		base.update();

		if ((character.isAnimOver() || character.sprite.loopCount >= 1) &&
			character.health >= character.maxHealth &&
			(character is not Blues blues || blues.shieldHP >= blues.shieldMaxHP)
		) {
			character.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.stopMoving();
		character.useGravity = false;
		specialId = SpecialStateIds.WarpIdle;
		character.invulnTime = firstSpawn ? 5 : 0;
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		character.visible = true;
		character.useGravity = true;
		character.splashable = true;
		specialId = SpecialStateIds.None;
		if (character.ownedByLocalPlayer) {
			character.invulnTime = firstSpawn ? 2 : 0;
		}
	}
}

public class WarpOut : CharState {
	public bool warpSoundPlayed;
	public float destY;
	public float startY;
	public Anim? warpAnim;
	public const float yOffset = 200;
	public bool isSigma { get { return player.isSigma; } }
	public bool is1v1MaverickStart;

	public WarpOut(bool is1v1MaverickStart = false) : base("warp_beam") {
		this.is1v1MaverickStart = is1v1MaverickStart;
	}

	public override void update() {
		if (warpAnim == null) {
			return;
		}
		if (is1v1MaverickStart) {
			return;
		}

		if (character.player == Global.level.mainPlayer && !warpSoundPlayed) {
			warpSoundPlayed = true;
			character.playSound("warpin");
		}

		warpAnim.pos.y -= Global.spf * 1000;

		if (character.pos.y <= destY) {
			warpAnim.destroySelf();
			warpAnim = null;
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.stopMoving();
		character.useGravity = false;
		character.visible = false;
		destY = character.pos.y - yOffset;
		startY = character.pos.y;
		if (!is1v1MaverickStart) {
			warpAnim = new Anim(character.pos, character.getSprite("warp_beam"), character.xDir, player.getNextActorNetId(), false, sendRpc: true);
			warpAnim.splashable = false;
		}
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		if (warpAnim != null) {
			warpAnim.destroySelf();
		}
	}
}

public class Idle : CharState {
	public Idle(string transitionSprite = "") : base("idle", "shoot", "attack", transitionSprite) {
		exitOnAirborne = true;
		attackCtrl = true;
		normalCtrl = true;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		if ((character is RagingChargeX || player.health < 4)) {
			if (Global.sprites.ContainsKey(character.getSprite("weak"))) {
				defaultSprite = "weak";
				if (!inTransition()) {
					sprite = defaultSprite;
					character.changeSpriteFromName("weak", true);
				}
			}
		}
		character.dashedInAir = 0;
	}

	public override void update() {
		base.update();

		if (player.input.isHeld(Control.Left, player) || player.input.isHeld(Control.Right, player)) {
			if (!character.isSoftLocked() && character.canTurn()) {
				if (player.input.isHeld(Control.Left, player)) character.xDir = -1;
				if (player.input.isHeld(Control.Right, player)) character.xDir = 1;
				if (character.canMove()) character.changeState(new Run());
			}
		}

		if (Global.level.gameMode.isOver) {
			if (Global.level.gameMode.playerWon(player)) {
				character.changeState(new Win(), true);
				
			} else {
				if (!character.sprite.name.Contains("lose")) {
					string loseSprite;
					int spriteNum = Helpers.randomRange(1, 3);
					switch(spriteNum) {
						case 2: loseSprite = "lose2"; break;
						case 3: loseSprite = "lose3"; break;
						default: loseSprite = "lose"; break;
					}
					character.changeSpriteFromName(loseSprite, true);
				}
			}
		}
	}
}

public class Run : CharState {
	public Run() : base("run", "run_shoot", "attack") {
		accuracy = 5;
		exitOnAirborne = true;
		attackCtrl = true;
		normalCtrl = true;
	}

	public override void update() {
		base.update();
		var move = new Point(0, 0);
		float runSpeed = character.getRunSpeed();
		if (stateFrames <= 4) {
			runSpeed = 60 * character.getRunDebuffs();
		}
		if (player.input.isHeld(Control.Left, player)) {
			character.xDir = -1;
			if (character.canMove()) move.x = -runSpeed;
		} else if (player.input.isHeld(Control.Right, player)) {
			character.xDir = 1;
			if (character.canMove()) move.x = runSpeed;
		}
		if (move.magnitude > 0) {
			character.move(move);
		} else {
			character.changeToIdleOrFall();
		}
	}
}

public class Crouch : CharState {
	public Crouch(string transitionSprite = ""
	) : base(
		"crouch", "crouch_shoot", "attack_crouch", transitionSprite
	) {
		exitOnAirborne = true;
		attackCtrl = true;
		normalCtrl = true;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
	}

	public override void update() {
		base.update();

		var dpadXDir = player.input.getXDir(player);
		if (dpadXDir != 0) {
			character.xDir = dpadXDir;
		}

		if (!character.grounded || !player.isCrouchHeld()) {
			character.changeState(new CrouchEnd());
			return;
		}
		if (Global.level.gameMode.isOver) {
			if (Global.level.gameMode.playerWon(player)) {
				if (!character.sprite.name.Contains("_win")) {
					character.changeSpriteFromName("win", true);
				}
			} else {
				if (!character.sprite.name.Contains("lose")) {
					character.changeSpriteFromName("lose", true);
				}
			}
		}
	}
}

public class SwordBlock : CharState {
	public SwordBlock() : base("block") {
		superArmor = true;
		exitOnAirborne = true;
		attackCtrl = true;
		normalCtrl = true;
		stunResistant = true;
		immuneToWind = true;
	}

	public override void update() {
		base.update();

		bool isHoldingGuard = (
			player.input.isHeld(Control.WeaponLeft, player) ||
			player.input.isHeld(Control.WeaponRight, player)
		);
		if (!isHoldingGuard) {
			character.changeToIdleOrFall();
			return;
		}
		if (Global.level.gameMode.isOver) {
			if (Global.level.gameMode.playerWon(player)) {
				if (!character.sprite.name.Contains("_win")) {
					character.changeSpriteFromName("win", true);
				}
			} else {
				if (!character.sprite.name.Contains("lose")) {
					character.changeSpriteFromName("lose", true);
				}
			}
		}
	}
}

public class ZeroClang : CharState {
	public int hurtDir;
	public float hurtSpeed;

	public ZeroClang(int dir) : base("clang") {
		hurtDir = dir;
		hurtSpeed = dir * 100;
	}

	public override void update() {
		base.update();
		if (hurtSpeed != 0) {
			hurtSpeed = Helpers.toZero(hurtSpeed, 400 * Global.spf, hurtDir);
			character.move(new Point(hurtSpeed, 0));
		}
		/*
		if (this.character.isAnimOver()) {
			this.character.changeToIdleOrFall();
		}
		*/
		if (hurtSpeed == 0) {
			character.changeToIdleOrFall();
		}
	}
}

public class Jump : CharState {
	public Jump() : base("jump", "jump_shoot", Options.main.getAirAttack()) {
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
		if (character.vel.y > 0) {
			if (character.sprite.name.EndsWith("cannon_air") == false) {
				character.changeState(new Fall());
			}
			return;
		}
		if (character is Zero zero) {
			if (zero.kuuenbuJump >= 1) {
				zero.kuuenbuJump = 0;
				character.changeSpriteFromName("kuuenbu", true);
			}
		}
		if (character is PunchyZero pzero) {
			if (pzero.kuuenbuJump >= 1) {
				pzero.kuuenbuJump = 0;
				character.changeSpriteFromName("kuuenbu", true);
			}
		}
	}
}

public class Fall : CharState {
	public float limboVehicleCheckTime;
	public Actor? limboVehicle;

	public Fall() : base("fall", "fall_shoot", Options.main.getAirAttack(), "fall_start") {
		accuracy = 5;
		exitOnLanding = true;
		useDashJumpSpeed = true;
		airMove = true;
		canStopJump = false;
		attackCtrl = true;
		normalCtrl = true;
	}

	public override void update() {
		base.update();
		if (limboVehicleCheckTime > 0) {
			limboVehicleCheckTime -= Global.spf;
			if (limboVehicle?.destroyed == true || limboVehicleCheckTime <= 0) {
				limboVehicleCheckTime = 0;
				character.useGravity = true;
				character.limboRACheckCooldown = 1;
				limboVehicleCheckTime = 0;
			}
		}
	}

	public void setLimboVehicleCheck(Actor limboVehicle) {
		if (limboVehicleCheckTime == 0 && character.limboRACheckCooldown == 0) {
			this.limboVehicle = limboVehicle;
			limboVehicleCheckTime = 1;
			character.stopMoving();
			character.useGravity = false;
			if (limboVehicle is RideArmor ra) {
				//RPC.checkRAEnter.sendRpc(player.id, ra.netId, ra.neutralId, ra.raNum);
			} else if (limboVehicle is RideChaser rc) {
				//RPC.checkRCEnter.sendRpc(player.id, rc.netId, rc.neutralId);
			}
		}
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		character.useGravity = true;
	}
}

public class Dash : CharState {
	public float dashTime = 0;
	public string initialDashButton;
	public int initialDashDir;
	public bool stop;
	public Anim dashSpark;
	public bool isColliding;

	public Dash(string initialDashButton) : base("dash", "dash_shoot", "attack_dash") {
		enterSound = "slide";
		this.initialDashButton = initialDashButton;
		accuracy = 10;
		//exitOnAirborne = true;
		attackCtrl = true;
		normalCtrl = true;
		enterSound = "dash";
		enterSoundArgs = "larmor";
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		initialDashDir = character.xDir;
		character.isDashing = true;
	}

	public override void update() {
		base.update();
		isColliding = Global.level.checkTerrainCollisionOnce(character, 0, -10) != null;

		if (!player.isAI && !player.input.isHeld(initialDashButton, player) && !stop && !isColliding) {
			dashTime = 800;
		}
		float speedModifier = 1;
		float distanceModifier = 1;
		float inputXDir = player.input.getInputDir(player).x;
		if (dashTime > 32 * distanceModifier || stop) {
			if (!stop) {
				dashTime = 0;
				character.frameIndex = 0;
				character.sprite.frameTime = 0;
				character.sprite.animTime = 0;
				character.sprite.frameSpeed = 0.1f;
				stop = true;
			} else {
				if (inputXDir != 0 && character.grounded) {
					character.changeState(new Run(), true);
				} else {
					character.changeState(new DashEnd());
				}
				return;
			}
		}
		if (dashTime > 3 || stop) {
			var move = new Point(0, 0);
			move.x = character.getDashSpeed() * initialDashDir * speedModifier;
			character.move(move);
		} else {
			var move = new Point(0, 0);
			move.x = Physics.DashStartSpeed * character.getRunDebuffs() * initialDashDir * speedModifier; ;
			character.move(move);
		}
		if (dashTime <= 3 || stop) {
			if (inputXDir != 0 && inputXDir != initialDashDir) {
				character.xDir = (int)inputXDir;
				initialDashDir = (int)inputXDir;
			}
		}
		dashTime += Global.speedMul;
		if (stateTime > 0.1 && !character.isUnderwater()) {
			stateTime = 0;
			new Anim(
				character.getDashDustEffectPos(initialDashDir),
				"dust", initialDashDir, player.getNextActorNetId(), true,
				sendRpc: true
			);
		}
		if (!character.grounded || stop) {
			character.dashedInAir++;
			character.changeState(new DashEnd());
		}
	}
}

public class AirDash : CharState {
	public float dashTime = 0;
	public string initialDashButton;
	public int initialDashDir;
	public bool stop;
	public Anim? dashSpark;

	public AirDash(string initialDashButton) : base("dash", "dash_shoot") {
		this.initialDashButton = initialDashButton;
		accuracy = 10;
		attackCtrl = true;
		enterSound = "airdash";
		enterSoundArgs = "larmor";
	}

	public override void update() {
		base.update();
		if (!player.isAI && !player.input.isHeld(initialDashButton, player) && !stop) {
			dashTime = 50;
		}
		float inputXDir = player.input.getInputDir(player).x;
		float speedModifier = 1;
		float distanceModifier = 1;
		if (character.sprite.name.EndsWith("unpo_grab_dash")) {
			speedModifier = 1.25f;
			distanceModifier = 1.25f;
		}
		if (character is CmdSigma or Doppma) {
			character.frameIndex = 1;
		}
		if (dashTime > Global.spf * 28 * distanceModifier || stop) {
			if (!stop) {
				dashTime = 0;
				stop = true;
				character.frameIndex = 0;
				character.sprite.frameTime = 0;
				character.sprite.animTime = 0;
				character.sprite.frameSpeed = 0.1f;
				stop = true;
			} if (inputXDir != 0 && character.grounded) {
				character.changeState(new Run(), true);
			} else {
				character.changeState(new DashEnd());
			}
		}
		if (dashTime > Global.spf * 3 || stop) {
			var move = new Point(0, 0);
			move.x = character.getDashSpeed() * initialDashDir * speedModifier;
			character.move(move);
		} else {
			var move = new Point(0, 0);
			move.x = Physics.DashStartSpeed * character.getRunDebuffs() * initialDashDir * speedModifier;
			character.move(move);
		}
		dashTime += Global.spf;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		initialDashDir = character.xDir;

		if (character is Axl && (character.currentWeapon as AxlWeapon)?.isTwoHanded(false) == true) {
			if (player.input.isHeld(Control.Left, player)) initialDashDir = -1;
			else if (player.input.isHeld(Control.Right, player)) initialDashDir = 1;
		}

		character.isDashing = true;
		character.useGravity = false;
		character.vel = new Point(0, 0);
		character.dashedInAir++;
		new Anim(character.getDashSparkEffectPos(initialDashDir), "dash_sparks", initialDashDir, null, true);
	}

	public override void onExit(CharState? newState) {
		if (!dashSpark?.destroyed == true) {
			dashSpark?.destroySelf();
		}
		character.useGravity = true;
		base.onExit(newState);
	}
}

public class WallSlide : CharState {
	public int wallDir;
	public float dustTime;
	public Collider wallCollider;
	MegamanX? mmx;

	public WallSlide(
		int wallDir, Collider wallCollider
	) : base(
		"wall_slide", "wall_slide_shoot", "wall_slide_attack"
	) {
		this.wallDir = wallDir;
		this.wallCollider = wallCollider;
		accuracy = 2;
		attackCtrl = true;
		enterSound = "wallLand";
		enterSoundArgs = "larmor";
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		mmx = character as MegamanX;
		character.dashedInAir = 0;
		if (player.isAI && character.ai != null) {
			character.ai.jumpTime = 0;
		}
	}

	public override void update() {
		base.update();
		if (character.grounded) {
			character.changeToIdleOrFall();
			return;
		}
		/*
		if (player.input.isPressed(Control.Jump, player)) {
			if (player.input.isHeld(Control.Dash, player)) {
				character.isDashing = true;
			}
			character.vel.y = -character.getJumpPower();
			character.changeState(new WallKick(wallDir * -1));
			return;
		}
		*/
		if (character is CmdSigma && player.input.isPressed(Control.Special1, player) && character.flag == null) {
			int yDir = player.input.isHeld(Control.Down, player) ? 1 : -1;
			character.changeState(new SigmaWallDashState(yDir, false), true);
			return;
		}

		character.useGravity = false;
		character.vel.y = 0;

		/*
		if (wallDir == -1 && wallCollider?.actor?.isPlatform == true)
		{
			float charWidth = character.collider?.shape.getRect().w() ?? 0;
			character.changePos(new Point(wallCollider.shape.getRect().x2 + 1 + charWidth / 2, character.pos.y));
		}

		if (wallDir == 1 && wallCollider?.actor?.isPlatform == true)
		{
			float charWidth = character.collider?.shape.getRect().w() ?? 0;
			character.changePos(new Point(wallCollider.shape.getRect().x1 - 1 - charWidth / 2, character.pos.y));
		}
		*/

		if (stateFrames >= 9) {
			if (mmx == null || mmx.strikeChainProj?.destroyed != false) {
				var hit = character.getHitWall(wallDir, 0);
				var hitWall = hit?.gameObject as Wall;

				if (wallDir != player.input.getXDir(player)) {
					character.changeState(new Fall());
				} else if (hitWall == null || !hitWall.collider.isClimbable) {
					var hitActor = hit?.gameObject as Actor;
					if (hitActor == null || !hitActor.isPlatform) {
						character.changeState(new Fall());
					}
				}
			}
			character.move(new Point(0, 100));
		}

		dustTime += Global.speedMul;
		if (stateFrames > 12 && dustTime > 6) {
			dustTime = 0;
			generateDust(character);
		}
	}

	public override void onExit(CharState? newState) {
		character.useGravity = true;
		base.onExit(newState);
	}

	public static void generateDust(Character character) {
		Point animPoint = character.pos.addxy(12 * character.xDir, 0);
		Rect rect = new Rect(animPoint.addxy(-3, -3), animPoint.addxy(3, 3));
		if (Global.level.checkCollisionShape(rect.getShape(), null) != null) {
			new Anim(animPoint, "dust", character.xDir, character.player.getNextActorNetId(), true, sendRpc: true);
		}
	}
}

public class WallSlideAttack : CharState {
	public int wallDir;
	public float dustTime;
	public Collider wallCollider;
	public bool exitOnAnimEnd;
	public bool canCancel;

	public WallSlideAttack(string anim, int wallDir, Collider wallCollider) : base(anim) {
		this.wallDir = wallDir;
		this.wallCollider = wallCollider;
		useGravity = false;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.dashedInAir = 0;
	}

	public override void update() {
		base.update();
		if (canCancel && (character.grounded || player.input.getXDir(player) != wallDir)) {
			character.changeToIdleOrFall();
			return;
		}
		if (!character.grounded) {
			character.move(new Point(0, 100));
			dustTime += Global.speedMul;
		}
		if (stateFrames > 12 && dustTime > 6) {
			dustTime = 0;
			WallSlide.generateDust(character);
		}
		if (exitOnAnimEnd && character.isAnimOver()) {
			WallSlide wallSlideState = new WallSlide(wallDir, wallCollider) { enterSound = "", stateFrames = 14 };
			character.changeState(wallSlideState);
			character.sprite.frameIndex = character.sprite.totalFrameNum - 1;
			return;
		}
	}
}

public class WallKick : CharState {
	public WallKick() : base("wall_kick", "wall_kick_shoot") {
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
		if (character.vel.y > 0) {
			character.changeState(new Fall());
		}
	}
}

public class LadderClimb : CharState {
	public Ladder ladder;
	public float snapX;
	public float? incY;
	public LadderClimb(
		Ladder ladder, float snapX, float? incY = null
	) : base(
		"ladder_climb", "ladder_shoot", "ladder_attack", "ladder_start"
	) {
		this.ladder = ladder;
		this.snapX = MathF.Round(snapX);
		this.incY = incY;
		attackCtrl = true;
		immuneToWind = true;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.changePos(new Point(snapX, character.pos.y));

		if (incY != null) {
			character.incPos(new Point(0, (float)incY));
		}

		if (character.player == Global.level.mainPlayer) {
			Global.level.lerpCamTime = 0.25f;
		}
		character.stopMoving();
		character.useGravity = false;
		character.dashedInAir = 0;
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		character.frameSpeed = 1;
		character.useGravity = true;
	}

	public override void update() {
		base.update();
		character.changePos(new Point(snapX, character.pos.y));
		character.xPushVel = 0;
		character.vel.x = 0;
		character.vel.y = 0;
		if (inTransition() || character.rootTime > 0) {
			return;
		}
		bool isAttacking = (
			character.sprite.name != character.getSprite("ladder_climb")
		);

		if (isAttacking) {
			character.frameSpeed = 1;
		} else {
			character.frameSpeed = 0;
		}
		if (!isAttacking && character.canClimbLadder()) {
			if (player.input.isHeld(Control.Up, player)) {
				character.move(new Point(0, character.getClimbLadderSpeed() * -1));
				//character.vel.y = character.getClimbLadderSpeed() * -1;
				character.frameSpeed = 1;
			} else if (player.input.isHeld(Control.Down, player)) {
				character.move(new Point(0, character.getClimbLadderSpeed()));
				//character.vel.y = character.getClimbLadderSpeed();
				character.frameSpeed = 1;
			}
		}

		var ladderTop = ladder.collider.shape.getRect().y1;
		var yDist = character.physicsCollider.shape.getRect().y2 - ladderTop;
		if (!ladder.collider.isCollidingWith(character.physicsCollider) || MathF.Abs(yDist) < 12) {
			if (player.input.isHeld(Control.Up, player)) {
				var targetY = ladderTop - 1;
				if (Global.level.checkTerrainCollisionOnce(character, 0, targetY - character.pos.y) == null && MathF.Abs(targetY - character.pos.y) < 20) {
					character.changeState(new LadderEnd(targetY));
				}
			} else {
				character.changeState(new Fall());
			}
		} else if (!player.isAI && player.input.isPressed(Control.Jump, player)) {
			if (!isAttacking) {
				dropFromLadder();
			}
		}

		if (character.grounded) {
			character.changeToIdleOrFall();
		}
	}

	// AI should call this manually when they want to drop from a ladder
	public void dropFromLadder() {
		character.changeState(new Fall());
	}
}

public class LadderEnd : CharState {
	public float targetY;
	public LadderEnd(float targetY) : base("ladder_end") {
		this.targetY = targetY;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.useGravity = false;
		character.stopMoving();
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		character.useGravity = true;
	}

	public override void update() {
		base.update();
		character.xPushVel = 0;
		character.vel.x = 0;
		character.vel.y = 0;
		if (character.isAnimOver()) {
			if (character.player == Global.level.mainPlayer) {
				Global.level.lerpCamTime = 0.25f;
			}
			//this.character.pos.y = this.targetY;
			character.incPos(new Point(0, targetY - character.pos.y));
			character.stopCamUpdate = true;
			character.grounded = true;
			character.changeToIdleOrFall();
		}
	}
}

public class Taunt : CharState {
	float tauntTime = 1;
	Anim? zeroching;
	bool finishedMatch;
	public Taunt(bool finishedMatch = false) : base("win") {
		this.finishedMatch = finishedMatch;
		airMove = true;
	}

	public override bool canEnter(Character character){
		if (character.charState is Slide) return false;
		return base.canEnter(character);
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		if (player.charNum == 0) tauntTime = 0.75f;
		if (player.charNum == 3) tauntTime = 0.75f;
		if (player.charNum == (int)CharIds.Blues) tauntTime = 1.25f;

		bool air = !character.grounded || character.vel.y < 0;
        defaultSprite = sprite;
        landSprite = "win";
        if (air) {
			sprite = "win_air";
			defaultSprite = sprite;
		}
        character.changeSpriteFromName(sprite, true);
		if (air && finishedMatch) character.useGravity = false;
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		zeroching?.destroySelf();
	}

	public override void update() {
		base.update();

		if (finishedMatch) {
			character.useGravity = false;
			character.stopMoving();
		}

		if (player.charNum == 2) {
			if (character.isAnimOver()) {
				character.changeToIdleOrFall();
			}
		} else if (stateTime >= tauntTime && !finishedMatch) {
			character.changeToIdleOrFall();
		}
		if (player.charNum == (int)CharIds.Zero || player.charNum == (int)CharIds.PunchyZero) {
			character.changeSprite("zero_taunt", true);
			if (character.isAnimOver()) {
				character.changeToIdleOrFall();
			} 
		}
		if (character.sprite.name == "bzero_win" && character.frameIndex == 1 && !once) {
			once = true;
			character.playSound("ching", sendRpc: true);
			zeroching = new Anim(
				character.pos.addxy(character.xDir, -25f),
				"zero_ching", -character.xDir,
				player.getNextActorNetId(),
				destroyOnEnd: true, sendRpc: true
			);
		}
		if ((character.sprite.name == "zero_win" || character.sprite.name == "zero_taunt") && character.frameIndex == 6 && !once) {
			once = true;
			character.playSound("ching", sendRpc: true);
			zeroching = new Anim(
				character.pos.addxy(character.xDir * -7, -28f),
				"zero_ching", -character.xDir,
				player.getNextActorNetId(),
				destroyOnEnd: true, sendRpc: true
			);
		}
	}
}

public class Win : CharState {
	public Win() : base("win") {
		normalCtrl = false;
		attackCtrl = false;
		useGravity = false;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		bool air = !character.grounded || character.vel.y < 0;
        defaultSprite = sprite;
        landSprite = "win";
        if (air) {
			sprite = "win_air";
			defaultSprite = sprite;
		}
        character.changeSpriteFromName(sprite, true);
	}

	public override void update() {
		character.stopMoving();
		character.useGravity = false;
	}
}

public class Die : CharState {
	int frames;
	public bool hidden;
	public bool respawnTimerOn;

	public Die() : base("die") {
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.useGravity = false;
		character.stopMoving();
		character.stopCharge();

		if (character is Blues blues) {
			blues.delinkStarCrash();
		}
		player.lastDeathPos = character.getCenterPos();
	}

	public override void onExit(CharState newState) {
		character.visible = true;
		base.onExit(newState);
	}

	public override void update() {
		character.xPushVel = 0;
		character.vel.x = 0;
		character.vel.y = 0;
		if (!respawnTimerOn) {
			player.respawnTime = player.getRespawnTime();
		}
		base.update();

		if (!hidden && stateTime >= 0.75f) {
			hidden = true;
			character.visible = false;
			new DieEffect(character.getCenterPos(), player.charNum, true);
			character.playSound("die", sendRpc: true);
		}
		if (!character.ownedByLocalPlayer) {
			return;
		}
		if (!respawnTimerOn && frames >= 60) {
			player.startDeathTimer();
			if (player.getRespawnTime() <= 3 && !character.destroyed) {
				player.destroyCharacter(true);
			}
		}
		if (stateTime >= 2 && frames >= 60 && !character.destroyed) {
			player.destroyCharacter(true);
		}
		frames++;
	} 

	public void destroyRideArmor() {
		if (character.linkedRideArmor != null) {
			character.linkedRideArmor.selfDestructTime = Global.spf;
			RPC.actorToggle.sendRpc(character.linkedRideArmor.netId, RPCActorToggleType.StartMechSelfDestruct);
		}
	}

	public override bool canExit(Character character, CharState newState) {
		if (newState is not BluesRevive and not NetLimbo { allowResurection: true }) {
			return false;
		}
		return base.canExit(character, newState);
	}
}

public class GenericGrabbedState : CharState {
	public Actor grabber;
	public long savedZIndex;
	public string grabSpriteSuffix;
	public bool reverseZIndex;
	public bool freeOnHitWall;
	public bool lerp;
	public bool freeOnGrabberLeave;
	public string additionalGrabSprite;
	public float notGrabbedTime;
	public float maxNotGrabbedTime;
	public bool customUpdate;
	public GenericGrabbedState(
		Actor grabber, float maxGrabTime, string grabSpriteSuffix,
		bool reverseZIndex = false, bool freeOnHitWall = true,
		bool lerp = true, string additionalGrabSprite = "", float maxNotGrabbedTime = 0.5f
	) : base(
		"grabbed"
	) {
		this.isGrabbedState = true;
		this.grabber = grabber;
		grabTime = maxGrabTime;
		this.grabSpriteSuffix = grabSpriteSuffix;
		this.reverseZIndex = reverseZIndex;
		//Don't use this unless absolutely needed, it causes issues with octopus grab in FTD
		//this.freeOnHitWall = freeOnHitWall;
		this.lerp = lerp;
		this.additionalGrabSprite = additionalGrabSprite;
		this.maxNotGrabbedTime = maxNotGrabbedTime;
	}

	public override void update() {
		base.update();
		if (!character.ownedByLocalPlayer) { return; }
		if (customUpdate) return;

		if (grabber.sprite.name.EndsWith(grabSpriteSuffix) == true || (
				!string.IsNullOrEmpty(additionalGrabSprite) &&
				grabber.sprite.name.EndsWith(additionalGrabSprite) == true
			)
		) {
			bool didNotHitWall = trySnapToGrabPoint(lerp);
			if (!didNotHitWall && freeOnHitWall) {
				character.changeToIdleOrFall();
				return;
			}
		} else {
			notGrabbedTime += Global.spf;
			if (notGrabbedTime > maxNotGrabbedTime) {
				character.changeToIdleOrFall();
				return;
			}
		}

		grabTime -= player.mashValue();
		if (grabTime <= 0) {
			character.changeToIdleOrFall();
		}
	}

	public bool trySnapToGrabPoint(bool lerp) {
		Point grabberGrabPoint = grabber.getFirstPOIOrDefault("g");
		Point victimGrabOffset = character.pos.subtract(character.getFirstPOIOrDefault("g", 0));

		Point destPos = grabberGrabPoint.add(victimGrabOffset);
		if (character.pos.distanceTo(destPos) > 25) lerp = true;
		Point lerpPos = lerp ? Point.lerp(character.pos, destPos, 0.25f) : destPos;

		var hit = Global.level.checkTerrainCollisionOnce(character, lerpPos.x - character.pos.x, lerpPos.y - character.pos.y);
		if (hit?.gameObject is Wall) {
			return false;
		}

		character.changePos(lerpPos);
		return true;
	}

	public override bool canEnter(Character character) {
		if (!base.canEnter(character)) {
			return false;
		}
		return !character.isInvulnerable() && !character.charState.invincible;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.stopMoving();
		//character.stopCharge();
		character.useGravity = false;
		character.grounded = false;
		savedZIndex = character.zIndex;
		if (!reverseZIndex) character.setzIndex(grabber.zIndex - 100);
		else character.setzIndex(grabber.zIndex + 100);
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		character.grabInvulnTime = 2;
		character.useGravity = true;
		character.setzIndex(savedZIndex);
	}
}

public class NetLimbo : CharState {
	public bool allowResurection;

	public NetLimbo() : base("not_a_real_sprite") {

	}
}
public class Land : CharState {
	public Land() : base("land", "land_shoot") {
		attackCtrl = true;
		normalCtrl = true;
		enterSound = "land";
	}

	public override void update() {
		base.update();
		if (player.input.isHeld(Control.Left, player)
		|| player.input.isHeld(Control.Right, player)
		) {
			character.changeState(new Run(), false);
		}
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}
	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
	}
}
public class CrouchStart : CharState {
	public CrouchStart() : base("crouch_start", "crouch_start_shoot") {
		exitOnAirborne = true;
		attackCtrl = true;
		normalCtrl = true;
	}
	public override bool canEnter(Character character) {
		if (character.charState is Crouch) return false;
		return base.canEnter(character);
	}
	public override void update() {
		base.update();
		if (character.isAnimOver()) {
			character.changeState(new Crouch());
		}
	}
}
public class CrouchEnd : CharState {
	public CrouchEnd() : base("crouch_start", "crouch_start_shoot") {
		exitOnAirborne = true;
		attackCtrl = true;
		normalCtrl = true;
	}
	public override void update() {
		base.update();
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}
}
/*
public class DashEnd : CharState {
	public DashEnd() : base("dash_end", "dash_end_shoot") {
		attackCtrl = true;
		normalCtrl = true;
		useDashJumpSpeed = true;
		airMove = true;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		if (character is Doppma doppma && !doppma.grounded) {
			character.changeSprite("sigma3_fall", false);
			exitOnLanding = true;
		}
		if (character is CmdSigma cmdSigma && !cmdSigma.grounded) {
			character.changeSprite("sigma_fall", false);
			exitOnLanding = true;
		}
		if (player.input.isHeld(Control.Left, player) || player.input.isHeld(Control.Right, player)) {
			character.frameTime = 6;
		}
	}

	public override void update() {
		base.update();

		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}
}
*/
