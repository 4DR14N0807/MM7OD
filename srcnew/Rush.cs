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

	public Rush(Point pos, Player player, int xDir, ushort netId, bool ownedByLocalPlayer, bool rpc = false) :
	base("rush_warp_beam", pos, netId, ownedByLocalPlayer, false) {

		spriteToCollider["rush_warp_beam"] = null;

		//changeState(new RushIdle(), true);
	}

	public override Collider getTerrainCollider() {
		if (physicsCollider == null) {
			return null;
		}
		return new Collider(
			new Rect(0f, 0f, 26, 38).getPoints(),
			false, this, false, false,
			HitboxFlag.Hurtbox, new Point(0, 0)
		);
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

		RushState? oldState = rushState;
		oldState?.onExit(newState);

		rushState = newState;
		newState.onEnter(oldState);
	}

	public override void preUpdate() {
		base.preUpdate();
		changedStateInFrame = false;
	}
}

