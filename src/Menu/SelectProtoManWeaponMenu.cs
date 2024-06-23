using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static SFML.Window.Keyboard;

namespace MMXOnline;

public class ProtoManWeaponCursor {
	public int index;

	public ProtoManWeaponCursor(int index) {
		this.index = index;
	}

	public int startOffset() {
		return 0;
	}

	public int numWeapons() {
		return 7;
	}

	/*public void cycleLeft() {
		if (index < 9) index = 9;
	}

	public void cycleRight() {
		if (index > 9) index = 0;
	}*/
}


public class SelectProtoManWeaponMenu : IMainMenu {
	public bool inGame;
	public List<ProtoManWeaponCursor> cursors;
	public int selCursorIndex;
	public List<Point> weaponPositions = new List<Point>();
	public string error = "";
	public int maxRows = 1;
	public int maxCols = 9;
	public static List<string> weaponNames = new List<string>()
	{
			"GEMINI LASER",
			"HARD KNUCKLE",
			"SEARCH SNAKE",
			"SPARK SHOCK",
			"POWER STONE",
			"GYRO ATTACK",
			"STAR CRASH",
		};

	public List<int> selectedWeaponIndices;
	public IMainMenu prevMenu;

	public SelectProtoManWeaponMenu(IMainMenu prevMenu, bool inGame) {
		this.prevMenu = prevMenu;
		for (int i = 0; i < 9; i++) {
			weaponPositions.Add(new Point(80, 42 + (i * 18)));
		}

		selectedWeaponIndices = Options.main.protomanLoadout.getProtoManWeaponIndices();
		this.inGame = inGame;

		cursors = new List<ProtoManWeaponCursor>();
		foreach (var selectedWeaponIndex in selectedWeaponIndices) {
			cursors.Add(new ProtoManWeaponCursor(selectedWeaponIndex));
		}
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
				if (cursors[selCursorIndex].index == -1) cursors[selCursorIndex].index = 6; //8;
				Global.playSound("menu");
			} else if (Global.input.isPressedMenu(Control.MenuRight)) {
				cursors[selCursorIndex].index++;
				if (cursors[selCursorIndex].index == 7) cursors[selCursorIndex].index = 0; //0;
				Global.playSound("menu");
			}
		} else {
			Helpers.menuLeftRightInc(ref cursors[selCursorIndex].index, 0, 1, playSound: true);
		}

		Helpers.menuUpDown(ref selCursorIndex, 0, 0);

		for (int i = 0; i < 1; i++) {
			selectedWeaponIndices[i] = cursors[i].index;
		}

		//Adrián: Random Loadout feature (via Loadout menu)
		
		bool randomPressed = Global.input.isPressedMenu(Control.Special1);

		if (randomPressed) {
			Random slot0 = new Random();
			
			Global.playSound("menu");
			cursors[0].index = slot0.Next(0, 9);
		}

		bool backPressed = Global.input.isPressedMenu(Control.MenuBack);
		bool selectPressed = Global.input.isPressedMenu(Control.MenuConfirm) || (backPressed && !inGame);
		if (selectPressed) {

			bool shouldSave = false;
			/*
            if (cursors[3].index != Options.main.xLoadout.melee) {
				Options.main.xLoadout.melee = cursors[3].index;
				if (Global.level?.mainPlayer != null) {
					Global.level.mainPlayer.loadout.xLoadout.melee = cursors[3].index;
					Global.level.mainPlayer.syncLoadout();
				}
				shouldSave = true;
			}
            */

				Options.main.protomanLoadout.specialWeapon = selectedWeaponIndices[0];
				shouldSave = true;
				if (inGame) {
					if (Options.main.killOnLoadoutChange) {
						Global.level.mainPlayer.forceKill();
					} else if (!Global.level.mainPlayer.isDead) {
						Global.level.gameMode.setHUDErrorMessage(Global.level.mainPlayer, "Change will apply on next death", playSound: false);
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

		Fonts.drawText(FontType.BlueMenu, "Protoman Loadout", Global.screenW * 0.5f, 22, Alignment.Center);
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
		for (int i = 0; i < 1; i++) {
			float yPos = startY - 6 + (i * wepH);

			Fonts.drawText(FontType.BlueMenu,"Slot " + (i + 1).ToString(), 40, yPos, selected: selCursorIndex == i);

			if (Global.frameCount % 60 < 30) {
				Fonts.drawText(
					FontType.BlueMenu, ">", cursors[i].index < 9 ? rightArrowPos : rightArrowPos - 18, yPos - 1,
					Alignment.Center, selected: selCursorIndex == i
				);
				Fonts.drawText(
					FontType.BlueMenu, "<", leftArrowPos, yPos - 1 , Alignment.Center, selected: selCursorIndex == i
				);
			}

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

		if (selCursorIndex >= 3) {} 
		else 
		{
			int wi = selectedWeaponIndices[selCursorIndex];
			var weapon = Weapon.getAllProtoManWeapons()[wi];
			int weakAgainstIndex = weapon.weaknessIndex;

			Fonts.drawText(FontType.BlueMenu, "Slot " + (selCursorIndex + 1).ToString() + " weapon", Global.halfScreenW, 128, Alignment.Center);
			Fonts.drawText(FontType.Grey, weaponNames[selectedWeaponIndices[selCursorIndex]], Global.halfScreenW, 149, Alignment.Center);
			//Global.sprites["hud_weapon_icon"].drawToHUD(weapon.weaponSlotIndex, Global.halfScreenW + 75, 148);

			

			var wep = Weapon.getAllProtoManWeapons()[wi];

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
