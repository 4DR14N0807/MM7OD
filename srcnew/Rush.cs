using System;
using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;

namespace MMXOnline;


public class Rush : Actor {

	public Character? character;
	public Player? player { get { return character?.player; } }
	public RushState rushState;
	public bool changedStateInFrame;
	public bool usedCoil;

	public Rush(Point pos, Player owner, int xDir, ushort netId, bool ownedByLocalPlayer, bool rpc = false) :
	base("rush_warp_beam", pos, netId, ownedByLocalPlayer, false) {

		netOwner = owner;
		//spriteToCollider["rush_warp_beam"] = null;
		changeState(new RushIdle(), true);
	}

	public override Collider getTerrainCollider() {
		if (physicsCollider == null) {
			return null;
		}

		if (rushState is RushJetState) return getJetCollider();

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

	public virtual void changeState(RushState newState, bool forceChange = false) {
		if (newState == null) {
			return;
		}
		if (!forceChange &&
			(rushState?.GetType() == newState.GetType() || changedStateInFrame)
		) {
			return;
		}
		// Set the character as soon as posible.
		newState.rush = this;
		
		if (rushState?.canExit(this, newState) == false) {
			return;
		}
		if (!newState.canEnter(this)) {
			return;
		}
		changedStateInFrame = true;

		changeSprite(getSprite(newState.sprite), true);

		RushState? oldState = rushState;
		oldState?.onExit(newState);

		rushState = newState;
		newState.onEnter(oldState);
	}

	public override void preUpdate() {
		base.preUpdate();
		changedStateInFrame = false;
	}

	public override void update() {
		base.update();
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
			changeState(new RushCoil(), true);
			chr.vel.y = -chr.getJumpPower() * 1.75f;
			chr.changeState(new Jump(), true);
			usedCoil = true;
		}

		
	}
}

