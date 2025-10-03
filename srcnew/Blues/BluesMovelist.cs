using System;
using System.Collections.Generic;

namespace MMXOnline;


public class BluesMovelistMenu : MovelistMenu {

	string ogTitle;
	string spriteNameBlues = "menu_movelist_blues";
	string spriteNameBreakMan = "menu_movelist_breakman";
	bool hasShield;
	int weaponId;
	string weaponName;
	bool isBreakMan;
	public BluesMovelistMenu(
		IMainMenu prevMenu, bool inGame, int weaponId, bool hasShield, bool isBreakMan
	) : base(
		prevMenu, inGame, isBreakMan ? "BREAK MAN" : "PROTO MAN"
	) {
		this.hasShield = hasShield;
		this.weaponId = (weaponId + 1) * 2;
		if (hasShield) this.weaponId++;
		weaponName = BluesWeaponMenu.specialWeapons[weaponId].displayName;
		this.isBreakMan = isBreakMan;
		string spriteName = isBreakMan ? spriteNameBreakMan : spriteNameBlues;
		ogTitle = title;

		moves = new() {
			//Blues default.
			new MovelistItem(
				spriteName, hasShield ? 1 : 0, "PROTO BUSTER", Control.Shoot
			),
			new MovelistItem(
				spriteName, this.weaponId, weaponName, Control.Special1, "LOST IN OVERHEAT"
			),
			new MovelistItem(
				spriteName, 18, "SHIELD SWAP", Control.WeaponLeft + " or " + Control.WeaponRight, "REDUCES SPEED"
			),
			new MovelistItem(
				spriteName, 22, "SPREAD SHOT", Control.Down + " + " + Control.Shoot, "MID-AIR"
			),
			new MovelistItem(
				spriteName, hasShield ? 20 : 19, "DASH", getDashInput(), "LOST IN OVERHEAT"
			),
			new MovelistItem(
				spriteName, 21, "SLIDE", getSlideInput()
			),
			new MovelistItem(
				spriteName, 23, "PROTO STRIKE", Control.Up + " + " + Control.Shoot, "LOST IN OVERHEAT"
			),
			new MovelistItem(
				spriteName, 24, "BIG BANG STRIKE", Control.Special1, "OVERHEAT ONLY"
			),
			//Break Man
			new MovelistItem(
				spriteNameBreakMan, hasShield ? 20 : 19, "AIR DASH", Control.Dash, "MID-AIR"
			),
			new MovelistItem(
				spriteNameBreakMan, 25, "SHIELD STOMP", 
				Control.Down + " + " + Control.WeaponLeft + " or " + Control.WeaponRight,
				"NEEDS SHIELD HP"
			),
			new MovelistItem(
				spriteNameBreakMan, 26, "RED STRIKE", Control.Special1, "OVERDRIVE ONLY"
			),
			new MovelistItem(
				spriteNameBreakMan, hasShield ? 28 : 27, "DOUBLE JUMP", Control.Jump, 
				"MID-AIR \nOVERDRIVE ONLY" 
			)
		};
	}

	public override void update() {
		base.update();

		if (scrollIndex > 1) title = "BREAK MAN";
		else title = ogTitle;
	}

	string getDashInput() {
		if (Options.main.reverseBluesDashInput) {
			if (Options.main.altBluesSlideInput) return Control.Down + " + " + Control.Dash;
			return Control.Down + " + " + Control.Jump;
		}
		return Control.Dash;
	}

	string getSlideInput() {
		if (Options.main.reverseBluesDashInput) return Control.Dash;
		else {
			if (Options.main.altBluesSlideInput) return Control.Down + " + " + Control.Dash;
			return Control.Down + " + " + Control.Jump;
		}
	} 
}
