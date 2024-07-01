using System;
using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;

namespace MMXOnline;


public class Rush : Actor {
	public Character character;
	public Player player => character.player;
	public RushState rushState;
	public bool usedCoil;

	// Object initalization happens here.
	public Rush(
		Point pos, Player owner, int xDir, ushort netId, bool ownedByLocalPlayer, bool rpc = false
	) : base(
		"rush_warp_beam", pos, netId, ownedByLocalPlayer, false
	) {
		// Normal variables.
		// Hopefully character is not null.
		// Character begin null only matters for the local player tho.
		netOwner = owner;
		this.character = owner.character;
		spriteToCollider["empty"] = null;
		// Forcefull change sprite to something before we crash.
		sprite = Global.sprites["empty"].clone();
		// We do this to manually call the state change.
		// As oldState cannot be null because we do not want null crashes.
		rushState = new RushState("empty");
		rushState.rush = this;
		rushState.character = character;
		// Then now that we set up a dummy state we call the actual changeState.
		// Only do this for the local player as we do not want other player to run state code.
		if (ownedByLocalPlayer) {
			changeState(new RushWarpIn());
		}
	}

	public override Collider? getTerrainCollider() {
		if (physicsCollider == null) {
			return null;
		}
		if (sprite.name == "rush_jet") {
			return getJetCollider();
		}
		return new Collider(
			new Rect(0f, 0f, 26, 38).getPoints(),
			false, this, false, false,
			HitboxFlag.Hurtbox, new Point(0, 0)
		);
	}

	public virtual Collider getJetCollider() {
		var rect = new Rect(0, 0, 40, 15);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public override Collider getGlobalCollider() {
		int yHeight = 38;
		var rect = new Rect(0, 0, 26, yHeight);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public virtual void changeState(RushState newState) {
		if (newState == null) {
			return;
		}
		// Set the character as soon as posible.
		newState.rush = this;
		newState.character = character;

		if (!rushState.canExit(this, newState)) {
			return;
		}
		if (!newState.canEnter(this)) {
			return;
		}
		changeSprite(getSprite(newState.sprite), true);

		RushState oldState = rushState;
		oldState.onExit(newState);

		rushState = newState;
		newState.onEnter(oldState);
	}

	public override void preUpdate() {
		base.preUpdate();
	}

	public override void update() {
		base.update();
	}

	public override void postUpdate() {
		base.postUpdate();
	}

	public override void statePreUpdate() {
		rushState.stateFrames += Global.speedMul;
		rushState.preUpdate();
	}

	public override void stateUpdate() {
		rushState.update();
	}

	public override void statePostUpdate() {
		rushState.postUpdate();
	}

	public virtual string getSprite(string spriteName) {
		return spriteName;
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		var chr = other.otherCollider.actor as Character;

		if (chr == null || chr.charState is Die) return;

		if (chr == netOwner.character && chr.charState is Fall &&
			chr != null && !usedCoil) {
			//changeSprite("rush_coil", true);
			changeState(new RushCoil());
			chr.vel.y = -chr.getJumpPower() * 1.75f;
			chr.changeState(new Jump(), true);
			usedCoil = true;
		}
	}
}

