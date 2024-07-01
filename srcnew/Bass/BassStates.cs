using System;
using System.Collections.Generic;

namespace MMXOnline;

public class BassShoot : CharState {
	Bass bass = null!;

	public BassShoot() : base("shoot") {
		attackCtrl = true;
		airMove = true;
		useDashJumpSpeed = true;
		canJump = true;
		landSprite = "shoot";
		airSprite = "jump_shoot";
	}

	public override void update() {
		base.update();
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		bass = character as Bass ?? throw new NullReferenceException();
		
		landSprite = getShootSprite(bass.getShootYDir());
		airSprite = "jump_" + landSprite;
		sprite = landSprite;
		if (!bass.grounded) {
			sprite = airSprite;
		}
		bass.changeSprite(sprite, true);
	}

	public static string getShootSprite(int dir) {
		return dir switch {
			-2 => "shoot_up",
			-1 => "shoot_up_diag",
			0 => "shoot",
			1 => "shoot_down_diag",
			2 => "shoot",
			_ => "shoot"
		};
	}
}
