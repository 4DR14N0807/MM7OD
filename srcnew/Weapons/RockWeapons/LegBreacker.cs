using System;
using System.Collections.Generic;

namespace MMXOnline;


public class LegBreaker : Weapon {
	public LegBreaker(Player player) : base() {
		damager = new Damager(player, 2, 0, 0.5f);
		index = (int)RockWeaponIds.LegBreaker;
		killFeedIndex = 0;
	}
}


public class LegBreakerState : CharState {

	public string initialSlideButton;
	public int initialSlideDir;
	bool isColliding;
	Anim? dust;
	Anim? effect;
	int particles = 3;
	Rock? rock;
	public LegBreakerState(string initialSlideButton) : base("sa_legbreaker", "", "", "") {
		enterSound = "slide";
		this.initialSlideButton = initialSlideButton;
		accuracy = 10;
		exitOnAirborne = true;
		attackCtrl = true;
		normalCtrl = true;
		useDashJumpSpeed = false;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		rock = character as Rock;
		initialSlideDir = character.xDir;
		character.isDashing = true;
		character.globalCollider = rock.getSlidingCollider();
		effect = new Anim(character.pos, "sa_double_jump_effect", character.xDir, player.getNextActorNetId(), false, true, zIndex: ZIndex.Character - 1);
	}

	public override void onExit(CharState oldState) {
		base.onExit(oldState);
		effect?.destroySelf();
	}

	public override void update() {
		base.update();

		Point charPos = character.pos;
		charPos.x -= character.xDir * 3f;
		charPos.y += 16f;
		if (effect != null) effect.changePos(charPos);

		float inputXDir = player.input.getInputDir(player).x;
		bool cancel = player.input.isPressed(getOppositeDir(initialSlideDir), player);

		if (Global.level.checkCollisionActor(character, 0, -24) != null) isColliding = true;
		else isColliding = false;

		if (stateTime >= Global.spf * 30 && !isColliding) {
			stateTime = 0;
			character.frameIndex = 0;
			character.sprite.frameTime = 0;
			character.sprite.animTime = 0;
			character.sprite.frameSpeed = 0.1f;

			character.changeState(new SlideEnd(), true);
			return;
		}

		var move = new Point(0, 0);
		move.x = character.getDashSpeed() * initialSlideDir;
		character.move(move);

		if (cancel) {
			if (isColliding) {
				character.xDir *= -1;
				initialSlideDir *= -1;
			} else character.changeState(new SlideEnd(), true);
		}

		if (stateTime >= Global.spf * 3 && particles > 0) {
			stateTime = 0;
			particles--;
			dust = new Anim(
				character.getDashDustEffectPos(initialSlideDir),
				"dust", initialSlideDir, player.getNextActorNetId(), true,
				sendRpc: true
			);
			dust.vel.y = (-particles - 1) * 20;
		}
	}

	string getOppositeDir(float inputX) {
		if (inputX == -1) return Control.Right;
		else return Control.Left;
	}
}
