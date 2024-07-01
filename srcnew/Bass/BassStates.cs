using System;
using System.Collections.Generic;

namespace MMXOnline;

public class BassShoot : CharState {
	Bass bass = null!;

	public BassShoot() : base("not_a_real_sprite") {
		attackCtrl = true;
		airMove = true;
		useDashJumpSpeed = true;
		canJump = true;
	}

	public override void update() {
		base.update();
		if (stateFrames >= 16) {
			character.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		bass = character as Bass ?? throw new NullReferenceException();

		sprite = getShootSprite(bass.getShootYDir());
		landSprite = sprite;
		airSprite = "jump_" + sprite;

		if (!bass.grounded) {
			bass.changeSpriteFromName(airSprite, false);
		} else {
			bass.changeSpriteFromName(sprite, true);
		}
	}

	public static string getShootSprite(int dir) {
		return dir switch {
			-2 => "shoot_up",
			-1 => "shoot_up_diag",
			0 => "shoot",
			1 or 2 => "shoot_down_diag",
			_ => "shoot"
		};
	}
}
