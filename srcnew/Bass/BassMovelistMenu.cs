using System;
using System.Collections.Generic;

namespace MMXOnline;


public class BassMovelistMenu : MovelistMenu {

	bool hasSuperAdaptor;
	string spriteName = "menu_movelist_bass";
	string ogTitle;

	public BassMovelistMenu(
		IMainMenu prevMenu, bool inGame, bool hasSuperAdaptor
	) : base(
		prevMenu, inGame, hasSuperAdaptor ? "SUPER BASS" : "BASS"
	) {
		this.hasSuperAdaptor = hasSuperAdaptor;
		ogTitle = title;

		if (hasSuperAdaptor) {
			moves = new() {
				new MovelistItem(
					spriteName, 13, "SHOOT", Control.Shoot, "CAN BE CHARGED"
				),
				new MovelistItem(
					spriteName, 14, "DASH", Control.Dash
				),
				new MovelistItem(
					spriteName, 15, "DOUBLE JUMP", Control.Jump, "MID-AIR"
				),
				new MovelistItem(
					spriteName, 16, "FLIGHT", Control.Jump, "MID-AIR AFTER\nDOUBJE JUMPING"
				),
				new MovelistItem(
					spriteName, 17, "KICK", Control.Special1, "ON GROUND"
				),
				new MovelistItem(
					spriteName, 18, "SONIC CRUSHER", Control.Special1, "MID-AIR"
				),
				new MovelistItem(
					spriteName, 19, "SWEEPING LASER", Control.Down + " + " + Control.Special1, "MID-AIR\nNEEDS EVIL ENERGY"
				),
				new MovelistItem(
					spriteName, 20, "DARK COMET", Control.Up + " + " + Control.Special1, "MID-AIR\nNEEDS EVIL ENERGY"
				)
			};
		} else {
			moves = new() {
				//Base Bass
				new MovelistItem(
					spriteName, 0, "SHOOT", Control.Shoot
				),
				new MovelistItem(
					spriteName, 1, "DASH", Control.Dash
				),
				new MovelistItem(
					spriteName, 2, "DOUBLE JUMP", Control.Jump, "MID-AIR"
				),
				new MovelistItemAnimated(
					spriteName, new [] {4,5,6,7,8,9,10,11,12}, 
					"SWITCH WEAPON", Control.WeaponLeft + " or " + Control.WeaponRight
				),
				new MovelistItem(
					spriteName, 3, "ACTIVATE T.BOOST", Control.Special2, "COSTS " + Bass.TrebleBoostCost + " " + Global.nameCoins
				),
				new MovelistItem(
					"empty", 0, "", ""
				),
				new MovelistItem(
					"empty", 0, "", ""
				),
				new MovelistItem(
					"empty", 0, "", ""
				),
				//Super Bass
				new MovelistItem(
					spriteName, 13, "SHOOT", Control.Shoot, "CAN BE CHARGED"
				),
				new MovelistItem(
					spriteName, 16, "FLIGHT", Control.Jump, "MID-AIR AFTER\nDOUBJE JUMPING"
				),
				new MovelistItem(
					spriteName, 17, "KICK", Control.Up + " + " + Control.Special1, "ON GROUND"
				),
				new MovelistItem(
					spriteName, 18, "SONIC CRUSHER", Control.Special1, "HOLD " + Control.Special1 + " TO\nKEEP FLYING"
				),
				new MovelistItem(
					spriteName, 19, "SWEEPING LASER", Control.Down + " + " + Control.Special1, "MID-AIR\nNEEDS EVIL ENERGY"
				),
				new MovelistItem(
					spriteName, 20, "DARK COMET", Control.Up + " + " + Control.Special1, "MID-AIR\nNEEDS EVIL ENERGY"
				)
			};
		}	
	}

	public override void update() {
		base.update();

		if (scrollIndex > 1) title = "SUPER BASS";
		else title = ogTitle;
	}
}
