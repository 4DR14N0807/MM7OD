using System;
using System.Collections.Generic;

namespace MMXOnline;

public class RushWarpIn : RushState {

	public Point destY;
	public Point rockPos;
	public Anim warpAnim;
	public Rock rock;
	
	public RushWarpIn(bool addInvulnFrames = true) : base("rush_idle") {
	}

	public override void onEnter(RushState oldState) {
		base.onEnter(oldState);
		rush.stopMoving();
		rush.useGravity = false;
		rush.frameSpeed = 0;
		rock = rush.netOwner.character as Rock;

		rockPos = rush.netOwner.character.pos;
		rush.globalCollider = null;
		//rush.pos = new Point(rockPos.x + (rush.netOwner.character.xDir * 32), rockPos.y);
		Point? checkGround = Global.level.getGroundPosNoKillzone(rock.pos);
		rush.pos = checkGround.GetValueOrDefault();
		//rush.pos = Global.level.getGroundPosNoKillzone(new Point(rock.pos.x, rock.pos.y));
		warpAnim = new Anim(new Point(rush.pos.x, rush.pos.y - 200), "rush_warp_beam", 1, null, false);
		//warpAnim.vel.y = 180;
		
	}

	public override void onExit(RushState newState) {
		base.onExit(newState);
		rush.visible = true;
		rush.useGravity = true;
		rush.splashable = true;
		/*if (warpAnim != null) {
			warpAnim.destroySelf();
		}*/
	}

	public override void update() {
		base.update();

		if (warpAnim != null) {
			warpAnim.move(warpAnim.pos.directionToNorm(rush.pos).times(180));
		}
	}
}

public class RushState {
	public string sprite;
	public string defaultSprite;
	public string attackSprite;
	public string carrySprite;
	public string transitionSprite;
	public Point busterOffset;
	public Rush rush;
	public Character character;
	public Collider lastLeftWall;
	public Collider lastRightWall;
	public float stateTime;
	public string enterSound;
	public float framesJumpNotHeld = 0;

	public RushState(string sprite, string attackSprite = null, string carrySprite = null, string transitionSprite = null) {
		this.sprite = string.IsNullOrEmpty(transitionSprite) ? sprite : transitionSprite;
		this.transitionSprite = transitionSprite;
		defaultSprite = sprite;
		this.attackSprite = attackSprite;
		this.carrySprite = carrySprite;
		stateTime = 0;
	}

	public Player player {
		get {
			Player charPlayer = character?.player;
			if (charPlayer != null) {
				return charPlayer;
			} 
			return null;
		}
	}

	public virtual void onExit(RushState newState) {
		
	}

	public virtual void onEnter(RushState oldState) {
		//if (!string.IsNullOrEmpty(enterSound)) rush.playSound(enterSound);
	}

	public bool inTransition() {
		return (!string.IsNullOrEmpty(transitionSprite) &&
			sprite == transitionSprite &&
			character?.sprite?.name != null &&
			character.sprite.name.Contains(transitionSprite)
		);
	}

	public virtual void update() {
		stateTime += Global.spf;
	}

	public virtual bool canEnter(Rush rush) {
		/*if (rush.rushState is WarpOut && this is not WarpIn) {
			return false;
		}*/
		return true;
	}

	public virtual bool canExit(Rush rush, RushState newState) {
		return true;
	}
}

public class RushIdle : RushState {
	public RushIdle() : base("rush_idle", "", "", "") {

	}


	public override void onEnter(RushState oldState) {
		
	}
}


public class RushCoil : RushState {

	public RushCoil() : base("rush_coil", "", "", "") {

	}

	public override void update() {
		base.update();

		if (stateTime >= Global.spf * 90) Global.playSound("ding");
	}
}