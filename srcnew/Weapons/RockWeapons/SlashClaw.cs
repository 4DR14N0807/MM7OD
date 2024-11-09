using System;
using System.Collections.Generic;
using System.Security;

namespace MMXOnline;

public class SlashClawWeapon : Weapon {
	public SlashClawWeapon(Player player) : base() {
		damager = new Damager(player, 3, 0, 0.5f);
		index = (int)RockWeaponIds.SlashClaw;
		weaponBarBaseIndex = (int)RockWeaponBarIds.SlashClaw;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = (int)RockWeaponSlotIds.SlashClaw;
		killFeedIndex = 0;
		maxAmmo = 18;
		ammo = maxAmmo;
		fireRate = 60;
		description = new string[] { "Fast melee attack, requires proper spacing.", "No tiene flinch." };
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		int chargeLevel = args[0];

		if (character.charState is LadderClimb) {
			character.changeState(new SlashClawLadder(), true);
		} else {
			character.changeState(new SlashClawState(), true);
		}
		character.playSound("slash_claw", sendRpc: true);
	}
}


public class SlashClawState : CharState {
	public SlashClawState() : base("slashclaw") {
		normalCtrl = false;
		attackCtrl = false;
		airMove = true;
		canStopJump = true;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		bool air = !character.grounded || character.vel.y < 0;
		defaultSprite = sprite;
		landSprite = "slashclaw";
		if (air) {
			sprite = "slashclaw_air";
			defaultSprite = sprite;
		}
		character.changeSpriteFromName(sprite, true);
	}

	public override void update() {
		base.update();

		if (character.isAnimOver()) character.changeToIdleOrFall();
		else {
			if (character.grounded && player.input.isPressed(Control.Jump, player)) {
				character.vel.y = -character.getJumpPower();
				sprite = "slashclaw_air";
				character.changeSpriteFromName(sprite, false);
			}
		}
	}
 }


public class SlashClawLadder : CharState {
	public SlashClawLadder() : base("ladder_slashclaw") {
		normalCtrl = false;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.stopMoving();
		character.useGravity = false;
	}

	public override void update() {
		var ladders = Global.level.getTriggerList(character, 0, 1, null, typeof(Ladder));
		var midX = ladders[0].otherCollider.shape.getRect().center().x;

		if (character.isAnimOver()) character.changeState(new LadderClimb(ladders[0].gameObject as Ladder, midX), true);
	}
}
