using System.Collections.Generic;
using ProtoBuf;
using SFML.Graphics;

namespace MMXOnline;

[ProtoContract]
public class ProtoManLoadout {
	[ProtoMember(1)]
	public int specialWeapon;

	public List<int> getProtoManWeaponIndices() {
		return new List<int>() { specialWeapon };
	}

	public void validate() {
		if (specialWeapon < 0 || specialWeapon > 9) specialWeapon = 0;
	}

	public static ProtoManLoadout createRandom() {
		int[] weapons = { 1, 4, 4 };
		return new ProtoManLoadout() {
			specialWeapon = weapons[Helpers.randomRange(0, weapons.Length - 1)]
		};
	}
}

public class BluesWeaponMenu : IMainMenu {
	// Menu controls.
	public IMainMenu prevMenu;
	public int cursorRow;
	bool inGame;
	public ProtoManLoadout targetLoadout = Options.main.protomanLoadout;

	// Loadout items.
	public int specialWeapon;

	public int[][] weaponIcons = [
		[0, 0, 0, 0, 0, 0, 0]
	];
	public string[] categoryNames = [
		"Special Weapon"
	];
	public Weapon[] specialWeapons = [
		PowerStone.netWeapon,
		PowerStone.netWeapon,
		PowerStone.netWeapon,
		PowerStone.netWeapon,
		PowerStone.netWeapon,
		PowerStone.netWeapon,
		PowerStone.netWeapon,
	];

	public BluesWeaponMenu(IMainMenu prevMenu, bool inGame) {
		this.prevMenu = prevMenu;
		this.inGame = inGame;
		specialWeapon = targetLoadout.specialWeapon;
	}

	public void update() {
		bool okPressed = Global.input.isPressedMenu(Control.MenuConfirm);
		bool backPressed = Global.input.isPressedMenu(Control.MenuBack);
		//Helpers.menuUpDown(ref cursorRow, 0, 1);

		if (cursorRow == 0) {
			Helpers.menuLeftRightInc(ref specialWeapon, 0, 2, playSound: true);
		}

		if (okPressed || backPressed && !inGame) {
			bool isChanged = false;
			if (targetLoadout.specialWeapon != specialWeapon) {
				targetLoadout.specialWeapon = specialWeapon;
				isChanged = true;
			}
			if (inGame && Global.level != null && isChanged && Options.main.killOnLoadoutChange) {
				Global.level.mainPlayer.forceKill();
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
		int startX2 = 128;
		int wepW = 18;
		int wepH = 20;
		Global.sprites["cursor"].drawToHUD(0, startX, startY + cursorRow * wepH);

		for (int i = 0; i < 1; i++) {
			// Position.
			float yPos = startY - 6 + (i * wepH);
			// Current variable.
			int selectVar = i switch {
				_ => 0
			};
			// Category name.
			Fonts.drawText(FontType.Blue, categoryNames[i], 40, yPos + 2, selected: cursorRow == i);
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
		string weaponSubDescription = "";
		if (cursorRow == 0) {
			menuTitle = "Giga Attack";
			weaponTitle = "";
			weaponDescription = "";
			weaponSubDescription = "";
		}
		// Draw rectangle.
		int wsy = 124;
		DrawWrappers.DrawRect(
			25, wsy - 4, Global.screenW - 25, wsy + 68, true, new Color(0, 0, 0, 100), 1,
			ZIndex.HUD, false, outlineColor: Helpers.LoadoutBorderColor
		);
		DrawWrappers.DrawRect(
			25, wsy - 4, Global.screenW - 25, wsy + 11, true, new Color(0, 0, 0, 100), 1,
			ZIndex.HUD, false, outlineColor:  Helpers.LoadoutBorderColor
		);
		// Draw descriptions.
		float titleY1 = 124;
		float titleY2 = 140;
		float row1Y = 153;
		float row2Y = 181;
		Fonts.drawText(
			FontType.Purple, menuTitle,
			Global.halfScreenW, titleY1, Alignment.Center
		);
		Fonts.drawText(
			FontType.Orange, weaponTitle,
			Global.halfScreenW, titleY2, Alignment.Center
		);
		Fonts.drawText(
			FontType.Green, weaponDescription,
			Global.halfScreenW, row1Y, Alignment.Center
		);
		Fonts.drawText(
			FontType.Blue, weaponSubDescription,
			Global.halfScreenW, row2Y, Alignment.Center
		);
	}
}
