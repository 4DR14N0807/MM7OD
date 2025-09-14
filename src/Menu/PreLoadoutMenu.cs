using System;

namespace MMXOnline;

public class PreLoadoutMenu : IMainMenu {
	public int selectY;
	public int[] optionPos = {
		50,
		70,
		90,
		110,
		130,
		150,
		170
	};
	public int[] optionPos2 = {
		(int)Global.halfScreenW - 40,
		(int)Global.halfScreenW - 50,
		(int)Global.halfScreenW - 55,
		(int)Global.halfScreenW - 50,
		(int)Global.halfScreenW - 45,
		(int)Global.halfScreenW - 55,
		(int)Global.halfScreenW - 50
	};
	public int[] optionPos3 = {
		(int)Global.halfScreenW + 40,
		(int)Global.halfScreenW + 50,
		(int)Global.halfScreenW + 55,
		(int)Global.halfScreenW + 50,
		(int)Global.halfScreenW + 45,
		(int)Global.halfScreenW + 55,
		(int)Global.halfScreenW + 50
	};
	public MainMenu prevMenu;
	public string message;
	public Action yesAction;
	public bool inGame;
	public bool isAxl;
	public float startX = Global.halfScreenW;
	public float Time = 1, Time2;
	public bool Confirm = false, Confirm2 = false;
	public PreLoadoutMenu(MainMenu prevMenu) {
		this.prevMenu = prevMenu;
		selectY = 0;
	}
	public void TimeUpdate() {
		if (Confirm == false) Time -= Global.spf * 2;
		if (Time <= 0) {
			Confirm = true;
			Time = 0;
		}
		if (Global.input.isPressedMenu(Control.MenuBack)) Confirm2 = true;
		if (Confirm2 == true) Time2 += Global.spf * 2;
	}
	public void update() {
		TimeUpdate();
		Helpers.menuUpDown(ref selectY, 0, 2);

		if (Global.input.isPressedMenu(Control.MenuConfirm)) {
			/*if (selectY == 0) {
				Menu.change(new SelectWeaponMenu(this, false));
			}
			if (selectY == 1) {
				Menu.change(new SelectZeroWeaponMenu(this, false));
			}
			if (selectY == 2) {
				Menu.change(new SelectPunchyZeroWeaponMenu(this, false));
			}
			*/
			if (selectY == 0) {
				Menu.change(new RockWeaponMenu(this, false));
			}
			if (selectY == 1) {
				Menu.change(new BluesWeaponMenu(this, false));
			}
			if (selectY == 2) {
				Menu.change(new BassWeaponMenu(this, false));
			}
		} else if (Global.input.isPressedMenu(Control.MenuBack)) {
			if (Time2 >= 1) {
				Menu.change(prevMenu);
				prevMenu.Time = 0;
				prevMenu.Time2 = 1;
				prevMenu.Confirm = false;
				prevMenu.Confirm2 = false;
			}
		}
		
	}

	public void render() {
		if (!inGame) {
			DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0);
			//DrawWrappers.DrawTextureMenu(Global.textures["cursor"], 20, topLeft.y + ySpace + (selectArrowPosY * ySpace));
			//Global.sprites["cursor"].drawToHUD(0, startX - 10, 53 + (selectY * 20));
		} else {
			DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
			//Global.sprites["cursor"].drawToHUD(0, startX - 10, 53 + (selectY * 20));
		}
		if (Global.flFrameCount % 60 < 30) {
			for (int i = 0; i < 6; i++) {
				if (selectY == i) {
					Fonts.drawText(FontType.Blue, "<", optionPos2[i], optionPos[i], Alignment.Center, selected: selectY == i, selectedFont: FontType.DarkOrange);
					Fonts.drawText(FontType.Blue, ">", optionPos3[i]-1, optionPos[i], Alignment.Center, selected: selectY == i, selectedFont: FontType.DarkOrange);
				}
			}
		}

		Fonts.drawText(FontType.BlueMenu, "SELECT CHARACTER LOADOUT", Global.screenW * 0.5f, 20, Alignment.Center);

		Fonts.drawText(FontType.Grey, "ROCKMAN LOADOUT", startX, optionPos[0], selected: selectY == 0);
		Fonts.drawText(FontType.Grey, "PROTOMAN LOADOUT", startX, optionPos[1], selected: selectY == 1);
		Fonts.drawText(FontType.Grey, "BASS LOADOUT", startX, optionPos[2], selected: selectY == 2);

		Fonts.drawTextEX(FontType.Grey, "[OK]: Choose, [BACK]: Back", Global.halfScreenW, 200, Alignment.Center);
		if (Options.main.blackFade) {
			DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0, 384, 216, 0, 0, Time);
			DrawWrappers.DrawTextureHUD(Global.textures["menubackground"], 0, 0, 384, 216, 0, 0, Time2);
		}
	}
}
