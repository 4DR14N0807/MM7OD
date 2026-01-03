using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline;

public class SlashmanRun : BaseRun {
	public Slashman slashman = null!;
	public int xDir = 1;

	public SlashmanRun(bool skipInto = false) : base("run", transitionSprite: "run_start") {
		exitOnAirborne = true;
		attackCtrl = true;
		normalCtrl = true;
		if (skipInto) {
			sprite = defaultSprite;
		}
	}

	public override void update() {
		base.update();
		float runSpeed = character.getRunSpeed();
		float maxSpeed = SMPhysics.maxSprint - SMPhysics.minWalk;
		float animSpeed = ((runSpeed - SMPhysics.minWalk) / (maxSpeed * 1.25f)) + 1;
		if (character.sprite.name != character.getSprite("land")) {
			character.sprite.frameSpeed = animSpeed;
		}

		int ixDir = player.input.getXDir(player);
		if (runSpeed <= 0.5f && ixDir != character.xDir) {
			if (character.grounded && ixDir != 0) {
				int fi = character.frameIndex;
				character.changeState(character.getRunState(true));
				character.setFrameIndexSafe(fi);
				return;
			}
			character.changeToIdleOrFall();
			return;
		} else {
			character.moveXY(runSpeed * xDir, 0);
			if (runSpeed > 1 && ixDir != 0 && ixDir != xDir &&
				!character.groundedIce && !character.sprite.name.EndsWith("land")
			) {
				character.changeSpriteFromName("land", true);
			}
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		slashman = character as Slashman ?? throw new NullReferenceException();
		int ixDir = player.input.getXDir(player);
		if (ixDir != 0 && character.canTurn()) {
			character.xDir = player.input.getXDir(player);
		}
		xDir = character.xDir;
	}
}
