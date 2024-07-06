using System;

namespace MMXOnline;

public class Slide : CharState {
	public float slideTime = 0;
	public string initialSlideButton;
	public int initialSlideDir;
	public bool stop;
	public int particles = 3;
	Anim? dust;
	bool isColliding;
	Rock? rock;

	public Slide(string initialSlideButton) : base("slide", "", "") {
		enterSound = "slide";
		this.initialSlideButton = initialSlideButton;
		accuracy = 10;
		exitOnAirborne = true;
		attackCtrl = true;
		normalCtrl = true;
		useDashJumpSpeed = false;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		rock = character as Rock;
		initialSlideDir = character.xDir;
		if (rock != null) character.globalCollider = rock.getSlidingCollider();	
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
	}

	public override void update() {
		base.update();

		float inputXDir = player.input.getInputDir(player).x;
		bool cancel = player.input.isPressed(getOppositeDir(initialSlideDir), player);

		if (Global.level.checkCollisionActor(character, 0, -24) != null && rock != null) rock.isSlideColliding = true;
		else if (rock != null) rock.isSlideColliding = false;
		
		if ((slideTime > 30 || stop) && rock != null && !rock.isSlideColliding) {
			if (!stop) {
				slideTime = 0;
				character.frameIndex = 0;
				character.sprite.frameTime = 0;
				character.sprite.animTime = 0;
				character.sprite.frameSpeed = 0.1f;
				stop = true;
			}
			character.changeState(new SlideEnd(), true);
			return;
		}

		var move = new Point(0, 0);
		if (rock != null) move.x = rock.getSlideSpeed() * initialSlideDir;
		character.move(move);

		if (cancel) {
			if (rock != null && rock.isSlideColliding) {
				character.xDir *= -1;
				initialSlideDir *= -1;
			} else character.changeState(new SlideEnd(), true);
		}

		slideTime++;
		if (stateFrames >= 3 && particles > 0) {
			stateFrames = 0;
			particles--;
			dust = new Anim(
				character.getDashDustEffectPos(initialSlideDir),
				"dust", initialSlideDir, player.getNextActorNetId(), true,
				sendRpc: true
			);
			dust.vel.y = (-particles - 1) * 20;
		}
	}

	string getOppositeDir(float inputX) {
		if (inputX == -1) return Control.Right;
		else return Control.Left;
	}
}

public class SlideEnd : CharState {

	public SlideEnd() : base("slide_end", "shoot", "") {
		attackCtrl = true;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
	}

	public override void update() {
		base.update();

		float inputXDir = player.input.getInputDir(player).x;
		bool canMove = character.player.input.isHeld(Control.Left, player) ||
		character.player.input.isHeld(Control.Right, player);

		if (stateFrames >= 5 || canMove) {
			character.changeToIdleOrFall();
			return;
		}
	}
}

public class ShootAlt : CharState {

	Weapon stateWeapon;
	bool fired;
	int chargeLv;
	public ShootAlt(Weapon stateWeapon, int chargeLv) : base("slashclaw", "", "", "") {
		normalCtrl = false;
		airMove = true;
		canStopJump = true;
		this.stateWeapon = stateWeapon;
		this.chargeLv = chargeLv;

		if (stateWeapon is ScorchWheel || stateWeapon is JunkShield || stateWeapon is WildCoil) base.sprite = "shoot2";
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		bool air = !character.grounded || character.vel.y < 0;
		//sprite = stateWeapon is SlashClawWeapon ? "slashclaw" : "shoot2";
		defaultSprite = sprite;
		landSprite = stateWeapon is SlashClawWeapon ? "slashclaw" : "shoot2";
		if (air) {
			sprite = stateWeapon is SlashClawWeapon ? "slashclaw_air" : "shoot2_air";
			defaultSprite = sprite;
		}
		character.changeSpriteFromName(sprite, true);
	}

	public override bool canEnter(Character character) {
		if (character.charState is Burning) return false;
		return base.canEnter(character);
	}

	public override void update() {
		base.update();
		float offset = character.xDir < 0 ? 20 : 0;
		Point centerPos = character.getCenterPos();
		centerPos.x += offset;

		if (stateWeapon is JunkShield) {
			if (!fired && character.frameIndex == 2) {
				fired = true;
				new JunkShieldProj(new JunkShield(), character.getCenterPos(), character.getShootXDir(), player, player.getNextActorNetId(true), true);
			}
		} else if (stateWeapon is ScorchWheel) {
			if (!fired && character.currentFrame.getBusterOffset() != null) {
				fired = true;

				if (character.isUnderwater()) {
					new UnderwaterScorchWheelProj(new ScorchWheel(), character.getCenterPos(), character.getShootXDir(), player, player.getNextActorNetId(true), rpc: true);
				} else {
					new ScorchWheelSpawn(new ScorchWheel(), character.getCenterPos(), character.getShootXDir(), player, player.getNextActorNetId(true), rpc: true);
					//new ScorchWheelProj(new ScorchWheel(), new Point(character.getCenterPos().x - offset, character.getCenterPos().y), character.getShootXDir(), player, player.getNextActorNetId(true), rpc: true);
				}
			}
		} else if (stateWeapon is WildCoil) {
			if (!fired && character.currentFrame.getBusterOffset() != null) {

				fired = true;
				var pois = character.currentFrame.POIs;
				Point? shootPos = character.getFirstPOI();

				if (chargeLv >= 2) {
					new WildCoilChargedProj(new WildCoil(), (Point)character.getFirstPOI(0), character.getShootXDir(), player, 0, player.getNextActorNetId(true), rpc: true);
					new WildCoilChargedProj(new WildCoil(), (Point)character.getFirstPOI(1), character.getShootXDir(), player, 1, player.getNextActorNetId(true), rpc: true);
					player.character.playSound("buster3", sendRpc: true);
				} else {
					new WildCoilProj(new WildCoil(), (Point)character.getFirstPOI(0), character.getShootXDir(), player, 0, player.getNextActorNetId(true), rpc: true);
					new WildCoilProj(new WildCoil(), (Point)character.getFirstPOI(1), character.getShootXDir(), player, 1, player.getNextActorNetId(true), rpc: true);
					player.character.playSound("buster2", sendRpc: true);
				}
			}
		} else {
			if (!fired && character.frameIndex >= 6) {
				fired = true;
			}
		}

		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		} else {
			if ((character.grounded) && player.input.isPressed(Control.Jump, player)) {
				character.vel.y = -character.getJumpPower();
				sprite = stateWeapon is SlashClawWeapon ? "slashclaw_air" : "shoot2_air";
				character.changeSpriteFromName(sprite, false);
			}
		}
	}
}


public class ShootAltLadder : CharState {
	Weapon ladderWeapon;
	bool fired;
	int chargeLv;

	//Adrián: This is used for weapons with different shoot anims (Wild Coil, Junk Shield, etc) while being in a ladder.
	public ShootAltLadder(Weapon ladderWeapon, int chargeLv) : base("ladder_slashclaw", "", "", "") {
		normalCtrl = false;
		this.ladderWeapon = ladderWeapon;
		this.chargeLv = chargeLv;

		if (ladderWeapon is ScorchWheel || ladderWeapon is JunkShield || ladderWeapon is WildCoil) base.sprite = "ladder_shoot2";
		//Adrián: Slash claw anim is the default behavior.
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.stopMoving();
		character.useGravity = false;
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
	}

	public override void update() {
		base.update();

		var ladders = Global.level.getTriggerList(character, 0, 1, null, typeof(Ladder));
		var midX = ladders[0].otherCollider.shape.getRect().center().x;
		float offset = character.xDir < 0 ? 20 : 0;
		Point centerPos = character.getCenterPos();
		centerPos.x += offset;

		if (ladderWeapon is JunkShield) {
			if (!fired && character.frameIndex == 1) {
				fired = true;
				new JunkShieldProj(new JunkShield(), character.getCenterPos(), character.getShootXDir(), player, player.getNextActorNetId(true), true);
			}
		} else if (ladderWeapon is ScorchWheel) {
			if (!fired && character.currentFrame.getBusterOffset() != null) {
				fired = true;
				character.getShootXDir();

				if (character.isUnderwater()) {
					new UnderwaterScorchWheelProj(new ScorchWheel(), character.getCenterPos(), character.getShootXDir(), player, player.getNextActorNetId(true), rpc: true);
				} else {
					new ScorchWheelProj(new ScorchWheel(), character.getCenterPos(), character.getShootXDir(), player, player.getNextActorNetId(true), rpc: true);
				}
			}
		} else if (ladderWeapon is WildCoil) {
			if (!fired && character.currentFrame.getBusterOffset() != null) {

				fired = true;
				var pois = character.currentFrame.POIs;
				Point? shootPos = character.getFirstPOI();

				if (chargeLv >= 2) {
					new WildCoilChargedProj(new WildCoil(), (Point)character.getFirstPOI(0), character.getShootXDir(), player, 0, player.getNextActorNetId(), rpc: true);
					new WildCoilChargedProj(new WildCoil(), (Point)character.getFirstPOI(1), character.getShootXDir(), player, 1, player.getNextActorNetId(), rpc: true);
					player.character.playSound("buster3", sendRpc: true);
				} else {
					new WildCoilProj(new WildCoil(), (Point)character.getFirstPOI(0), character.getShootXDir(), player, 0, player.getNextActorNetId(), rpc: true);
					new WildCoilProj(new WildCoil(), (Point)character.getFirstPOI(1), character.getShootXDir(), player, 1, player.getNextActorNetId(), rpc: true);
					player.character.playSound("buster2", sendRpc: true);
				}
			}
		} else {
			if (!fired && character.frameIndex >= 6) {
				fired = true;
			}
		}

		if (character.isAnimOver()) character.changeState(new LadderClimb(ladders[0].gameObject as Ladder, midX), true);
	}
}


public class RockDoubleJump : CharState {

	public float time = 0;
	Anim? anim;
	public const float jumpSpeedX = 120;
	public const float jumpSpeedY = -180;

	public RockDoubleJump() : base("doublejump", "doublejump_shoot", "", "") {
		enterSound = "super_adaptor_jump";
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.useGravity = false;
		character.vel = new Point(jumpSpeedX * character.xDir, jumpSpeedY);
		anim = new Anim(character.pos, "sa_double_jump_effect", character.xDir, player.getNextActorNetId(), false, true, zIndex: ZIndex.Character - 1);
		//Global.playSound("super_adaptor_jump");
	}

	public override void onExit(CharState oldState) {
		base.onExit(oldState);
		character.vel = new Point();
		character.useGravity = true;
		anim?.destroySelf();
	}


	public override void update() {
		base.update();


		if (anim != null) anim.changePos(character.pos);

		CollideData collideData = Global.level.checkCollisionActor(character, character.xDir, 0);
		if (collideData != null && character.ownedByLocalPlayer) {
			character.move(new Point(0, jumpSpeedY));
		}

		time += Global.spf;
		if (stateFrames > 30 || (stateFrames > 6 && character.player.input.isPressed(Control.Jump, character.player))) {
			character.changeState(new Fall(), true);
		}
	}
}


public class CallDownRush : CharState {

	Anim? rush;
	const float rushAnimStartPos = 200;
	int phase = 0;
	float jumpTime = 0;
	bool isXAllign;
	bool isYAllign;

	public CallDownRush() : base("sa_activate", "", "", "") {
		invincible = true;
		normalCtrl = false;
		attackCtrl = false;
	}

	public override void update() {
		base.update();

		if (stateFrames >= 240) character.changeState(new Idle(), true);

		if (rush != null) {

			switch (phase) {
				case 0: //Rush Call
					if (rush.pos.y < character.pos.y) {
						rush.vel.y = 480;
					} else phase = 1;
					break;

				case 1: //Rush Land
					rush.vel = new Point();
					rush.changeSprite("rush_warp_in", true);

					if (rush.isAnimOver()) phase = 2;
					break;

				case 2: //Rush Jumps and stops above the player
						//TO DO: Improve this part :derp:
					var rockPos = character.pos;
					rockPos.y -= 64;
					jumpTime++;
					rush.changeSprite("sa_rush_jump", true);
					rush.move(rush.pos.directionToNorm(rockPos).times(300));
					if (rush.pos.distanceTo(rockPos) < 10) isXAllign = true;
					//rush.moveToXPos(new Point(character.pos.x, character.pos.y - 32), 60);
					//rush.moveToPos(new Point(character.pos.x, character.pos.y + 64), 120);
					if (isXAllign) phase = 3;
					break;


				/*if (rush.pos.x != character.pos.x){
					//rush.vel.x = -90 * character.xDir;
					
					
				} else {
					rush.vel.x = 0;
					isXAllign = true;
				}

				if (jumpTime < Global.spf * 16) {
					rush.vel.y = -357;
				} else {
					rush.vel.y = 0;
					isYAllign = true;
				}

				if (isXAllign && isYAllign) phase = 3;
				break;*/

				case 3: //Rush transform
					rush.changeSprite("sa_rush_transform", true);

					if (rush.isAnimOver()) {
						jumpTime = 0;
						phase = 4;
					}
					break;

				case 4:
					rush.vel.y = 420;
					jumpTime++;

					if (jumpTime >= 12) {
						rush.destroySelf();
						new Anim(character.pos, "sa_activate_effect", 1, null, true, true);
						string endSprite = character.grounded ? "rock_sa_activate_end" : "rock_sa_activate_end_air";
						character.changeSprite(endSprite, true);
						Global.playSound("super_adaptor_activate");
						phase = 5;
					}
					break;

				case 5:
					if (character.sprite.name.Contains("sa_activate_end") && character.isAnimOver()) {
						character.player.setSuperAdaptor(true);

						character.changeToIdleOrFall();
					}
					break;
			}
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.stopMoving();
		character.useGravity = false;
		bool air = !character.grounded || character.vel.y < 0;
		if (air) character.changeSpriteFromName("sa_activate_air", true);
		rush = new Anim(new Point(character.pos.x + (30 * character.xDir), character.pos.y - rushAnimStartPos),
		"rush_warp_beam", -character.xDir, player.getNextActorNetId(), false, true);
		Global.playSound("warpin");
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		character.useGravity = true;
	}
}


public class CoilJump : CharState {
	public CoilJump() : base("jump", "jump_shoot") {
		accuracy = 5;
		enterSound = "jump";
		exitOnLanding = true;
		useDashJumpSpeed = true;
		airMove = true;
		canStopJump = false;
		attackCtrl = true;
		normalCtrl = true;
	}

	public override void update() {
		base.update();
		if (character.vel.y > 0) {
			character.changeState(new Fall());
			return;
		}
	}
}


public class RushJetRide : CharState {
	
	Rock? rock;
	public RushJetRide() : base("idle", "shoot") {
		normalCtrl = false;
		attackCtrl = true;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		rock = character as Rock;
		float rushPosX = rock.rush.getCenterPos().x;
		character.changePos(new Point(rushPosX, rock.rush.pos.y - 16));
	}

	public override void update() {
		base.update();

		if (player.input.isPressed(Control.Jump, player) || !character.grounded) character.changeState(new Jump(), true);
	}

}
