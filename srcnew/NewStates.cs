using System;
using SFML.Graphics;

namespace MMXOnline;

public class HealState : CharState {
	public Tank tank;
	public float healEffectTime;

	public HealState(Tank tank) : base("rest_heal") {
		this.tank = tank;
		normalCtrl = true;
		attackCtrl = true;
	}

	public override void update() {
		base.update();

		tank.heal(player, character);
		healGfx();

		int ixDir = player.input.getXDir(player);
		if (ixDir != 0 && !character.isSoftLocked() && character.canMove()) {
			if (character.canTurn() || ixDir == character.xDir) {
				character.xDir = ixDir;
				character.changeState(character.getRunState());
				return;
			}
			character.changeToIdleOrFall("rest_heal_end");
			return;
		}

		if (!tank.isHealing || character.shootAnimTime > 0 ||
			stateFrames > 10 && player.input.isPressed(Control.Special2, player)
		) {
			character.changeToIdleOrFall("rest_heal_end");
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.shootAnimTime = 0;
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		if (tank?.buff != null) {
			character.buffList.Remove(tank.buff);
			tank.buff = null;
		}
	}

	public void healGfx() {
		healEffectTime += Global.speedMul;
		if (healEffectTime >= 3) {
			healEffectTime = 0;
			Point gfxPos = character.pos.addxy(0, -15);

			Anim tempAnim = new Anim(
				gfxPos.addRand(14, 15), "charge_part_2", 1,
				null, true, host: character
			);
			tempAnim.vel.y = -120;
		}
	}

	public override void render(float x, float y) {
		base.render(x, y);

		renderHealBar(tank);
	}
}


public class HealStateLadder : CharState{

	public Tank tank;
	public float healEffectTime;
	Ladder ladder;
	float snapX;
	float? incY = null;
	public HealStateLadder(Tank tank, Ladder ladder, float snapX, float? incY = null
	) : base("ladder_climb") {
		this.tank = tank;
		this.ladder = ladder;
		this.snapX = snapX;
		this.incY = incY;
		useGravity = false;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.shootAnimTime = 0;
		character.frameSpeed = 0;
	}

	public override void update() {
		base.update();

		tank.heal(player, character);
		healGfx();

		if (character.canClimbLadder()) {
			if (
				(
					player.input.isPressed(Control.Down, player) || player.input.isPressed(Control.Up, player)
				) || (
					!tank.isHealing || character.shootAnimTime > 0 ||
					stateFrames > 10 && player.input.isPressed(Control.Special2, player)
				)
			) {
				character.changeState(new LadderClimb(ladder, snapX, incY));
			}
		}
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		character.frameSpeed = 1;
		if (tank?.buff != null) {
			character.buffList.Remove(tank.buff);
			tank.buff = null;
		}
	}

	public void healGfx() {
		healEffectTime += Global.speedMul;
		if (healEffectTime >= 3) {
			healEffectTime = 0;
			Point gfxPos = character.pos.addxy(0, -15);

			Anim tempAnim = new Anim(
				gfxPos.addRand(14, 15), "charge_part_2", 1,
				null, true, host: character
			);
			tempAnim.vel.y = -120;
		}
	}

	public override void render(float x, float y) {
		base.render(x, y);

		renderHealBar(tank);
	}
}


public class HurtSlide : Hurt {

	CharState? oldState;
	public HurtSlide(int dir, int flinchFrames) : base(dir, flinchFrames) {
		sprite = "hurt_slide";
		move = false;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		this.oldState = oldState;
	}

	public override void exit() {
		character.changeState(oldState ?? character.getIdleState());
	}
}