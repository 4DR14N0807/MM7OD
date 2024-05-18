using System;
using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;

namespace MMXOnline;


public class Rush : Actor {

    public Character? character;
    public Player? player { get { return character?.player; } }
    public RushState? rushState;
    public bool changedStateInFrame;

    public Rush(Player owner, Point pos, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false) :
    base("", pos, netId, ownedByLocalPlayer, true) {

        netOwner = owner;
        changeState(new RushIdle());
    }

    public override void preUpdate() {
		base.preUpdate();
		changedStateInFrame = false;
	}

    public void changeState(RushState newState, bool forceChange = false) {
		if (rushState != null && newState != null && rushState.GetType() == newState.GetType()) return;
		if (changedStateInFrame && !forceChange) return;

		changedStateInFrame = true;
		newState.rush = this;

		changeSprite(newState.sprite, true);

		var oldState = rushState;
		if (oldState != null) oldState.onExit(newState);
		rushState = newState;
		newState.onEnter(oldState);
	}
}

public class RushState {

    public string sprite;
	public string defaultSprite;
	public string attackSprite;
	public string carrySprite;
	public string transitionSprite;
	public Point busterOffset;
	public Rush? rush;
	public Character? character { get { return rush?.character; } }
	public Collider? lastLeftWall;
	public Collider? lastRightWall;
	public float stateTime;
	public string? enterSound;
	public float framesJumpNotHeld = 0;

    public RushState(string sprite, string attackSprite = "", string carrySprite = "", string transitionSprite = "") {
        this.sprite = string.IsNullOrEmpty(transitionSprite) ? sprite : transitionSprite;
		this.transitionSprite = transitionSprite;
		defaultSprite = sprite;
		this.attackSprite = attackSprite;
		this.carrySprite = carrySprite;
		stateTime = 0;
    }

    public Player? player {
		get {
			return character?.player;
		}
	}

    public virtual void onExit(RushState newState) {
	}

	public virtual void onEnter(RushState oldState) {
		if (!string.IsNullOrEmpty(enterSound)) rush.playSound(enterSound);
	}

	public virtual void update() {
		stateTime += Global.spf;
	}
}


public class RushIdle : RushState {
    public RushIdle() : base("rush_idle", "", "", "") {

    }
}


public class RushCallDown : RushState {
    const float warpHeight = 150;
	float origYPos;
	int phase = 0;
	Point summonPos;

    public RushCallDown(Point summonPos) : base("rush_warp_beam"){
        this.summonPos = summonPos;
    }

    public override void update() {
		base.update();
		if (phase == 0) {
			rush.incPos(new Point(0, -Global.spf * 450));
			if (rush.pos.y < origYPos - warpHeight) {
				rush.changePos(summonPos.addxy(0, -warpHeight));
				rush.playSound("warpIn");
				phase = 1;
			}
		} else if (phase == 1) {
			rush.incPos(new Point(0, Global.spf * 450));
		}
	}

    public override void onEnter(RushState oldState) {
		base.onEnter(oldState);
		rush.playSound("warpIn");
		rush.vel = Point.zero;
		rush.xIceVel = 0;
		rush.xPushVel = 0;
		rush.xFlinchPushVel = 0;
		rush.useGravity = false;
		origYPos = rush.pos.y;
	}

    public override void onExit(RushState newState) {
		base.onExit(newState);
		rush.useGravity = true;
	}
}