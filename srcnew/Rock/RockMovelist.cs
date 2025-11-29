using System;
using System.Collections.Generic;

namespace MMXOnline;

public class RockMovelistMenu : MovelistMenu {

	bool hasSuperAdaptor;
	string spriteName = "menu_movelist_rock";
	string ogTitle;

	public RockMovelistMenu(
		IMainMenu prevMenu, bool inGame, bool hasSuperAdaptor
	) : base(
		prevMenu, inGame, hasSuperAdaptor ? "SUPER MEGAMAN" : "MEGAMAN"
	) {
		this.hasSuperAdaptor = hasSuperAdaptor;
		ogTitle = title;

		if (hasSuperAdaptor) {
			moves = new() {
				new MovelistItem(
					spriteName, 15, "SHOOT", Control.Shoot
				),
				new MovelistItem(
					spriteName, 16, "SLIDE", slideInput()
				),
				new MovelistItem(
					spriteName, 17, "DOUBLE JUMP", Control.Jump, "MID-AIR"
				),
				new MovelistItem(
					spriteName, 18, "LEG BREAKER", Control.Down + " + " + Control.Dash
				),
				new MovelistItem(
					spriteName, 19, "ARROW SLASH", Control.Special1
				)
			};
		} else {
			moves = new() {
				//Base Mega Man
				new MovelistItem(
					spriteName, hasSuperAdaptor ? 15 : 0, "SHOOT", Control.Shoot
				),
				new MovelistItem(
					spriteName, hasSuperAdaptor ? 16 : 1, "SLIDE", slideInput()
				),
				new MovelistItemAnimated(
					spriteName, new [] {5,6,7,8,9,10,11,12,13}, 
					"SWITCH WEAPON", Control.WeaponLeft + " or " + Control.WeaponRight
				),
				new MovelistItem(
					spriteName, 2, "RUSH COIL", Control.Special1, "COSTS AMMO"
				),
				new MovelistItem(
					spriteName, 3, "RUSH JET", Control.Up + " + " + Control.Special1, "COSTS AMMO"
				),
				new MovelistItem(
					spriteName, 4, "RUSH SEARCH", Control.Down + " + " + Control.Special1, "COSTS " + Rock.RushSearchCost + " " + Global.nameCoins
				),
				new MovelistItem(
					spriteName, 14, "ACTIVATE ADAPTOR", Control.Special2, "COSTS " + Rock.SuperAdaptorCost + " " + Global.nameCoins
				),
				new MovelistItem(
					"empty", 0, "", ""
				),
				//Super Adaptor
				new MovelistItem(
					spriteName, 17, "DOUBLE JUMP", Control.Jump, "MID-AIR"
				),
				new MovelistItem(
					spriteName, 18, "LEG BREAKER", Control.Down + " + " + Control.Dash
				),
				new MovelistItem(
					spriteName, 19, "ARROW SLASH", Control.Special1
				)
			};
		}	
	}

	public override void update() {
		base.update();

		if (scrollIndex > 1) title = "SUPER MEGAMAN";
		else title = ogTitle;
	}

	string slideInput() {
		if (Options.main.downJumpSlide) return Control.Down + " + " + Control.Jump;
		return Control.Dash;
	}
}
