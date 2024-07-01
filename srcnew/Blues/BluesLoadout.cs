using System.Collections.Generic;
using ProtoBuf;
using SFML.Graphics;

namespace MMXOnline;

[ProtoContract]
public class BluesLoadout {
	[ProtoMember(1)]
	public int specialWeapon;

	public static BluesLoadout createDefault() {
		return new BluesLoadout() {
			specialWeapon = 4
		};
	}

	public List<int> getProtoManWeaponIndices() {
		return new List<int>() { specialWeapon };
	}

	public void validate() {
		if (specialWeapon is < 0 or > 9) {
			specialWeapon = 4;
		}
	}

	public static BluesLoadout createRandom() {
		// Star Crash not here for AI reasons.
		int[] weapons = { 0, 1, 2, 3, 4, 5, 6 };
		return new BluesLoadout() {
			specialWeapon = weapons[Helpers.randomRange(0, weapons.Length - 1)]
		};
	}
}

public class BluesWeaponMenu : IMainMenu {
	// Menu controls.
	public IMainMenu prevMenu;
	public int cursorRow;
	bool inGame;
	public BluesLoadout targetLoadout;

	// Loadout items.
	public int specialWeapon;

	public int[][] weaponIcons = [
		[4, 5, 6, 7, 8, 9, 10, 11]
	];
	public string[] categoryNames = [
		"Special Weapon"
	];
	public Weapon[] specialWeapons = [
		new NeedleCannon(),
		new HardKnuckle(),
		new SearchSnake(),
		new SparkShock(),
		new PowerStone(),
		new WaterWave(),
		new GyroAttack(),
		new StarCrash(),
	];

	public BluesWeaponMenu(IMainMenu prevMenu, bool inGame) {
		this.prevMenu = prevMenu;
		this.inGame = inGame;
		targetLoadout = Options.main.bluesLoadout;
		specialWeapon = targetLoadout.specialWeapon;
	}

	public void update() {
		bool okPressed = Global.input.isPressedMenu(Control.MenuConfirm);
		bool backPressed = Global.input.isPressedMenu(Control.MenuBack);
		//Helpers.menuUpDown(ref cursorRow, 0, 1);

		if (cursorRow == 0) {
			Helpers.menuLeftRightInc(ref specialWeapon, 0, specialWeapons.Length - 1, true, playSound: true);
		}

		if (okPressed || backPressed && !inGame) {
			bool isChanged = false;
			// Temportally disables water wave.
			if (specialWeapon == 5) {
				specialWeapon = 0;
			}
			if (targetLoadout.specialWeapon != specialWeapon) {
				targetLoadout.specialWeapon = specialWeapon;
				isChanged = true;
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
			
			Menu.change(prevMenu);
			return;
		}
		if (backPressed) {
			Menu.change(prevMenu);
		}
	}

	public void render() {
		if (!inGame) {
			DrawWrappers.DrawTextureHUD(Global.textures["loadoutbackground"], 0, 0);
		} else {
			DrawWrappers.DrawTextureHUD(Global.textures["pausemenuload"], 0, 0);
		}
		Fonts.drawText(FontType.Yellow, "Protoman Loadout", Global.screenW * 0.5f, 24, Alignment.Center);

		int startY = 55;
		int startX = 30;
		int startX2 = 160;
		int wepW = 18;
		int wepH = 20;
		Global.sprites["cursor"].drawToHUD(0, startX, startY + cursorRow * wepH);

		for (int i = 0; i < 1; i++) {
			// Position.
			float yPos = startY - 6 + (i * wepH);
			// Current variable.
			int selectVar = i switch {
				_ => specialWeapon
			};
			// Category name.
			Fonts.drawText(FontType.LigthGrey, categoryNames[i], 40, yPos + 2);
			// Icons.
			for (int j = 0; j < weaponIcons[i].Length; j++) {
				// Draw icon sprite.
				Global.sprites["hud_blues_weapon_icon"].drawToHUD(
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
		string weaponSubDescription = "";
		if (cursorRow == 0) {
			Weapon currentWeapon = specialWeapons[specialWeapon];
			menuTitle = "Special Weapon";
			weaponTitle = currentWeapon.displayName;;
			weaponDescription = currentWeapon.descriptionV2;;
			if (currentWeapon.ammoUseText != "") {
				weaponSubDescription = $"Heat generation: {currentWeapon.ammoUseText}";
			} else {
				weaponSubDescription = $"Heat generation: {currentWeapon.defaultAmmoUse}";
			}
		}
		// Draw rectangle.
		int wsy = 124;
		DrawWrappers.DrawRect(
			25, wsy - 4, Global.screenW - 25, wsy + 68, true, new Color(0, 0, 0, 100), 1,
			ZIndex.HUD, false, outlineColor: Helpers.LoadoutBorderColor
		);
		DrawWrappers.DrawRect(
			25, wsy - 4, Global.screenW - 25, wsy + 11, true, new Color(0, 0, 0, 100), 1,
			ZIndex.HUD, false, outlineColor: Helpers.LoadoutBorderColor
		);
		// Draw descriptions.
		float titleY1 = wsy;
		float titleY2 = titleY1 + 16;
		float row1Y = titleY2 + 13;
		float row2Y = row1Y + 28;
		Fonts.drawText(
			FontType.Purple, menuTitle,
			Global.halfScreenW, titleY1, Alignment.Center
		);
		Fonts.drawText(
			FontType.Orange, weaponTitle,
			Global.halfScreenW, titleY2, Alignment.Center
		);
		Fonts.drawText(
			FontType.Grey, weaponDescription,
			Global.halfScreenW, row1Y, Alignment.Center
		);
		Fonts.drawText(
			FontType.Green, weaponSubDescription,
			Global.halfScreenW, row2Y, Alignment.Center
		);
	}
}
