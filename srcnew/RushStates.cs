using System;
using System.Collections.Generic;

namespace MMXOnline;

public class RushState {
	public string sprite;
	public string defaultSprite;
	public Rush rush = null!;
	public Character character = null!;
	public float stateTime;
	public float stateSeconds => stateTime / Global.secondsFrameDuration;

	public RushState(string sprite) {
		this.sprite = sprite;
		defaultSprite = sprite;
	}

	public Player player => rush.player;

	public virtual void onExit(RushState newState) { }

	public virtual void onEnter(RushState oldState) { }

	public virtual void preUpdate() { }
	public virtual void update() { stateTime++; }
	public virtual void postUpdate() { }

	public virtual bool canEnter(Rush rush) {
		return true;
	}

	public virtual bool canExit(Rush rush, RushState newState) {
		return true;
	}
}

public class RushWarpIn : RushState {
	public Point destY;
	public Point rockPos;
	public Anim warpAnim = null!;
	bool landed;

	public RushWarpIn(bool addInvulnFrames = true) : base("empty") { }

	public override void onEnter(RushState oldState) {
		base.onEnter(oldState);
		rush.stopMoving();
		rush.useGravity = false;
		rush.frameSpeed = 0;

		rockPos = rush.netOwner.character.pos;
		Point? checkGround = Global.level.getGroundPosNoKillzone(character.pos);
		rush.pos = checkGround.GetValueOrDefault();
		warpAnim = new Anim(new Point(rush.pos.x, rush.pos.y - 200), "rush_warp_beam", 1, null, false);
	}

	public override void onExit(RushState newState) {
		base.onExit(newState);
		rush.visible = true;
		rush.useGravity = true;
		rush.splashable = true;
		warpAnim.destroySelf();
	}

	public override void update() {
		base.update();
		warpAnim.move(warpAnim.pos.directionToNorm(rush.pos).times(300));

		if (warpAnim.pos.distanceTo(rush.pos) <= 32) {
			warpAnim.destroySelf();
			rush.changeSprite("rush_warp_in", true);
			landed = true;
		}

		if (landed && rush.isAnimOver()) rush.changeState(new RushIdle());
	}
}

public class RushIdle : RushState {

	int count;
	bool otherAnim;
	public RushIdle() : base("rush_idle") {

	}

	public override void onEnter(RushState oldState) {
		base.onEnter(oldState);
		//rush.isPlatform = true;
	}

	public override void update() {
		base.update();

		if (otherAnim && rush.isAnimOver()) {
			rush.changeSprite("rush_idle", true);
			otherAnim = false;
			stateTime = 0;
		}

		if (stateTime >= 180 && !otherAnim) {
			if (count < 2) rush.changeSprite("rush_look_around", true);
			else if (count < 4) rush.changeSprite("rush_yawn", true);
			else rush.changeState(new RushSleep());
			count++;
			otherAnim = true;
			stateTime = 0;
		}
	}
}


public class RushSleep : RushState {

	int sleepTime;
	bool sleeping;
	
	public RushSleep() : base("rush_sleep_start") {

	}

	public override void update() {
		base.update();

		if (rush.isAnimOver()) {
			rush.changeSprite("rush_sleep", true);
			sleeping = true;
		}

		if (sleeping) sleepTime++;
		if (sleepTime >= 120) rush.changeState(new RushWarpOut());
	}
}

public class RushCoil : RushState {
	public RushCoil() : base("rush_coil") {
	}

	public override void update() {
		base.update();
		if (stateTime >= 90) {
			rush.changeState(new RushWarpOut());
		}
	}
}

public class RushJetState : RushState {
	Rock? rock;
	public RushJetState() : base("rush_jet") { }

	public override void onEnter(RushState oldState) {
		base.onEnter(oldState);
		rush.isPlatform = true;
		rush.globalCollider = rush.getJetCollider();
		rush.vel.x = rush.xDir * 120;
		rush.useGravity = false;
		rock = rush.character as Rock;
	}

	public override void onExit(RushState newState) {
		base.onExit(newState);
		rush.isPlatform = false;
		rush.stopMoving();
	}

	public override void update() {
		base.update();

		if (character.player.input.isHeld(getOppositeDir(rush.xDir), character.player)) {
			rush.vel.x = 60 * rush.xDir;
		} else rush.vel.x = 120 * rush.xDir;
	}

	string getOppositeDir(float inputX) {
		if (inputX == -1) return Control.Right;
		else return Control.Left;
	}
}


public class RushWarpOut : RushState {

	int time;
	bool beam;
	Rock? rock;


	public RushWarpOut() : base("rush_warp_in") {
		
	}

	public override void onEnter(RushState oldState) {
		base.onEnter(oldState);
		rush.frameSpeed = -1;
		rock = rush.character as Rock;
	}

	public override void update() {
		base.update();

		if (rush.frameIndex == 0) {
			rush.changeSprite("rush_warp_beam", true);
			rush.vel.y = -240;
			beam = true;
		}

		if (beam) time++;
		
		if (time >= 60) {
			rush.destroySelf();
			rock.rush = null;
		} 
	}
}
