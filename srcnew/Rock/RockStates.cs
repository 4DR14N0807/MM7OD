using System;

namespace MMXOnline;

public class Slide : CharState {
	public float slideTime = 0;
	public string initialSlideButton;
	public int initialSlideDir;
	public bool stop;
	public int particles = 3;
	Anim? dust;
	Rock rock = null!;

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
		rock = character as Rock ?? throw new NullReferenceException();
		initialSlideDir = character.xDir;
	}

	public override void update() {
		base.update();

		float inputXDir = player.input.getInputDir(player).x;
		bool cancel = player.input.isPressed(getOppositeDir(initialSlideDir), player);

		if (Global.level.checkTerrainCollisionOnce(character, 0, -16) != null && rock != null) {
			rock.isSlideColliding = true;
		}
		else if (rock != null) {
			rock.isSlideColliding = false;
		}
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
		if (rock != null) character.moveXY(rock.getSlideSpeed() * initialSlideDir, 0);

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
		normalCtrl = true;
		attackCtrl = true;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
	}

	public override void update() {
		base.update();

		//groundCodeWithMove()

		if (stateFrames >= 5 || player.input.getXDir(player) != 0) {
			character.changeToIdleOrFall();
			return;
		}
	}
}

public class ShootAltRock : CharState {
	Rock rock = null!;
	Weapon stateWeapon;
	bool fired;
	int chargeLv;
	bool isUnderwaterSW;
	public ShootAltRock(
		Weapon stateWeapon, int chargeLv, bool underwater = false
	) : base(
		underwater ? "shoot_swell" : "shoot2"
	) {
		normalCtrl = false;
		airMove = true;
		canStopJump = true;
		this.stateWeapon = stateWeapon;
		this.chargeLv = chargeLv;
		landSprite = underwater ? "shoot_swell" : "shoot2";
		fallSprite = landSprite;
		airSprite = isUnderwaterSW ? "shoot_swell_air" : "shoot2_air";
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		rock = character as Rock ?? throw new NullReferenceException();
		if (character.isUnderwater() && stateWeapon is ScorchWheel) {
			character.changeSpriteFromName("shoot_swell", true);
		}

		isUnderwaterSW = character.isUnderwater() && stateWeapon is ScorchWheel;
		bool air = !character.grounded || character.vel.y < 0;
		if (air) character.changeSpriteFromName(airSprite, true);
	}

	public override bool canEnter(Character character) {
		if (character.isBurnState) return false;
		return base.canEnter(character);
	}

	public override void update() {
		base.update();
		float offset = character.xDir < 0 ? 20 : 0;
		Point centerPos = character.getCenterPos();
		centerPos.x += offset;

		if (stateWeapon is JunkShield) {
			if (!fired && character.frameIndex == 2) {
				stateWeapon.getProjs(rock, new int[] {});
				fired = true;
			}
		} else if (stateWeapon is ScorchWheel) {
			if (!fired && character.currentFrame.getBusterOffset() != null) {
				fired = true;
				stateWeapon.getProjs(rock, new int[] {});
			}
		} else if (stateWeapon is WildCoil) {
			if (!fired && character.currentFrame.getBusterOffset() != null) {
				fired = true;	
				stateWeapon.getProjs(rock , new int[] {chargeLv});
			}
		} else {
			if (!fired && character.frameIndex >= 6) {
				fired = true;
			}
		}

		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		} else {
			if ((character.grounded) && player.input.isPressed(Control.Jump, player) && character.canJump()) {
				character.vel.y = -character.getJumpPower();
				sprite = isUnderwaterSW ? "shoot_swell_air" : "shoot2_air";
				character.changeSpriteFromName(sprite, false);
			}
		}
	}
}


public class ShootAltLadder : CharState {
	Weapon ladderWeapon;
	bool fired;
	int chargeLv;
	Ladder ladder;
	Rock rock = null!;

	//Adrián: This is used for weapons with different shoot anims (Wild Coil, Junk Shield, etc) while being in a ladder.
	public ShootAltLadder(Ladder ladder, Weapon ladderWeapon, int chargeLv, bool underwater = false) : 
	base(underwater ? "ladder_shoot_swell" : "ladder_shoot2") {
		normalCtrl = false;
		this.ladderWeapon = ladderWeapon;
		this.chargeLv = chargeLv;
		this.ladder = ladder;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.stopMoving();
		character.useGravity = false;
		rock = character as Rock ?? throw new NullReferenceException();
	}
	public override void update() {
		base.update();

		var midX = ladder.collider.shape.getRect().center().x;
		float offset = character.xDir < 0 ? 20 : 0;
		Point centerPos = character.getCenterPos();
		centerPos.x += offset;

		if (ladderWeapon is JunkShield) {
			if (!fired && character.frameIndex == 1) {
				ladderWeapon.getProjs(rock, new int[] {});
				fired = true;
			}
		} else if (ladderWeapon is ScorchWheel) {
			if (!fired && character.currentFrame.getBusterOffset() != null) {
				fired = true;
				ladderWeapon.getProjs(rock, new int[] {});
			}
		} else if (ladderWeapon is WildCoil) {
			if (!fired && character.currentFrame.getBusterOffset() != null) {
				fired = true;
				ladderWeapon.getProjs(rock , new int[] {chargeLv});
			}
		} else {
			if (!fired && character.frameIndex >= 6) {
				fired = true;
			}
		}

		if (character.isAnimOver()) character.changeState(new LadderClimb(ladder, midX), true);
	}
}


public class RockDoubleJump : CharState {

	public float time = 0;
	Anim? anim;
	public const float jumpSpeedX = 120;
	public const float jumpSpeedY = -180;

	public RockDoubleJump() : base("doublejump", "doublejump_shoot", "", "") {
		useGravity = false;
		enterSound = "super_adaptor_jump";
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		//character.vel = new Point(jumpSpeedX * character.xDir, jumpSpeedY);
		anim = new Anim(character.pos, "sa_double_jump_effect", character.xDir, player.getNextActorNetId(), false, true, zIndex: ZIndex.Character - 1);
		//Global.playSound("super_adaptor_jump");
	}

	public override void onExit(CharState? oldState) {
		base.onExit(oldState);
		anim?.destroySelf();
	}


	public override void update() {
		base.update();


		if (anim != null) anim.changePos(character.pos);

		/* CollideData? collideData = Global.level.checkTerrainCollisionOnce(character, character.xDir, 0);
		if (collideData != null && character.ownedByLocalPlayer) {
			character.move(new Point(0, jumpSpeedY));
		} */
		character.move(new Point(character.xDir * jumpSpeedX, 0));
		character.move(new Point(0, jumpSpeedY));

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

	public CallDownRush() : base("sa_activate", "", "", "") {
		invincible = true;
		normalCtrl = false;
		attackCtrl = false;
		useGravity = false;
	}

	public override void update() {
		base.update();

		if (stateFrames >= 240) character.changeState(new Idle(), true);

		if (rush != null) {

			switch (phase) {
				case 0: //Rush Call
					if (rush.pos.y < character.getCenterPos().y) {
						rush.vel.y = 480;
					} else phase = 1;
					break;

				case 1: //Rush Land
					rush.vel = new Point();
					rush.changeSprite("rush_warp_in", false);

					if (rush.isAnimOver()) phase = 2;
					break;

				case 2: //Rush Jumps and stops above the player
						//TO DO: Improve this part :derp:
					var rockPos = character.pos;
					rockPos.y -= 64;
					jumpTime++;
					rush.changeSprite("sa_rush_jump", false);
					rush.move(rush.pos.directionToNorm(rockPos).times(300));
					if (rush.pos.distanceTo(rockPos) < 10) isXAllign = true;
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
					rush.changeSprite("sa_rush_transform", false);

					if (rush.isAnimOver()) {
						jumpTime = 0;
						phase = 4;
					}
					break;

				case 4:
					rush.vel.y = 420;
					jumpTime++;

					if (jumpTime >= 6) {
						rush.destroySelf();
						new Anim(character.pos, "sa_activate_effect", 1, character.player.getNextActorNetId(), true, true);
						string endSprite = character.grounded ? "rock_sa_activate_end" : "rock_sa_activate_end_air";
						character.changeSprite(endSprite, true);
						Global.playSound("super_adaptor_activate");
						phase = 5;
					}
					break;

				case 5:
					if (character.sprite.name.Contains("sa_activate_end") && character.isAnimOver()) {
						(character as Rock)?.setSuperAdaptor(true);

						character.changeToIdleOrFall();
					}
					break;
			}
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.stopMoving();
		bool air = !character.grounded || character.vel.y < 0;
		if (air) character.changeSpriteFromName("sa_activate_air", true);
		rush = new Anim(new Point(character.pos.x + (30 * character.xDir), character.pos.y - rushAnimStartPos),
		"rush_warp_beam", -character.xDir, player.getNextActorNetId(), false, true);
		Global.playSound("warpin");
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		character.invulnTime = 0.5f;
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
	
	Rock rock = null!;
	public RushJetRide() : base("idle", "shoot") {
		normalCtrl = false;
		attackCtrl = true;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		rock = character as Rock ?? throw new NullReferenceException();
		//float rushPosX = rock.rush.getCenterPos().x;
		//character.changePos(new Point(rushPosX, rock.rush.pos.y - 16));
	}

	public override void update() {
		base.update();

		if (player.input.isPressed(Control.Jump, player) || !character.grounded) character.changeState(new Jump(), true);
	}

}
