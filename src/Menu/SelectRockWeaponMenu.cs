using SFML.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static SFML.Window.Keyboard;

namespace MMXOnline;

public class RockWeaponMenu : IMainMenu {
	// Menu controls.
	public IMainMenu prevMenu;
	public int cursorRow;
	bool inGame;
	public RockLoadout targetLoadout;

	// Loadout items.

	public int[] sWeapons = [
		0,1,2
	];

	public int[][] weaponIcons = [
		[0, 1, 2, 3, 4, 5, 6, 7, 8],
		[0, 1, 2, 3, 4, 5, 6, 7, 8],
		[0, 1, 2, 3, 4, 5, 6, 7, 8]
	];
	public string[] categoryNames = [
		"Slot 1", "Slot 2", "Slot 3"
	];

	public Weapon[] specialWeapons = [
		new RockBuster(false),
		new FreezeCracker(),
		new ThunderBolt(),
		new JunkShield(),
		new ScorchWheel(),
		new SlashClawWeapon(),
		new NoiseCrush(),
		new DangerWrap(),
		new WildCoil(),
	];

	int latestIndex = 0;
	int latestRow;
	int descIndex = 0;
	string error = "";

	public RockWeaponMenu(IMainMenu prevMenu, bool inGame) {
		this.prevMenu = prevMenu;
		this.inGame = inGame;
		targetLoadout = Options.main.rockLoadout;
		sWeapons[0] = targetLoadout.weapon1;
		sWeapons[1] = targetLoadout.weapon2;
		sWeapons[2] = targetLoadout.weapon3;
		latestIndex = sWeapons[cursorRow];
		latestRow = cursorRow;
	}

	public void update() {
		bool okPressed = Global.input.isPressedMenu(Control.MenuConfirm);
		bool backPressed = Global.input.isPressedMenu(Control.MenuBack);
		bool commandPressed = Global.input.isPressedMenu(Control.Special2);

		if (!string.IsNullOrEmpty(error)) {
			if (okPressed) {
				error = "";
			}
			return;
		}

		if (commandPressed) {
			descIndex++;
			Global.playSound("menu");
		} 

		Helpers.menuUpDown(ref cursorRow, 0, 2);
		Helpers.menuLeftRightInc(ref sWeapons[cursorRow], 0, specialWeapons.Length - 1, true, playSound: true);

		// Random loadout
		bool randomPressed = Global.input.isPressedMenu(Control.Special1);

		if (randomPressed) {
			bool duplicatedWeapons = true;

			for (int i = 0; i < 3; i++) {
				if (i != cursorRow) sWeapons[i] = Helpers.randomRange(0, 8);
			}
			Global.playSound("menu");
			
			while (duplicatedWeapons) {
				for (int i = 0; i < 3; i++) {
					if (i != cursorRow) sWeapons[i] = Helpers.randomRange(0, 8);
				}

				if (!duplicateWeapons()) duplicatedWeapons = false;
			}
		}

		if (latestIndex != sWeapons[cursorRow] || latestRow != cursorRow) {
			latestIndex = sWeapons[cursorRow];
			latestRow = cursorRow;
			descIndex = 0;
		}

		if (okPressed || backPressed && !inGame) {
			bool isChanged = false;
			if (!duplicateWeapons() && differentLoadout()) {
				targetLoadout.weapon1 = sWeapons[0];
				targetLoadout.weapon2 = sWeapons[1];
				targetLoadout.weapon3 = sWeapons[2];
				isChanged = true;
			} else if (duplicateWeapons()) {
				error = "Cannot select same weapon more than once!";
				return;
			}
			if (isChanged) {
				if (inGame && Global.level != null) {
					if (Options.main.killOnLoadoutChange) {
						Global.level.mainPlayer.forceKill();
					} else {
						Global.level.gameMode.setHUDErrorMessage(
							Global.level.mainPlayer,
							"Loadout change will apply on the next respawn",
							playSound: false
						);
					}
				}
				Options.main.saveToFile();
			}
			
			if (inGame) Menu.exit();
			else Menu.change(prevMenu);
			return;
		}
		if (backPressed) {
			Menu.change(prevMenu);
		}
	}

	public bool duplicateWeapons() {
		return sWeapons[0] == sWeapons[1] || 
		sWeapons[1] == sWeapons[2] || 
		sWeapons[0] == sWeapons[2];
	}

	bool differentLoadout() {
		return sWeapons[0] != targetLoadout.weapon1 || 
		sWeapons[1] != targetLoadout.weapon2 || 
		sWeapons[2] != targetLoadout.weapon3;
	}

	void drawTitleSquare(FontType font, string text, bool error) {
		int size = (Fonts.measureText(font, text)) / 2;
		DrawWrappers.DrawRect(
			Global.halfScreenW - size - 3, 22, Global.halfScreenW + size + 2 + (error ? 0 : 1), 38, 
			true, new Color(0, 0, 0, 150), 1, ZIndex.HUD, false, 
			outlineColor: error ? Color.White : Helpers.LoadoutBorderColor
		);
	}

	public void render() {
		int titleYPos = 24;

		if (!inGame) {
			DrawWrappers.DrawTextureHUD(Global.textures["loadoutbackground"], 0, 0);
		} else {
			DrawWrappers.DrawTextureHUD(Global.textures["pausemenuload"], 0, 0);
		}
		if (!string.IsNullOrEmpty(error)) {
			DrawWrappers.DrawRect(
				23 - (inGame ? 7 : 0), 16, Global.screenW - 23 + (inGame ? 7 : 0), Global.screenH - 16, true,
				new Color(0, 0, 0, 224), 0, ZIndex.HUD, false
			);
			string errorTitle = "ERROR";
			FontType errorFont = FontType.RedMenu;
			drawTitleSquare(errorFont, errorTitle, true);
			Fonts.drawText(errorFont, errorTitle, Global.screenW / 2, titleYPos, alignment: Alignment.Center);
			Fonts.drawText(FontType.Red, error.ToUpper(), Global.screenW / 2, Global.halfScreenH - 16, alignment: Alignment.Center);
			Fonts.drawTextEX(
				FontType.WhiteSmall, Helpers.controlText("Press [OK] to continue"),
				Global.screenW / 2, Global.halfScreenH + 16, alignment: Alignment.Center
			);
			return;
		}
		string title = "Rock Loadout";
		FontType font = FontType.BlueMenu;
		drawTitleSquare(font, title, false);
		Fonts.drawText(font, title, Global.screenW * 0.5f, titleYPos, Alignment.Center);

		int startY = 55;
		int startX = 30;
		int startX2 = 120;
		int wepW = 18;
		int wepH = 20;
		float rightArrowPos = Global.screenW - 106;
		float leftArrowPos = startX2 - 15;

		Global.sprites["cursor"].drawToHUD(0, startX, startY - 1 + cursorRow * wepH);

		for (int i = 0; i < 3; i++) {
			// Position.
			float yPos = startY - 6 + (i * wepH);
			// Current variable.
			int selectVar = i switch {
				_ => sWeapons[i]
			};
			// Category name.
			Fonts.drawText(FontType.BlueMenu, categoryNames[i], 40, yPos - 1, selected: i == cursorRow);
			if (Global.frameCount % 60 < 30) {
				Fonts.drawText(
					FontType.BlueMenu, ">", i < 3 ? rightArrowPos : rightArrowPos - 108, yPos - 1,
					Alignment.Center, selected: cursorRow == i
				);
				Fonts.drawText(
					FontType.BlueMenu, "<", leftArrowPos, yPos - 1 , Alignment.Center, selected: cursorRow == i
				);
			}
			// Icons.
			for (int j = 0; j < weaponIcons[i].Length; j++) {
				// Draw icon sprite.
				Global.sprites["hud_weapon_icon"].drawToHUD(
					weaponIcons[i][j], startX2 + (j * wepW), startY + (i * wepH)
				);
				// Darken non-selected icons.
				if (selectVar != j) {
					DrawWrappers.DrawRectWH(
						startX2 + (j * wepW) - 7, startY + (i * wepH) - 7,
						14, 14, true, Helpers.FadedIconColor, 1, ZIndex.HUD, false
					);
				}
			}
		}
		// Weapon and data.
		string menuTitle = "";
		string weaponTitle = "";
		string weaponDescription = "";
		Weapon currentWeapon = new();
		
			currentWeapon = specialWeapons[sWeapons[cursorRow]];
			menuTitle = "Special Weapon";
			weaponTitle = currentWeapon.displayName;;
			int di = 0;
			if (currentWeapon.descriptionV2.Length > 0) {
				di = descIndex % currentWeapon.descriptionV2.Length;
			}
			weaponDescription = currentWeapon.descriptionV2[di][0];
		
		// Draw rectangle.
		int wsy = 108;
		DrawWrappers.DrawRect(
			25, wsy, Global.screenW - 25, wsy + 18, true, new Color(0, 0, 0, 100), 1,
			ZIndex.HUD, false, outlineColor: Helpers.LoadoutBorderColor
		);
		DrawWrappers.DrawRect(
			25, wsy, Global.screenW - 25, wsy + 72, true, new Color(0, 0, 0, 100), 1,
			ZIndex.HUD, false, outlineColor: Helpers.LoadoutBorderColor
		);
		// Draw descriptions.
		float titleY1 = wsy + 3;
		float titleY2 = titleY1 + 19;
		float row1Y = titleY2 + 13;
		float row2Y = row1Y + 25;
		Fonts.drawText(
			FontType.BlueMenu, menuTitle,
			Global.halfScreenW, titleY1, Alignment.Center
		);
		Fonts.drawText(
			FontType.Blue, weaponTitle,
			Global.halfScreenW, titleY2, Alignment.Center
		);
		Fonts.drawText(
			FontType.WhiteSmall, weaponDescription,
			Global.halfScreenW, row1Y, Alignment.Center
		); 

		//Switch info page section.
		for (int i = 0; i < currentWeapon.descriptionV2.Length; i++) {
			int fi = currentWeapon.descriptionV2.Length - 1 - i == descIndex % currentWeapon.descriptionV2.Length ? 2 : 0;

			Global.sprites["cursor"].drawToHUD(
				fi, Global.screenW - 31 - (i  * 10), row2Y + 6
			);
		}

		Fonts.drawTextEX(
			FontType.White, "[SPC]: Random, [CMD]: More weapon info",
			Global.screenW / 2, row2Y + 19, Alignment.Center
		);
	}
}
