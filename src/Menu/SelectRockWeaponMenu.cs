using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static SFML.Window.Keyboard;

namespace MMXOnline;

public class RockWeaponCursor {
	public int index;

	public RockWeaponCursor(int index) {
		this.index = index;
	}

	public int startOffset() {
		return 0;
	}

	public int numWeapons() {
		return 9;
	}

	/*public void cycleLeft() {
		if (index < 9) index = 9;
	}

	public void cycleRight() {
		if (index > 9) index = 0;
	}*/
}

public class SelectRockWeaponMenu : IMainMenu {
	public bool inGame;
	public List<RockWeaponCursor> cursors;
	public int selCursorIndex;
	public List<Point> weaponPositions = new List<Point>();
	public string error = "";
	public int maxRows = 1;
	public int maxCols = 9;
	public static List<string> weaponNames = new List<string>()
	{
			"ROCK BUSTER",
			"FREEZE CRACKER",
			"THUNDER BOLT",
			"JUNK SHIELD",
			"SCORCH WHEEL",
			"SLASH CLAW",
			"NOISE CRUSH",
			"DANGER WRAP",
			"WILD COIL",
		};

	public List<int> selectedWeaponIndices;
	public IMainMenu prevMenu;

	public SelectRockWeaponMenu(IMainMenu prevMenu, bool inGame) {
		this.prevMenu = prevMenu;
		for (int i = 0; i < 9; i++) {
			weaponPositions.Add(new Point(80, 42 + (i * 18)));
		}

		selectedWeaponIndices = Options.main.rockLoadout.getRockWeaponIndices();
		this.inGame = inGame;

		cursors = new List<RockWeaponCursor>();
		foreach (var selectedWeaponIndex in selectedWeaponIndices) {
			cursors.Add(new RockWeaponCursor(selectedWeaponIndex));
		}
	}

	public bool duplicateWeapons() {
		return selectedWeaponIndices[0] == selectedWeaponIndices[1] || selectedWeaponIndices[1] == selectedWeaponIndices[2] || selectedWeaponIndices[0] == selectedWeaponIndices[2];
	}

	public bool areWeaponArrSame(List<int> wepArr1, List<int> wepArr2) {
		for (int i = 0; i < wepArr1.Count; i++) {
			if (wepArr1[i] != wepArr2[i]) return false;
		}

		return true;
	}

	public void update() {
		if (!string.IsNullOrEmpty(error)) {
			if (Global.input.isPressedMenu(Control.MenuConfirm)) {
				error = null;
			}
			return;
		}

		if (selCursorIndex < 3) {
			if (Global.input.isPressedMenu(Control.MenuLeft)) {
				cursors[selCursorIndex].index--;
				if (cursors[selCursorIndex].index == -1) cursors[selCursorIndex].index = 8; //8;
				Global.playSound("menu");
			} else if (Global.input.isPressedMenu(Control.MenuRight)) {
				cursors[selCursorIndex].index++;
				if (cursors[selCursorIndex].index == 9) cursors[selCursorIndex].index = 0; //0;
				Global.playSound("menu");
			}
		} else {
			Helpers.menuLeftRightInc(ref cursors[selCursorIndex].index, 0, 2, playSound: true);
		}

		Helpers.menuUpDown(ref selCursorIndex, 0, 3);

		for (int i = 0; i < 4; i++) {
			selectedWeaponIndices[i] = cursors[i].index;
		}

		//Adrián: Random Loadout feature (via Loadout menu)
		
		bool randomPressed = Global.input.isPressedMenu(Control.Special1);

		if (randomPressed) {
			Random slot0 = new Random(), slot1 = new Random(), slot2 = new Random();
			
			Global.playSound("menu");
			//cursors[0].index = slot0.Next(0, 9);
			cursors[1].index = slot1.Next(0, 9);
			cursors[2].index = slot2.Next(0, 9);

		}

		bool backPressed = Global.input.isPressedMenu(Control.MenuBack);
		bool selectPressed = Global.input.isPressedMenu(Control.MenuConfirm) || (backPressed && !inGame);
		if (selectPressed) {
			if (duplicateWeapons()) {
				error = "Cannot select same weapon more than once!";
				return;
			}

			bool shouldSave = false; 

			if (!areWeaponArrSame(selectedWeaponIndices, Options.main.rockLoadout.getRockWeaponIndices())) {
					
				Options.main.rockLoadout.weapon1 = selectedWeaponIndices[0];
				Options.main.rockLoadout.weapon2 = selectedWeaponIndices[1];
				Options.main.rockLoadout.weapon3 = selectedWeaponIndices[2];
				Options.main.rockLoadout.rushLoadout = selectedWeaponIndices[3];
				shouldSave = true;
				if (inGame) {
					if (Options.main.killOnLoadoutChange) {
						Global.level.mainPlayer.forceKill();
					} else if (!Global.level.mainPlayer.isDead) {
						Global.level.gameMode.setHUDErrorMessage(Global.level.mainPlayer, "Change will apply on next death", playSound: false);
					}
				}
			}

			if (shouldSave) {
				Options.main.saveToFile();
			}

			if (inGame) Menu.exit();
			else Menu.change(prevMenu);
		} else if (backPressed) {
			Menu.change(prevMenu);
		}
	}

	public void render() {
		if (!inGame) {
			DrawWrappers.DrawTextureHUD(Global.textures["loadoutbackground"], 0, 0);
		} else {
			DrawWrappers.DrawRect(5, 5, Global.screenW - 5, Global.screenH - 5, true, Helpers.MenuBgColor, 0, ZIndex.HUD + 200, false);
		}

		Fonts.drawText(FontType.BlueMenu, "Rockman Loadout", Global.screenW * 0.5f, 22, Alignment.Center);
		var outlineColor = inGame ? Color.White : Helpers.LoadoutBorderColor;
		float botOffY = inGame ? 0 : -1;

		int startY = 54;
		int startX = 30;
		int startX2 = 120;
		int wepW = 18;
		int wepH = 20;

		float rightArrowPos = Global.screenW - 106;
		float leftArrowPos = startX2 - 15;

		Global.sprites["cursor"].drawToHUD(0, startX, startY + (selCursorIndex * wepH));
		for (int i = 0; i < 4; i++) {
			float yPos = startY - 6 + (i * wepH);

			if (Global.frameCount % 60 < 30) {
				Fonts.drawText(
					FontType.BlueMenu, ">", i < 3 ? rightArrowPos : rightArrowPos - 108, yPos - 1,
					Alignment.Center, selected: selCursorIndex == i
				);
				Fonts.drawText(
					FontType.BlueMenu, "<", leftArrowPos, yPos - 1 , Alignment.Center, selected: selCursorIndex == i
				);
			}

			if (i == 3) {
				Fonts.drawText(FontType.BlueMenu, "RUSH ", 40, yPos, selected: selCursorIndex == i);

				for (int j = 0; j < 3; j++) {
					
					Global.sprites["hud_weapon_icon"].drawToHUD(j + 9, startX2 + (j * wepW), startY + (i * wepH));
				
					if (cursors[3].index != j) {
						DrawWrappers.DrawRectWH(
							startX2 + (j * wepW) - 7, startY + (i * wepH) - 7, 14, 14,
							true, Helpers.FadedIconColor, 1, ZIndex.HUD, false
						);
					}
				}
				break;
			}

			Fonts.drawText(FontType.BlueMenu,"Slot " + (i + 1).ToString(), 40, yPos, selected: selCursorIndex == i);


			for (int j = 0; j < cursors[i].numWeapons(); j++) {
				int jIndex = j + cursors[i].startOffset();
				Global.sprites["hud_rock_weapon_icon"].drawToHUD(jIndex, startX2 + (j * wepW), startY + (i * wepH));
				//Helpers.drawTextStd((j + 1).ToString(), startX2 + (j * wepW), startY + (i * wepH) + 10, Alignment.Center, fontSize: 12);
				if (selectedWeaponIndices[i] == jIndex) {
					DrawWrappers.DrawRectWH(startX2 + (j * wepW) - 7, startY + (i * wepH) - 7, 14, 14, false, Helpers.DarkGreen, 1, ZIndex.HUD, false);
				} else {
					DrawWrappers.DrawRectWH(startX2 + (j * wepW) - 7, startY + (i * wepH) - 7, 14, 14, true, Helpers.FadedIconColor, 1, ZIndex.HUD, false);
				}
			}
		}

		int wsy = 167;


		DrawWrappers.DrawRect(25, wsy - 41, Global.screenW - 25, wsy + 30, true, new Color(0, 0, 0, 100), 0.5f, ZIndex.HUD, false, outlineColor: outlineColor);
		DrawWrappers.DrawRect(25, wsy - 41, Global.screenW - 25, wsy - 24, true, new Color(0, 0, 0, 100), 0.5f, ZIndex.HUD, false, outlineColor: outlineColor);

		if (selCursorIndex >= 3) {
			int wi = selectedWeaponIndices[selCursorIndex];
			var rush = Rock.getAllRushWeapons()[wi];

			Fonts.drawText(FontType.BlueMenu, "Rush Adaptor", Global.halfScreenW, 128, Alignment.Center);
			Fonts.drawText(FontType.Grey, rush.displayName, Global.halfScreenW, 149, Alignment.Center);

			if (rush.description?.Length == 1) Fonts.drawText(FontType.LigthGrey, rush.description[0], 30, wsy + 2);
			else if (rush.description?.Length > 0) Fonts.drawText(FontType.LigthGrey, rush.description[0], 30, wsy - 2);
			if (rush.description?.Length > 1) Fonts.drawText(FontType.LigthGrey, rush.description[1], 30, wsy + 7);
			if (rush.description?.Length > 2) Fonts.drawText(FontType.LigthGrey, rush.description[2], 30, wsy + 16);
		} 
		else 
		{
			int wi = selectedWeaponIndices[selCursorIndex];
			var weapon = Weapon.getAllRockWeapons()[wi];
			int weakAgainstIndex = weapon.weaknessIndex;

			Fonts.drawText(FontType.BlueMenu, "Slot " + (selCursorIndex + 1).ToString() + " weapon", Global.halfScreenW, 128, Alignment.Center);
			Fonts.drawText(FontType.Grey, weaponNames[selectedWeaponIndices[selCursorIndex]], Global.halfScreenW, 149, Alignment.Center);
			//Global.sprites["hud_weapon_icon"].drawToHUD(weapon.weaponSlotIndex, Global.halfScreenW + 75, 148);

			

			var wep = Weapon.getAllRockWeapons()[wi];

			if (wep.description?.Length == 1) Fonts.drawText(FontType.LigthGrey, wep.description[0], 30, wsy + 2);
			else if (wep.description?.Length > 0) Fonts.drawText(FontType.LigthGrey, wep.description[0], 30, wsy - 2);
			if (wep.description?.Length > 1) Fonts.drawText(FontType.LigthGrey, wep.description[1], 30, wsy + 7);
			if (wep.description?.Length > 2) Fonts.drawText(FontType.LigthGrey, wep.description[2], 30, wsy + 16);
		}

		//Helpers.drawTextStd(Helpers.menuControlText("Left/Right: Change Weapon"), Global.screenW * 0.5f, 200 + botOffY, Alignment.Center, fontSize: 16);
		//Helpers.drawTextStd(Helpers.menuControlText("Up/Down: Change Slot"), Global.screenW * 0.5f, 205 + botOffY, Alignment.Center, fontSize: 16);
		//Helpers.drawTextStd(Helpers.menuControlText("WeaponL/WeaponR: Quick cycle X1/X2/X3 weapons"), Global.screenW * 0.5f, 205 + botOffY, Alignment.Center, fontSize: 16);
		string helpText = Helpers.menuControlText("[Z]: Back, [X]: Confirm");
		if (!inGame) helpText = Helpers.menuControlText("[Z]: Save and back");
		//Helpers.drawTextStd(helpText, Global.screenW * 0.5f, 210 + botOffY, Alignment.Center, fontSize: 16);
		if (!string.IsNullOrEmpty(error)) {
			float top = Global.screenH * 0.4f;
			DrawWrappers.DrawRect(5, 5, Global.screenW - 5, Global.screenH - 5, true, new Color(0, 0, 0, 224), 0, ZIndex.HUD, false);
			Fonts.drawText(FontType.Grey, error, Global.screenW / 2, top, Alignment.Center);
			//Helpers.drawTextStd(Helpers.controlText("Press [X] to continue"), Global.screenW / 2, 20 + top, alignment: Alignment.Center, fontSize: 24);
		}
	}
}
