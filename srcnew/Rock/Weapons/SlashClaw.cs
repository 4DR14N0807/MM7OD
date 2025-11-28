using System;
using System.Collections.Generic;
using System.Security;

namespace MMXOnline;

public class SlashClawWeapon : Weapon {
	public static SlashClawWeapon netWeapon = new();

	public SlashClawWeapon() : base() {
		displayName = "SLASH CLAW";
		index = (int)RockWeaponIds.SlashClaw;
		weaponBarBaseIndex = (int)RockWeaponBarIds.SlashClaw;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = (int)RockWeaponSlotIds.SlashClaw;
		killFeedIndex = 0;
		maxAmmo = 24;
		ammo = maxAmmo;
		fireRate = 30;
		descriptionV2 = [
			[ "Fast melee attack, able to pierce shields.\n" + "No tiene flinch." ]
		];
	}

	public override void shootRock(Rock rock, params int[] args) {
		base.shootRock(rock, args);
		int chargeLevel = args[0];

		if (rock.charState is LadderClimb lc) {
			rock.changeState(new SlashClawLadder(lc.ladder), true);
		} else {
			rock.changeState(new SlashClawState(), true);
		}
		rock.playSound("slash_claw", sendRpc: true);
	}
}


public class SlashClawMelee : GenericMeleeProj {
	public SlashClawMelee(Point pos, Player player, bool addToLevel) : base(
		SlashClawWeapon.netWeapon, pos, ProjIds.SlashClaw2,
		player, 2, 0, 0.25f * 60, addToLevel: addToLevel
	) {
		projId = (int)RockProjIds.SlashClaw;
	}
}

public class SlashClawState : CharState {
	public SlashClawState() : base("slashclaw") {
		normalCtrl = false;
		attackCtrl = false;
		airMove = true;
		canStopJump = true;
		airSprite = "slashclaw_air";
		landSprite = "slashclaw";
		fallSprite = landSprite;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		bool air = !character.grounded || character.vel.y < 0;
		if (air) character.changeSpriteFromName(airSprite, true);
	}

	public override void update() {
		base.update();

		airTrasition();
		if (character.isAnimOver()) character.changeToIdleOrFall();
	}
 }


public class SlashClawLadder : CharState {

	public Ladder ladder;

	public SlashClawLadder(Ladder ladder) : base("ladder_slashclaw") {
		normalCtrl = false;
		this.ladder = ladder;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.stopMoving();
		character.useGravity = false;
	}

	public override void update() {
		var midX = ladder.collider.shape.getRect().center().x;

		if (character.isAnimOver()) character.changeState(new LadderClimb(ladder, midX), true);
	}
}
