using System;
using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;

namespace MMXOnline;

public class BluesUpgradeMenu : IMainMenu {
		public static int selectArrowPosY;
		public static int selectArrowPosX;
		public IMainMenu prevMenu;
		public static bool onUpgradeMenu = true;
		public int lTankCost = 40;
		public int mTankCost = 20;
		public int startX = 25;

		public List<float> optionPositionsX = new List<float>() {
			16, 192
		};

		public List<float> optionPositionsY = new List<float>() {
			60, 80, 100, 120, 140
		};


	public BluesUpgradeMenu(IMainMenu prevMenu) {
		this.prevMenu = prevMenu;
	}

	public int getMaxIndex() {
		var mainPlayer = Global.level.mainPlayer;
		if (selectArrowPosX == 0) return Math.Clamp(1 + mainPlayer.ltanks.Count, 1, getMaxLTanks());
		return Math.Clamp(1 + mainPlayer.ltanks.Count, 1, getMaxLTanks());
	}

	public int getMaxLTanks() {
		return Global.level.server?.customMatchSettings?.maxETanks ?? 2;
	}

	public int getMaxMTanks() {
		return Global.level.server?.customMatchSettings?.maxWTanks ?? 2;
	}

	public Player mainPlayer {
		get { return Global.level.mainPlayer; }
	}

	public bool canUseLTankInMenu(bool canUseLTank) {
		if (!canUseLTank) return false;
		return lTankDelay == 0;
	}

	public bool canUseMTankInMenu(bool canUseMTank) {
		if (!canUseMTank) return false;
		return mTankDelay == 0;
	}


	public void update() {
		Helpers.menuUpDown(ref selectArrowPosY, 0, getMaxIndex() - 1);
		if (selectArrowPosY > getMaxIndex() - 1) selectArrowPosY = getMaxIndex() - 1;

		if (Global.input.isPressedMenu(Control.MenuConfirm)) {

			//LTANKS SECTION
			if (selectArrowPosX == 0) {
				if (mainPlayer.ltanks.Count <= selectArrowPosY) {
					if (mainPlayer.ltanks.Count < getMaxLTanks() && mainPlayer.currency >= lTankCost) {
						mainPlayer.currency -= lTankCost;
						mainPlayer.ltanks.Add(new LTank(mainPlayer.maxHealth));
						Global.playSound("upgrade");
					} else if (mainPlayer.ltanks.InRange(selectArrowPosY)) {
						if (canUseLTankInMenu(mainPlayer.canUseLTank(mainPlayer.ltanks[selectArrowPosY]))) {
							mainPlayer.ltanks[selectArrowPosY - 1].use(mainPlayer, mainPlayer.character);
						}
					}
				}
				else if (mainPlayer.ltanks.InRange(selectArrowPosY)) {
					if (canUseLTankInMenu(mainPlayer.canUseLTank(mainPlayer.ltanks[selectArrowPosY]))) {
						mainPlayer.ltanks[selectArrowPosY].use(mainPlayer, mainPlayer.character);
					}
				}
			}
		} else if (Global.input.isPressedMenu(Control.MenuBack)) {
			Menu.change(prevMenu);
		}
	}
	
	public void render() {
		var mainPlayer = Global.level.mainPlayer;
		var gameMode = Global.level.gameMode;

		DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);

		Global.sprites["cursor"].drawToHUD(0, optionPositionsX[selectArrowPosX], optionPositionsY[selectArrowPosY]);

		Fonts.drawText(FontType.BlueMenu, "UPGRADE MENU", Global.screenW * 0.5f, 16, Alignment.Center);
		Fonts.drawText(
			FontType.White,
			Global.nameCoins + ": " + mainPlayer.currency,
			Global.screenW * 0.5f, 32, Alignment.Center
		);

		// L-TANKS RENDER
		for (int i = 0; i < getMaxLTanks(); i++) {
			if (i > mainPlayer.ltanks.Count) continue;
			bool canUseLtank = true;
			bool owned = i < mainPlayer.ltanks.Count;
			string useString = !owned ? "BUY L-TANK" : "USE L-TANK";
			Point optionPos = new Point(optionPositionsX[0], optionPositionsY[i]);
			Point spritePos = new Point(optionPos.x + 6, optionPos.y - 8);

			if (owned) {
				LTank ltank = mainPlayer.ltanks[i];
				canUseLtank = mainPlayer.canUseLTank(ltank);

				Global.sprites["menu_ltank"].drawToHUD(0, optionPos.x + 6, optionPos.y - 8);

				if (lTankDelay > 0) {
					GameMode.drawWeaponSlotCooldown(optionPos.x + 14, optionPos.y, lTankDelay / maxLTankDelay);
				} else {
					Point topLeftBar = new Point(spritePos.x + 1, spritePos.y + 2);
					Point botRightBar = new Point(spritePos.x + 15, spritePos.y + 14);
					float yPos =  12 * (ltank.ammo / ltank.maxAmmo);
					DrawWrappers.DrawRect(
						topLeftBar.x, topLeftBar.y, botRightBar.x, botRightBar.y - yPos,
						true, new Color(0, 0, 0, 200), 1, ZIndex.HUD, isWorldPos: false
					);
				}
				Fonts.drawText(FontType.WhiteSmall, ltank.ammo.ToString(), optionPos.x + 6, optionPos.y - 12, Alignment.Center);
			} else {
				Global.sprites["menu_ltank"].drawToHUD(1, spritePos.x, spritePos.y);
				Point topLeftBar = new Point(spritePos.x, spritePos.y);
				Point botRightBar = new Point(spritePos.x + 16, spritePos.y + 16);

				DrawWrappers.DrawRect(
					topLeftBar.x + 1, topLeftBar.y, botRightBar.x - 1, botRightBar.y,
					true, new Color(0, 0, 0, 128), 1, ZIndex.HUD, isWorldPos: false
				);
			}

			FontType font = canUseLtank ? FontType.Blue : FontType.Red;
			if (owned) {
				if (!canUseLtank) {
					useString = "CANNOT USE L-TANK";
					font = FontType.Red;
				} 
				Fonts.drawText(
					font, useString, optionPos.x + 24, optionPos.y - 4,
					selected: selectArrowPosY == i && selectArrowPosX == 0
				);
			} else {
				Fonts.drawText(
					font, useString, optionPos.x + 24, optionPos.y - 4,
					selected: selectArrowPosY == i && selectArrowPosX == 0
				);
			}
			if (!owned) {
				string costStr = $" ({lTankCost} {Global.nameCoins})";
				int posOffset = Fonts.measureText(FontType.Grey, useString);
				Fonts.drawText(FontType.White, costStr, optionPos.x + 24 + posOffset, optionPos.y - 4);
			}
		}

		/*//MTANKS RENDER
		for (int i = 0; i < getMaxMTanks(); i++) {
			if (i > mainPlayer.mtanks.Count) continue;
			bool canUseMtank = true;
			bool buyOrUse = mainPlayer.mtanks.Count < i + 1;
			string buyOrUseStr = buyOrUse ? "BUY M-TANK" : "USE M-TANK";
			var optionPos = new Point(optionPositionsX[1], optionPositionsY[i]);
			if (!buyOrUse) {
				var mtank = mainPlayer.mtanks[i];

				Global.sprites["menu_mtank"].drawToHUD(0, optionPos.x + 6, optionPos.y - 8);
				if (!canUseMTankInMenu(canUseMtank)) {
					if (canUseMtank) {
						GameMode.drawWeaponSlotCooldown(optionPos.x + 6, optionPos.y - 8, mTankDelay / maxMTankDelay);
					} else {
						Global.sprites["menu_mtank"].drawToHUD(2, optionPos.x + 6, optionPos.y - 8, 0.5f);
					}
				}
			} else {
				Global.sprites["menu_mtank"].drawToHUD(1, optionPos.x + 6, optionPos.y - 8);
			}
			if (!buyOrUse) {
				if (!canUseMtank) buyOrUseStr = "CANNOT USE M-TANK";
				Fonts.drawText(FontType.Blue, buyOrUseStr, optionPos.x + 24, optionPos.y - 4);
			} else {
				Fonts.drawText(
					FontType.Blue, buyOrUseStr, optionPos.x + 24, optionPos.y - 4,
					selected: selectArrowPosY == i && selectArrowPosX == 1
				);
			}
			if (buyOrUse) {
				string costStr = $" ({mTankCost} {Global.nameCoins})";
				int posOffset = Fonts.measureText(FontType.Grey, buyOrUseStr);
				Fonts.drawText(FontType.White, costStr, optionPos.x + 24 + posOffset, optionPos.y - 4);
			}
		}*/

		Fonts.drawTextEX(
			FontType.Blue, "[MUP]/[MDOWN]: Select Item",
			Global.halfScreenW, Global.screenH - 28, Alignment.Center
		);
		Fonts.drawTextEX(
			FontType.Blue, "[OK]: Buy/Use, [BACK]: Back",
			Global.halfScreenW, Global.screenH - 18, Alignment.Center
		);
	}
}
