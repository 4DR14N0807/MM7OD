﻿using System;
using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;

namespace MMXOnline;

public class UpgradeMenu : IMainMenu {
	public static int selectArrowPosY;
	public static int selectArrowPosX;
	public IMainMenu prevMenu;
	public static bool onUpgradeMenu = true;
	public static bool isUsingWTank = false;
	public int eTankCost = 40; // 28
	public int wTankCost = 30; // 22
	public List<Weapon> eTankTargets = new List<Weapon>();
	public List<Weapon> wTankTargets = new List<Weapon>();
	public static int eTankTargetIndex;
	public static int wTankTargetIndex;
	public int startX = 25;
	public bool isFillingETank;
	public bool isFillingWTank;
	public static float eTankDelay = 0;
	public const float maxETankDelay = 2;
	public static float wTankDelay = 0;
	public const float maxWTankDelay = 2;

	public List<Point> optionPositions = new List<Point>() {
		new Point (16, 70),
		new Point (192, 90),
	};

	public List<float> optionPositionsX = new List<float>() {
		16, 192
	};

	public List<float> optionPositionsY = new List<float>() {
		70, 90, 110, 130
	};

	public UpgradeMenu(IMainMenu prevMenu) {
		this.prevMenu = prevMenu;
		optionPositions.Add(new Point(startX, 60));
		optionPositions.Add(new Point(startX, 80));
		optionPositions.Add(new Point(startX, 100));
		optionPositions.Add(new Point(startX, 120));
		optionPositions.Add(new Point(startX, 140));

		if (selectArrowPosY >= Global.level.mainPlayer.ETanks.Count + 1) {
			selectArrowPosY = Global.level.mainPlayer.ETanks.Count;
		}

		/* 		if (selectArrowPosY >= Global.level.mainPlayer.wtanks.Count + 1) {
			selectArrowPosY = Global.level.mainPlayer.wtanks.Count;
		} */
	}

	public int getMaxIndex() {
		var mainPlayer = Global.level.mainPlayer;
		if (selectArrowPosX == 0) return Math.Clamp(1 + mainPlayer.ETanks.Count, 1, getMaxETanks());
		return Math.Clamp(1 + mainPlayer.wtanks.Count, 1, getMaxWTanks());
	}

	public static int getHeartTankCost() {
		/*if (Global.level.server?.customMatchSettings != null) {
			return Global.level.server.customMatchSettings.heartTankCost;
		}*/
		return 2;
	}

	public static int getMaxHeartTanks() {
		return Global.level.server?.customMatchSettings?.maxHeartTanks ?? 8;
	}

	public int getMaxETanks() {
		return Global.level.server?.customMatchSettings?.maxETanks ?? 2;
	}

	public int getMaxWTanks() {
		return Global.level.server?.customMatchSettings?.maxWTanks ?? 2;
	}

	public Player mainPlayer {
		get { return Global.level.mainPlayer; }
	}

	public bool canUseETankInMenu(bool canUseETank) {
		if (!canUseETank) return false;
		return eTankDelay == 0;
	}

	public bool canUseWTankInMenu(bool canUseWTank) {
		if (!canUseWTank) return false;
		return wTankDelay == 0;
	}

	public float getAmmoPercentHeal() {
		int weps = 0;
		foreach(var wep in mainPlayer.weapons) {
			if (wep.getAmmoUsage(0) > 0 && wep is not RushWeapon) weps++;
		}
		if (weps == 2) return 100f;
		return 75f;
	}

	public void update() {
		if (updateAdaptorUpgrades(mainPlayer)) return;

		eTankTargets.Clear();
		wTankTargets.Clear();
		if (mainPlayer.isRock) {
			wTankTargets = mainPlayer.weapons.FindAll(
				w => (w is Weapon wep)
			).ToList();
		}

		if (eTankTargets.Count > 0 && selectArrowPosY >= 1 && selectArrowPosX == 0) {
			Helpers.menuLeftRightInc(ref eTankTargetIndex, 0, eTankTargets.Count - 1);
		}
		if (wTankTargets.Count > 0 && selectArrowPosY >= 1 && selectArrowPosY == 1 && isUsingWTank) {
			Helpers.menuLeftRightInc(ref wTankTargetIndex, 0, wTankTargets.Count - 1);
		}

		if (!eTankTargets.InRange(eTankTargetIndex)) eTankTargetIndex = 0;
		if (!wTankTargets.InRange(wTankTargetIndex)) wTankTargetIndex = 0;

		if (Global.input.isPressedMenu(Control.MenuLeft)) {
			if (!isUsingWTank) selectArrowPosX--;
			if (selectArrowPosX < 0) selectArrowPosX = 1;

			if (wTankTargets.Count > 0 && isUsingWTank) wTankTargetIndex--;
			if (wTankTargetIndex < 0 ) wTankTargetIndex = wTankTargets.Count - 1;

			if (mainPlayer.realCharNum == 0) {
				if (mainPlayer.canUpgradeXArmor()) {
					UpgradeArmorMenu.xGame = 3;
					Menu.change(new UpgradeArmorMenu(prevMenu));
					onUpgradeMenu = false;
					return;
				}
			}
		}

		if (Global.input.isPressedMenu(Control.MenuRight)) {
			if (!isUsingWTank) selectArrowPosX++;
			if (selectArrowPosX > 1) selectArrowPosX = 0;

			if (wTankTargets.Count > 0 && isUsingWTank) wTankTargetIndex++;
			if (wTankTargetIndex > wTankTargets.Count + 1) wTankTargetIndex = 0;

			if (mainPlayer.realCharNum == 0) {
				if (mainPlayer.canUpgradeXArmor()) {
					UpgradeArmorMenu.xGame = 1;
					Menu.change(new UpgradeArmorMenu(prevMenu));
					onUpgradeMenu = false;
					return;
				}
			} else if (mainPlayer.realCharNum == 2) {
				Menu.change(new SelectVileArmorMenu(prevMenu));
				onUpgradeMenu = false;
				return;
			}
		}

		Helpers.menuUpDown(ref selectArrowPosY, 0, getMaxIndex() - 1);
		if (selectArrowPosY > getMaxIndex() - 1) selectArrowPosY = getMaxIndex() - 1;

		if (Global.input.isPressedMenu(Control.MenuConfirm) && mainPlayer.character != null) {
			//ETANKS SECTION
			if (selectArrowPosX == 0) {
				if (mainPlayer.ETanks.Count <= selectArrowPosY) {
					if (mainPlayer.ETanks.Count < getMaxETanks() && mainPlayer.currency >= eTankCost) {
						mainPlayer.currency -= eTankCost;
						mainPlayer.ETanks.Add(new ETank());
						Global.playSound("upgrade");
					} else if (mainPlayer.ETanks.InRange(selectArrowPosY)) {
						if (canUseETankInMenu(mainPlayer.canUseEtank(mainPlayer.ETanks[selectArrowPosY]))) {
							mainPlayer.ETanks[selectArrowPosY - 1].use(mainPlayer, mainPlayer.character);
						}
					}
				}

				else if (mainPlayer.ETanks.InRange(selectArrowPosY)) {
					if (canUseETankInMenu(mainPlayer.canUseEtank(mainPlayer.ETanks[selectArrowPosY]))) {
						mainPlayer.ETanks[selectArrowPosY].use(mainPlayer, mainPlayer.character);
					}
				}
			}

			//WTANKS SECTION
			else if (selectArrowPosX == 1) {
				if (mainPlayer.wtanks.Count <= selectArrowPosY) {
					if (mainPlayer.wtanks.Count < getMaxWTanks() && mainPlayer.currency >= wTankCost) {
						mainPlayer.currency -= wTankCost;
						mainPlayer.wtanks.Add(new WTank());
						Global.playSound("upgrade");
					} else if (mainPlayer.wtanks.InRange(selectArrowPosY)) {
						if (!isUsingWTank) {
							isUsingWTank = true;
							
						} else {
							if (wTankTargets.Count > 0) {
								var currentTarget = wTankTargets[wTankTargetIndex];
							}

							if (canUseWTankInMenu(mainPlayer.canUseWTank(mainPlayer.wtanks[selectArrowPosY]))) {
								mainPlayer.wtanks[selectArrowPosY].use(mainPlayer, mainPlayer.character, getAmmoPercentHeal());
								mainPlayer.wtanks.RemoveAt(selectArrowPosY);
							}

							isUsingWTank = false;
						}
					}
				}

				else if (mainPlayer.wtanks.InRange(selectArrowPosY)) {
					if (wTankTargets.Count > 0) {
						var currentTarget = wTankTargets[wTankTargetIndex];
					}
					if (canUseWTankInMenu(mainPlayer.canUseWTank(mainPlayer.wtanks[selectArrowPosY]))) {
						mainPlayer.wtanks[selectArrowPosY].use(mainPlayer, mainPlayer.character, getAmmoPercentHeal());
						mainPlayer.wtanks.RemoveAt(selectArrowPosY);
					}
					isUsingWTank = false;
				}
			}

			
		} else if (Global.input.isPressedMenu(Control.MenuBack)) {
			if (isUsingWTank) isUsingWTank = false;
			else Menu.change(prevMenu);
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

		//ETANKS RENDER
		for (int i = 0; i < getMaxETanks(); i++) {
			if (i > mainPlayer.ETanks.Count) continue;
			bool canUseEtank = true;
			bool owned = i < mainPlayer.ETanks.Count;
			string useString = !owned ? "BUY E-TANK" : "USE E-TANK";
			Point optionPos = new Point(optionPositionsX[0], optionPositionsY[i]);
			Point spritePos = new Point(optionPos.x + 6, optionPos.y - 8);

			if (owned) {
				ETank etank = mainPlayer.ETanks[i];
				canUseEtank = mainPlayer.canUseEtank(etank);

				Global.sprites["menu_etank"].drawToHUD(0, optionPos.x + 6, optionPos.y - 8);

				if (eTankDelay > 0) {
					GameMode.drawWeaponSlotCooldown(optionPos.x + 14, optionPos.y, eTankDelay / maxETankDelay);
				} else {
					Point topLeftBar = new Point(spritePos.x + 1, spritePos.y + 2);
					Point botRightBar = new Point(spritePos.x + 15, spritePos.y + 14);
					float yPos =  12 * (etank.health / etank.maxHealth);
					DrawWrappers.DrawRect(
						topLeftBar.x, topLeftBar.y, botRightBar.x, botRightBar.y - yPos,
						true, new Color(0, 0, 0, 200), 1, ZIndex.HUD, isWorldPos: false
					);
				}
			} else {
				Global.sprites["menu_etank"].drawToHUD(0, spritePos.x, spritePos.y);
				Point topLeftBar = new Point(spritePos.x, spritePos.y);
				Point botRightBar = new Point(spritePos.x + 16, spritePos.y + 16);

				DrawWrappers.DrawRect(
					topLeftBar.x + 1, topLeftBar.y, botRightBar.x - 1, botRightBar.y,
					true, new Color(0, 0, 0, 128), 1, ZIndex.HUD, isWorldPos: false
				);
			}

			FontType font = canUseEtank ? FontType.Blue : FontType.Red;
			if (owned) {
				if (!canUseEtank) {
					useString = "CANNOT USE E-TANK";
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
				string costStr = $" ({eTankCost} {Global.nameCoins})";
				int posOffset = Fonts.measureText(FontType.Grey, useString);
				Fonts.drawText(FontType.White, costStr, optionPos.x + 24 + posOffset, optionPos.y - 4);
			}
		}

		//WTANKS RENDER
		for (int i = 0; i < getMaxWTanks(); i++) {
			if (i > mainPlayer.wtanks.Count) continue;
			bool canUseWtank = true;
			bool buyOrUse = mainPlayer.wtanks.Count < i + 1;
			string buyOrUseStr = buyOrUse ? "BUY W-TANK" : "USE W-TANK";
			var optionPos = new Point(optionPositionsX[1], optionPositionsY[i]);
			if (!buyOrUse) {
				var wtank = mainPlayer.wtanks[i];

				Global.sprites["menu_wtank"].drawToHUD(0, optionPos.x + 6, optionPos.y - 8);
				if (!canUseWTankInMenu(canUseWtank)) {
					if (canUseWtank) {
						GameMode.drawWeaponSlotCooldown(optionPos.x + 13, optionPos.y, wTankDelay / maxWTankDelay);
						if (wTankTargets.Count == 0) {
							buyOrUseStr = "CANNOT USE W-TANK";
						}
					} else {
						Global.sprites["menu_wtank"].drawToHUD(2, optionPos.x + 6, optionPos.y - 8, 0.5f);
					}
				}
				if (selectArrowPosY == i && wTankTargets.Count > 0) {
					if (!wTankTargets.InRange(wTankTargetIndex)) wTankTargetIndex = 0;

					var currentTarget = wTankTargets[wTankTargetIndex];
					
					float targetXPos = 113;
					if (wTankTargets.Count > 1 && isUsingWTank) {
						Global.sprites["hud_weapon_icon"].drawToHUD(currentTarget.weaponSlotIndex, optionPos.x + targetXPos + 5, optionPos.y);
						if (Global.frameCount % 60 < 30) {
							Fonts.drawText(FontType.Grey, "<", optionPos.x + targetXPos - 7, optionPos.y - 4, Alignment.Center);
							Fonts.drawText(FontType.Grey, ">", optionPos.x + targetXPos + 18, optionPos.y - 4, Alignment.Center);
						}
					}
				}
			} else {
				Global.sprites["menu_wtank"].drawToHUD(1, optionPos.x + 6, optionPos.y - 8);
			}
			if (!buyOrUse) {
				if (!canUseWtank && wTankTargets.Count == 0) buyOrUseStr = "CANNOT USE W-TANK";
				Fonts.drawText(FontType.Blue, buyOrUseStr, optionPos.x + 24, optionPos.y - 4,
				selected: selectArrowPosY == i && selectArrowPosX == 1
			);
			} else {
				Fonts.drawText(
					FontType.Blue, buyOrUseStr, optionPos.x + 24, optionPos.y - 4,
					selected: selectArrowPosY == i && selectArrowPosX == 1
				);
			}
			if (buyOrUse) {
				string costStr = $" ({wTankCost} {Global.nameCoins})";
				int posOffset = Fonts.measureText(FontType.Grey, buyOrUseStr);
				Fonts.drawText(FontType.White, costStr, optionPos.x + 24 + posOffset, optionPos.y - 4);
			}
			//if (buyOrUse) Fonts.drawText(FontType.Grey, $" ({eTankCost} bolts)", optionPos.x + 93, optionPos.y);
		}

		if (eTankTargets.Count > 1 && selectArrowPosY > 0) {
			Fonts.drawText(FontType.Grey, "Left/Right: Change Heal Target", Global.halfScreenW, 202, Alignment.Center);
		}

		drawAdaptorUpgrades(mainPlayer, 20);

		Fonts.drawTextEX(
			FontType.Blue, "[MUP]/[MDOWN]: Select Item",
			Global.halfScreenW, Global.screenH - 28, Alignment.Center
		);
		Fonts.drawTextEX(
			FontType.Blue, "[OK]: Buy/Use, [BACK]: Back",
			Global.halfScreenW, Global.screenH - 18, Alignment.Center
		);
	}

	public static bool updateAdaptorUpgrades(Player mainPlayer) {
		if (mainPlayer.character == null) return false;
		if (mainPlayer.character.charState is NovaStrikeState) return false;

		if (mainPlayer.character is not Rock rock) {
			return false;
		}

		if (Global.input.isPressedMenu(Control.Special2)) {
			if (rock.canGoSuperAdaptor()) {
				if (!rock.boughtSuperAdaptorOnce) {
					mainPlayer.currency -= Rock.SuperAdaptorCost;
					rock.boughtSuperAdaptorOnce = true;
				}
				mainPlayer.character.changeState(new CallDownRush(), true);
				//mainPlayer.setSuperAdaptor(true);
				//Global.playSound("chingX4");
				return true;
			} else Global.playSound("error");
		}
		return false;
	}
	
	public static void drawAdaptorUpgrades(Player mainPlayer, int offY) {
		if (mainPlayer.character == null) return;
		if (mainPlayer.character is not Rock) return;

		if (mainPlayer.character is not Rock rock) {
			return;
		}

		bool hasAdaptor = rock.hasSuperAdaptor;

		string specialText = "[CMD]: Super Adaptor" + 
			$" ({Rock.SuperAdaptorCost} {Global.nameCoins})";
		if (hasAdaptor) specialText = "Super Adaptor: Activated";
		/*specialText = (
			rock.boughtSuperAdaptorOnce ? "" : "[CMD]: Super Adaptor" + 
			$" ({Player.superAdaptorCost} {Global.nameCoins})"
		);*/
		if (rock.canGoSuperAdaptor() && mainPlayer.isRock) {
			
		} 
		
		float yPosb = Global.halfScreenH + 9;

		DrawWrappers.DrawLine(
			7, yPosb + offY, Global.screenW - 7,yPosb + offY, 
			new Color(232, 232, 232, 224), 1, ZIndex.HUD, false
		);

		DrawWrappers.DrawRect(
			7, yPosb + offY, Global.screenW - 7, yPosb + 30 + offY,
			true, Helpers.MenuBgColor, 0, ZIndex.HUD + 200, false
		);

		DrawWrappers.DrawLine(
			7, yPosb + 30 + offY, Global.screenW - 7, yPosb + 30 + offY, 
			new Color(232, 232, 232, 224), 1, ZIndex.HUD, false
		);

		if (!string.IsNullOrEmpty(specialText)) {
			specialText = specialText.TrimStart('\n');
			float yOff = specialText.Contains('\n') ? -3 : 0;
			float yPos = Global.halfScreenH + 9;
			float extraOffset = mainPlayer.currency >= Rock.SuperAdaptorCost ? 11 : 4;
			
			Fonts.drawText(
				hasAdaptor ? FontType.Green : FontType.Orange,
				Helpers.controlText(specialText).ToUpperInvariant(),
				Global.halfScreenW, yPos + extraOffset + yOff + offY, Alignment.Center
			);
		}

		specialText.ToUpperInvariant();

		if (mainPlayer.currency < Rock.SuperAdaptorCost && !rock.hasSuperAdaptor) {
			float yOff = specialText.Contains('\n') ? -3 : 0;
			float yPos = Global.halfScreenH + 9;
			Fonts.drawText(
				FontType.Red, Helpers.controlText("(Not enough bolts)").ToUpperInvariant(),
				Global.halfScreenW, yPos + 18 + yOff + offY, Alignment.Center
			);
		}
	
	}
}
