using System;
using System.Collections.Generic;

namespace MMXOnline;

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
		Global.playSound("ding");
		rush.changeSprite("rush_idle", true);
	}
}