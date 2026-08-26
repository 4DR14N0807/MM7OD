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

		Color color = new Color(123, 255, 123);
		int posX = ((int)Global.screenW / 2) - 32;
		int posY = (int)Global.screenH - 32;
		Color outline = new Color(24, 24, 24);

		int segments = 3;
		int stacks = tank.healStacks;
		float progress = 1 - (tank.healTime / tank.healMaxTime);
		int barSize = 20;
		int currentOffset = 0;

		DrawWrappers.DrawRectWH(
			posX, posY, 64, 6, true, outline, 0, ZIndex.HUD, false
		);
		for (int i = 0; i <= segments - 1; i++) {
			DrawWrappers.DrawRectWH(
				posX + 1 + currentOffset,
				posY + 1, barSize, 4, true, new Color(49, 49, 49), 0, ZIndex.HUD, false
			);
			currentOffset += barSize + 1;
		}
		currentOffset = 0;
		for (int i = 0; i <= stacks - 1; i++) {
			DrawWrappers.DrawRectWH(
				posX + 1 + currentOffset,
				posY + 1, barSize, 4, true, color, 0, ZIndex.HUD, false
			);
			currentOffset += barSize + 1;
		}
		DrawWrappers.DrawRectWH(
			posX + 1 + currentOffset, posY + 1,
			barSize * progress, 4, true, color, 0, ZIndex.HUD, false
		);
		Fonts.drawText(
			FontType.WhiteMini, "resting", posX + 32, posY - 5,
			Alignment.Center, false, depth: ZIndex.HUD, color: color
		);
	}
}