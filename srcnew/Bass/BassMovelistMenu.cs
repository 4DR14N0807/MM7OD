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

		if (!hasSuperAdaptor) {
			moves.AddRange([
				// Base Bass
				new MovelistItem(
					spriteName, 0, "Shoot", Control.Shoot
				),
				new MovelistItem(
					spriteName, 1, "Dash", Control.Dash
				),
				new MovelistItem(
					spriteName, 2, "Double jump", Control.Jump, "Mid-air"
				),
				new MovelistItemAnimated(
					spriteName, [4, 5, 6, 7, 8, 9, 10, 11, 12], "Switch weapon",
					$"{Control.WeaponLeft} or {Control.WeaponRight}"
				),
				new MovelistItem(
					spriteName, 3, "Trebble Boost", Control.Special2,
					$"Costs {Bass.TrebleBoostCost} {Global.nameCoins}"
				),
				new MovelistItem("", 0, "", ""),
				new MovelistItem("", 0, "", ""),
				new MovelistItem("", 0, "", ""),
			]);
		}
		// Super Bass
		moves.AddRange([
			new MovelistItem(
				spriteName, 13, "Shoot", Control.Shoot,
				"Can be used\nwhile walking.",
				"Can be charged.\nGets stronger\non Lv3."
			),
			new MovelistItem(
				spriteName, 14, "Dash", Control.Dash,
				"Grounded."
			),
			new MovelistItem(
				spriteName, 16, "Flight", Control.Jump,
				"Mid-air."
			),
			new MovelistItem(
				spriteName, 18, "Sonic Crusher", Control.Special1, "",
				$"Hold {Control.Special1} to\nkeep flying."
			),
			new MovelistItem(
				spriteName, 17, "Booster Kick", Control.Special1,
				"On Ground.\nNeeds Lv2."
			),
			new MovelistItem(
				spriteName, 20, "Dark Comet", Control.Up + " + " + Control.Special1,
				"Needs Lv2\nMid-air."
			),
			new MovelistItem(
				spriteName, 14, "Airdash", Control.Dash,
				"Needs Lv4\nMid-air."
			),
			new MovelistItem(
				spriteName, 19, "Sweeping Laser", Control.Down + " + " + Control.Special1,
				"Needs Lv4\nMid-air."
			),
			new MovelistItem(
				spriteName, 22, "Evil charge", Control.Special2, "",
				"Adds evil energy.\nReduces on LV5."
			),
			new MovelistItem(
				spriteName, 21, "Evil Release",
				$"{Control.Special2} + {Control.Down}", "Under Lv5\nOn Ground",
				"Heals 1 HP.\nMinus 2 Max HP.\nReduces LV.\nDamage Immune."
			),
			new MovelistItem(
				spriteName, 23, "Evil Unison", "Auto: on max Evil", "Under Lv5",
				"Heals 2 HP.\nAdds 2 Max HP.\nIncreases LV.\nDamage Immune.", true
			),
			new MovelistItem(
				spriteName, 24, "Evil Overload", "Auto: on max Evil", "At Lv5",
				"Heals 3 HP.\nStuns the user.", true
			),
		]);	
	}

	public override void update() {
		base.update();

		if (scrollIndex > 1) title = "SUPER BASS";
		else title = ogTitle;
	}
}
