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
		canStopJump = true;
		airSpriteReset = true;
	}

	public override void update() {
		base.update();
		if (player.dashPressed(out string dashControl) && character.grounded) {
			if (bass.canUseTBladeDash()) {
				bass.changeState(new TenguBladeDash(), true);
			} else {
				bass.changeState(new Dash(dashControl), true);
			}
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

		sprite = getShootSprite(bass.getShootYDir(), bass.player.weapon);
		landSprite = sprite;
		airSprite = "jump_" + sprite;
		fallSprite = "fall_" + sprite;

		if (!bass.grounded || bass.vel.y < 0) {
			string tempSprite = airSprite;
			if (bass.vel.y >= 0) {
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

	public static string getShootSprite(int dir, Weapon wep) {
		if (wep is not BassBuster
			and not MagicCard
			and not WaveBurner
		) {
				return "shoot";
		}
		return dir switch {
			-2 => "shoot_up",
			-1 => "shoot_up_diag",
			0 => "shoot",
			1 or 2 => "shoot_down_diag",
			_ => "shoot"
		};
	}
}


public class BassShootLadder : CharState {

	Bass bass = null!;
	List<CollideData> ladders;
	float midX; 
	public BassShootLadder() : base("spritent") {
		normalCtrl = false;
		attackCtrl = true;
		canJump = true;
		canStopJump = true;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		bass = character as Bass ?? throw new NullReferenceException();
		bass.useGravity = false;
		ladders = Global.level.getTriggerList(character, 0, 1, null, typeof(Ladder));
		midX = ladders[0].otherCollider.shape.getRect().center().x;

		sprite = getShootSprite(bass.getShootYDir(), bass.player.weapon);
 
		bass.changeSpriteFromName(sprite, true);
		bass.sprite.restart();
	}

	public override void update() {
		base.update();
	
		if (stateFrames >= 16) {
			character.changeState(new LadderClimb(ladders[0].gameObject as Ladder, midX), true);
			return;
		}
	}

	public static string getShootSprite(int dir, Weapon wep) {
		if (wep is not BassBuster &&
			wep is not MagicCard) return "ladder_shoot";

		else if (wep is MagicCard) {
			if (dir < 0) return "ladder_shoot_up";
			return "ladder_shoot";
		}

		return dir switch {
			-2 => "ladder_shoot_up",
			-1 => "ladder_shoot_up_diag",
			0 => "ladder_shoot",
			1 or 2 => "ladder_shoot_down_diag",
			_ => "ladder_shoot"
		};
	}
}
