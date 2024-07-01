using System;
using System.Collections.Generic;

namespace MMXOnline;

public class BassShoot : CharState {
	bool isFalling;
	Bass bass = null!;

	public BassShoot() : base("not_a_real_sprite") {
		attackCtrl = true;
		airMove = true;
		useDashJumpSpeed = true;
		canJump = true;
		canStopJump = true;
		airSpriteReset = true;
	}

	public override void update() {
		base.update();
		if (player.dashPressed(out string dashControl)) {
			bass.changeState(new Dash(dashControl), true);
			return;
		}
		if (stateFrames >= 16) {
			bass.changeToIdleOrFall();
			return;
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		bass = character as Bass ?? throw new NullReferenceException();

		sprite = getShootSprite(bass.getShootYDir());
		landSprite = sprite;
		airSprite = "jump_" + sprite;
		fallSprite = "fall_" + sprite;

		if (!bass.grounded || bass.vel.y < 0) {
			string tempSprite = airSprite;
			if (bass.vel.y >= 0) {
				isFalling = true;
				tempSprite = fallSprite;
			}
			if (bass.sprite.name != bass.getSprite("tempSprite")) {
				bass.changeSpriteFromName(tempSprite, false);
			}
		} else {
			bass.changeSpriteFromName(sprite, true);
			bass.sprite.restart();
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
