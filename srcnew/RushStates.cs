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
	public virtual void update() { }
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
	
	public RushWarpIn(bool addInvulnFrames = true) : base("empty") { }

	public override void onEnter(RushState oldState) {
		base.onEnter(oldState);
		rush.stopMoving();
		rush.useGravity = false;
		rush.frameSpeed = 0;
		warpAnim = new Anim(new Point(rush.pos.x, rush.pos.y - 200), "rush_warp_beam", 1, null, false);

		rockPos = rush.netOwner.character.pos;
		Point? checkGround = Global.level.getGroundPosNoKillzone(character.pos);
		rush.pos = checkGround.GetValueOrDefault();
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
		warpAnim.move(warpAnim.pos.directionToNorm(rush.pos).times(180));
	}
}

public class RushIdle : RushState {
	public RushIdle() : base("rush_idle") {

	}
}

public class RushCoil : RushState {
	public RushCoil() : base("rush_coil") { }

	public override void update() {
		base.update();
		if (stateTime >= 90) {
			Global.playSound("ding");
		}
	}
}

public class RushJetState : RushState {
	public RushJetState() : base("rush_jet") { }

	public override void onEnter(RushState oldState) {
		base.onEnter(oldState);
		rush.isPlatform = true;
		rush.globalCollider = rush.getJetCollider();
		rush.vel.x = rush.xDir * 60;
		rush.useGravity = false;
	}

	public override void onExit(RushState newState) {
		base.onExit(newState);
		rush.isPlatform = false;
		rush.stopMoving();
	}
}
