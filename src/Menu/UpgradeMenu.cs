using System;
using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;

namespace MMXOnline;

public class UpgradeMenu : IMainMenu {
	public static int selectArrowPosY;
	public IMainMenu prevMenu;
	public static bool onUpgradeMenu = true;
	public int eTankCost = 40;
	public int wTankCost = 20;
	public List<Weapon> eTankTargets = new List<Weapon>();
	public static int eTankTargetIndex;
	public int startX = 25;
	public bool isFillingETank;
	public static float eTankDelay = 0;
	public const float maxETankDelay = 2;

	public List<Point> optionPositions = new List<Point>();

	public UpgradeMenu(IMainMenu prevMenu) {
		this.prevMenu = prevMenu;
		optionPositions.Add(new Point(startX, 60));
		optionPositions.Add(new Point(startX, 80));
		optionPositions.Add(new Point(startX, 100));
		optionPositions.Add(new Point(startX, 120));
		optionPositions.Add(new Point(startX, 140));

		if (selectArrowPosY >= Global.level.mainPlayer.etanks.Count + 1) {
			selectArrowPosY = Global.level.mainPlayer.etanks.Count;
		}

		/* 		if (selectArrowPosY >= Global.level.mainPlayer.wtanks.Count + 1) {
			selectArrowPosY = Global.level.mainPlayer.wtanks.Count;
		} */
	}

	public int getMaxIndex() {
		var mainPlayer = Global.level.mainPlayer;
		return Math.Clamp(1 + mainPlayer.etanks.Count, 1, getMaxETanks());
	}

	public int getHeartTankCost() {
		// if (Global.level.server?.customMatchSettings?.heartTankHp == 2) return 4;
		return 2;
	}

	public int getMaxHeartTanks() {
		return Global.level.server?.customMatchSettings?.maxHeartTanks ?? 4;
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

	public void update() {
		if (updateAdaptorUpgrades(mainPlayer)) return;

		eTankTargets.Clear();
		if (mainPlayer.isSigma) {
			if (mainPlayer.isTagTeam()) {
				if (mainPlayer.currentMaverick != null) {
					var currentMaverickWeapon = mainPlayer.weapons.FirstOrDefault(
						w => w is MaverickWeapon mw && mw.maverick == mainPlayer.currentMaverick
					);
					if (currentMaverickWeapon != null) {
						eTankTargets.Add(currentMaverickWeapon);
					}
				}
			} else if (!mainPlayer.isStriker()) {
				eTankTargets = mainPlayer.weapons.FindAll(
					w => (w is MaverickWeapon mw && mw.maverick != null) || w is SigmaMenuWeapon
				).ToList();
			}
		}

		if (eTankTargets.Count > 0 && selectArrowPosY >= 1) {
			Helpers.menuLeftRightInc(ref eTankTargetIndex, 0, eTankTargets.Count - 1);
		}

		if (!eTankTargets.InRange(eTankTargetIndex)) eTankTargetIndex = 0;

		if (Global.input.isPressedMenu(Control.MenuLeft)) {
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

		if (Global.input.isPressedMenu(Control.MenuConfirm)) {
			/*if (selectArrowPosY == 0) {
				if (mainPlayer.heartTanks < getMaxHeartTanks() && mainPlayer.scrap >= getHeartTankCost()) {
					mainPlayer.scrap -= getHeartTankCost();
					mainPlayer.heartTanks++;
					Global.playSound("hearthX1");
					float currentMaxHp = mainPlayer.maxHealth;
					mainPlayer.maxHealth = mainPlayer.getMaxHealth();
					mainPlayer.character?.addHealth(mainPlayer.maxHealth - currentMaxHp);
					/*
					if (mainPlayer.isVile && mainPlayer.character?.vileStartRideArmor != null)
					{
						mainPlayer.character.vileStartRideArmor.addHealth(mainPlayer.getHeartTankModifier());
					}
					else if (mainPlayer.isSigma && mainPlayer.currentMaverick != null)
					{
						mainPlayer.currentMaverick.addHealth(mainPlayer.getHeartTankModifier(), false);
						mainPlayer.currentMaverick.maxHealth += mainPlayer.getHeartTankModifier();
					}
					
				}
			} */
			if (mainPlayer.etanks.Count <= selectArrowPosY) {
				if (mainPlayer.etanks.Count < getMaxETanks() && mainPlayer.currency >= eTankCost) {
					mainPlayer.currency -= eTankCost;
					mainPlayer.etanks.Add(new ETank());
					Global.playSound("upgrade");
				} else if (mainPlayer.etanks.InRange(selectArrowPosY)) {
					bool maverickUsed = false;
					if (eTankTargets.Count > 0) {
						var currentTarget = eTankTargets[eTankTargetIndex];
						if (currentTarget is MaverickWeapon mw && canUseETankInMenu(mw.canUseEtank(mainPlayer.etanks[selectArrowPosY - 1]))) {
							mainPlayer.etanks[selectArrowPosY - 1].use(mw.maverick);
							maverickUsed = true;
						}
					}

					if (!maverickUsed && canUseETankInMenu(mainPlayer.canUseEtank(mainPlayer.etanks[selectArrowPosY]))) {
						mainPlayer.etanks[selectArrowPosY - 1].use(mainPlayer, mainPlayer.character);
						mainPlayer.etanks.RemoveAt(selectArrowPosY - 1);
					}
				}
			}
			else if (mainPlayer.etanks.InRange(selectArrowPosY)) {
					bool maverickUsed = false;
					if (eTankTargets.Count > 0) {
						var currentTarget = eTankTargets[eTankTargetIndex];
						if (currentTarget is MaverickWeapon mw && canUseETankInMenu(mw.canUseEtank(mainPlayer.etanks[selectArrowPosY - 1]))) {
							mainPlayer.etanks[selectArrowPosY].use(mw.maverick);
							maverickUsed = true;
						}
					}

					if (!maverickUsed && canUseETankInMenu(mainPlayer.canUseEtank(mainPlayer.etanks[selectArrowPosY]))) {
						mainPlayer.etanks[selectArrowPosY].use(mainPlayer, mainPlayer.character);
						mainPlayer.etanks.RemoveAt(selectArrowPosY);
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

		Global.sprites["cursor"].drawToHUD(0, optionPositions[0].x - 8, optionPositions[selectArrowPosY].y + 4);

		Fonts.drawText(FontType.Grey, "UPGRADE MENU", Global.screenW * 0.5f, 16, Alignment.Center);
		Fonts.drawText(
			FontType.LigthGrey,
			Global.nameCoins + ": " + mainPlayer.currency,
			Global.screenW * 0.5f, 26, Alignment.Center
		);
		int maxHeartTanks = getMaxHeartTanks();
		/*for (int i = 0; i < maxHeartTanks; i++) {
			bool isBought = mainPlayer.heartTanks > i;
			Global.sprites["menu_hearttank"].drawToHUD(isBought ? 0 : 2, 71 + (i * 20) + ((8 - maxHeartTanks) * 10), 37);
		}*/

		if (Global.frameCount % 60 < 30 && mainPlayer.realCharNum == 2) {
			Fonts.drawText(FontType.DarkPurple, ">", Global.screenW - 14, Global.halfScreenH, Alignment.Center);
			//Fonts.drawText(FontType.DarkPurple, "Armor", Global.screenW - 25, Global.halfScreenH + 15, Alignment.Center);
		} else if (Global.frameCount % 60 < 30 && mainPlayer.canUpgradeXArmor()) {
			Fonts.drawText(FontType.DarkPurple, "<", 14, Global.halfScreenH, Alignment.Center);
			//Fonts.drawText(FontType.DarkPurple, "X3", 12, Global.halfScreenH + 15, Alignment.Center);

			Fonts.drawText(FontType.DarkPurple, ">", Global.screenW - 14, Global.halfScreenH, Alignment.Center);
			//Fonts.drawText(FontType.DarkPurple, "X1", Global.screenW - 19, Global.halfScreenH + 15, Alignment.Center);
		}

		bool soldOut = false;
		int textX = 48;

		if (mainPlayer.heartTanks >= getMaxHeartTanks()) soldOut = true;
		string heartTanksStr = soldOut ? "SOLD OUT" : "Buy Heart Tank";
		//Global.sprites["menu_hearttank"].drawToHUD(heartTanksStr == "SOLD OUT" ? 1 : 0, optionPositions[0].x, optionPositions[0].y - 4);
		//Point size = Helpers.measureTextStd(TCat.Option, heartTanksStr, fontSize: 24);
		//Helpers.drawTextStd(TCat.Option, heartTanksStr, optionPositions[0].x + 20, optionPositions[0].y, fontSize: 24, color: soldOut ? Helpers.Gray : Color.White, selected: selectArrowPosY == 0);
		/*if (!soldOut) {
			string heartTankCostStr = string.Format(" ({0} scrap)", getHeartTankCost());
			Helpers.drawTextStd(heartTankCostStr, optionPositions[0].x + 20 + size.x, optionPositions[0].y, fontSize: 24, color: soldOut ? Helpers.Gray : (mainPlayer.scrap < getHeartTankCost() ? Color.Red : Color.Green));
		}*/

		for (int i = 0; i < getMaxETanks(); i++) {
			if (i > mainPlayer.etanks.Count) continue;
			bool canUseEtank = true;
			bool buyOrUse = mainPlayer.etanks.Count < i + 1;
			string buyOrUseStr = buyOrUse ? "BUY E-TANK" : "USE E-TANK";
			var optionPos = optionPositions[i];
			if (!buyOrUse) {
				var etank = mainPlayer.etanks[i];
				canUseEtank = mainPlayer.canUseEtank(etank);
				if (mainPlayer.currentMaverick != null && mainPlayer.isTagTeam()) {
					canUseEtank = mainPlayer.currentMaverickWeapon.canUseEtank(etank);
				}

				Global.sprites["menu_subtank"].drawToHUD(0, optionPos.x - 2, optionPos.y - 4);
				//Global.sprites["menu_"].drawToHUD(0, optionPos.x + 5, optionPos.y - 3);
				float yPos = 14 * (etank.health / ETank.maxHealth);
				//DrawWrappers.DrawRect(optionPos.x + 5, optionPos.y - 3, optionPos.x + 9, optionPos.y + 11 - yPos, true, Color.Black, 1, ZIndex.HUD, isWorldPos: false);

				if (!canUseETankInMenu(canUseEtank)) {
					if (canUseEtank) {
						GameMode.drawWeaponSlotCooldown(optionPos.x + 7, optionPos.y + 4, eTankDelay / maxETankDelay);
						if (eTankTargets.Count == 0) {
							buyOrUseStr = "CANNOT USE E-TANK IN BATTLE";
						}
					} else {
						Global.sprites["menu_subtank"].drawToHUD(2, optionPos.x - 2, optionPos.y - 4, 0.5f);
					}
				}

				if (selectArrowPosY == i + 1 && eTankTargets.Count > 0) {
					if (!eTankTargets.InRange(eTankTargetIndex)) eTankTargetIndex = 0;

					var currentTarget = eTankTargets[eTankTargetIndex];
					if (currentTarget is MaverickWeapon mw) {
						canUseEtank = mw.canUseEtank(etank);
					}
					float targetXPos = 113;
					if (eTankTargets.Count > 1) {
						Global.sprites["hud_weapon_icon"].drawToHUD(currentTarget.weaponSlotIndex, optionPos.x + targetXPos, optionPos.y + 4);
						if (Global.frameCount % 60 < 30) {
							Fonts.drawText(FontType.DarkPurple, "<", optionPos.x + targetXPos - 12, optionPos.y - 2, Alignment.Center);
							Fonts.drawText(FontType.DarkPurple, ">", optionPos.x + targetXPos + 12, optionPos.y - 2, Alignment.Center);
						}
					}
				}
			} else {
				Global.sprites["menu_subtank"].drawToHUD(1, optionPos.x - 2, optionPos.y - 4);
			}
			if (!buyOrUse) {
				if (!canUseEtank && eTankTargets.Count == 0) buyOrUseStr = "CANNOT USE E-TANK NOW";
				Fonts.drawText(FontType.Grey, buyOrUseStr, optionPos.x + 20, optionPos.y);
			} else {
				Fonts.drawText(
					FontType.Grey, buyOrUseStr, textX, optionPos.y,
					selected: selectArrowPosY == i + 1
				);
			}
			if (buyOrUse) {
				string costStr = $" ({eTankCost} {Global.nameCoins})";
				int posOffset = Fonts.measureText(FontType.Grey, buyOrUseStr);
				Fonts.drawText(FontType.Grey, costStr, textX + posOffset, optionPos.y);
			}
			//if (buyOrUse) Fonts.drawText(FontType.Grey, $" ({eTankCost} bolts)", optionPos.x + 93, optionPos.y);
		}

		if (eTankTargets.Count > 1 && selectArrowPosY > 0) {
			Fonts.drawText(FontType.Grey, "Left/Right: Change Heal Target", Global.halfScreenW, 202, Alignment.Center);
		}

		drawAdaptorUpgrades(mainPlayer, 20);

		Fonts.drawTextEX(
			FontType.Grey, "[MUP]/[MDOWN]: Select Item",
			Global.halfScreenW, Global.screenH - 28, Alignment.Center
		);
		Fonts.drawTextEX(
			FontType.Grey, "[OK]: Buy/Use, [BACK]: Back",
			Global.halfScreenW, Global.screenH - 18, Alignment.Center
		);
	}

	public static bool updateAdaptorUpgrades(Player mainPlayer) {
		if (mainPlayer.character == null) return false;
		if (mainPlayer.character.charState is NovaStrikeState) return false;

		var rock = mainPlayer.character as Rock;

		if (Global.input.isPressedMenu(Control.Special2)) {
			if (mainPlayer.canGoSuperAdaptor()) {
				if (!rock.boughtSuperAdaptorOnce) {
					mainPlayer.currency -= Player.superAdaptorCost;
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
		if (mainPlayer.character.charState is NovaStrikeState) return;

		var rock = mainPlayer.character as Rock;

		string specialText = "[CMD]: Super Adaptor" + 
			$" ({Player.superAdaptorCost} {Global.nameCoins})";
		/*specialText = (
			rock.boughtSuperAdaptorOnce ? "" : "[CMD]: Super Adaptor" + 
			$" ({Player.superAdaptorCost} {Global.nameCoins})"
		);*/
		if (mainPlayer.canGoSuperAdaptor() && mainPlayer.isRock) {
			
		} 


		float yPosb = Global.halfScreenH + 9;
		DrawWrappers.DrawRect(
			5, yPosb + offY, Global.screenW - 5, yPosb + 30 + offY,
			true, Helpers.MenuBgColor, 0, ZIndex.HUD + 200, false
		);

		if (!string.IsNullOrEmpty(specialText)) {
			specialText = specialText.TrimStart('\n');
			float yOff = specialText.Contains('\n') ? -3 : 0;
			float yPos = Global.halfScreenH + 9;
			float extraOffset = mainPlayer.currency >= Player.superAdaptorCost ? 11 : 4;
			
			Fonts.drawText(
				FontType.Grey, Helpers.controlText(specialText).ToUpperInvariant(),
				Global.halfScreenW, yPos + extraOffset + yOff + offY, Alignment.Center
			);
		}

		specialText.ToUpperInvariant();

		if (mainPlayer.currency < Player.superAdaptorCost) {
			float yOff = specialText.Contains('\n') ? -3 : 0;
			float yPos = Global.halfScreenH + 9;
			Fonts.drawText(
				FontType.Red, Helpers.controlText("(Not enough bolts)").ToUpperInvariant(),
				Global.halfScreenW, yPos + 18 + yOff + offY, Alignment.Center
			);
		}
	
	}
}
