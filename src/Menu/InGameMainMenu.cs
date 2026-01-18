
using SFML.Graphics;

namespace MMXOnline;

public class InGameMainMenu : IMainMenu {
	public static int selectY = 0;
	public int[] optionPos = {
		48,
		66,
		84,
		102,
		120,
		138,
		156,
		174
	};
	public float startX = 92;

	public InGameMainMenu() {
	}

	public Player mainPlayer { get { return Global.level.mainPlayer; } }

	public void update() {
		if (!mainPlayer.canUpgradeXArmor()) {
			UpgradeMenu.onUpgradeMenu = true;
		}

		Helpers.menuUpDown(ref selectY, 0, 7);
		if (Global.input.isPressedMenu(Control.MenuConfirm)) {
			if (selectY == 0) {
				int selectedCharNum = Global.level.mainPlayer.newCharNum;
				if (Global.level.mainPlayer.character != null &&
					Global.level.mainPlayer.character.charState is not Die
				) {
					selectedCharNum = (int)Global.level.mainPlayer.character.charId;
				}
				if (isSelWepDisabled()) return;
				if (selectedCharNum == (int)CharIds.Rock) {
					Menu.change(new RockWeaponMenu(this, true));
				}  else if (selectedCharNum == (int)CharIds.Blues) {
					Menu.change(new BluesWeaponMenu(this, true));
				} else if (selectedCharNum == (int)CharIds.Bass) {
					Menu.change(new BassWeaponMenu(this, true));
				}
			} else if (selectY == 1) {
				if (isSelArmorDisabled()) return;
				if (Global.level.mainPlayer.realCharNum == 0 || Global.level.mainPlayer.realCharNum == 2) {
					if (UpgradeMenu.onUpgradeMenu && !Global.level.server.disableHtSt) {
						Menu.change(new UpgradeMenu(this));
					} else if (Global.level.mainPlayer.realCharNum == 0) {
						Menu.change(new UpgradeArmorMenu(this));						
					} else if (Global.level.mainPlayer.realCharNum == 2) {
						Menu.change(new SelectVileArmorMenu(this));
					}
				} else {
					if (!Global.level.server.disableHtSt) {
						if (Global.level.mainPlayer.realCharNum == (int)CharIds.Blues) {
							Menu.change(new BluesUpgradeMenu(this));
						} else Menu.change(new UpgradeMenu(this));
					}
				}
			} else if (selectY == 2) {
				bool hasHypermode = (
					mainPlayer.character is Rock { hasSuperAdaptor: true} ||
					mainPlayer.character is Blues { isBreakMan: true } ||
					mainPlayer.character is Bass { isSuperBass: true } ||
					mainPlayer.character is Bass { isTrebbleBoost: true } 
				);
				bool hasShield = mainPlayer.character is Blues { isShieldActive: true };
				Menu.change(
					MovelistMenuHandler.getCharMovelistMenu(
						this, true, Global.level.mainPlayer.realCharNum, hasHypermode, hasShield
					)
				);
			} else if (selectY == 3) {
				if (isSelCharDisabled()) return;
				Menu.change(new SelectCharacterMenu(this, Global.level.is1v1(), Global.serverClient == null, true, false, Global.level.gameMode.isTeamMode, Global.isHost, () => { Menu.exit(); }));
			} else if (selectY == 4) {
				if (isMatchOptionsDisabled()) return;
				Menu.change(new MatchOptionsMenu(this));
			} else if (selectY == 5) {
				Menu.change(new PreControlMenu(this, true));
			} else if (selectY == 6) {
				Menu.change(new PreOptionsMenu(this, true));
			} else if (selectY == 7) {
				Menu.change(new ConfirmLeaveMenu(this, "Are you sure you want to leave?", () => {
					Global._quickStart = false;
					Global.leaveMatchSignal = new LeaveMatchSignal(LeaveMatchScenario.LeftManually, null, null);
				}));
			}
		} else if (Global.input.isPressedMenu(Control.MenuBack)) {
			Menu.exit();
		}
	}

	public bool isSelWepDisabled() {
		return Global.level.is1v1() || mainPlayer?.realCharNum == (int)CharIds.BusterZero;
	}

	public bool isSelArmorDisabled() {
		if (Global.level.is1v1()) return true;
		if (mainPlayer.realCharNum == 2) return false;
		if (Global.level.server.disableHtSt) {
			if (mainPlayer.realCharNum != 0) return Global.level.server.disableHtSt;
			if (mainPlayer.canUpgradeXArmor()) {
				return false;
			} else {
				return Global.level.server.disableHtSt;
			}
		}
		return false;
	}

	public bool isSelCharDisabled() {
		if (Global.level.isElimination()) return true;

		if (Global.level.server?.customMatchSettings?.redSameCharNum > 4) {
			if (Global.level.gameMode.isTeamMode && Global.level.mainPlayer.alliance == GameMode.redAlliance) {
				return true;
			}
		}
		if (Global.level.server?.customMatchSettings?.sameCharNum > 4) {
			if (!Global.level.gameMode.isTeamMode || Global.level.mainPlayer.alliance == GameMode.blueAlliance) {
				return true;
			}
		}

		return false;
	}

	public bool isMatchOptionsDisabled() {
		return false;
	}

	public void render() {
		DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
		Fonts.drawText(FontType.BlueMenu, "MENU", Global.screenW * 0.5f, 20, Alignment.Center);

		Global.sprites["cursor"].drawToHUD(0, startX - 10, optionPos[selectY] + 4);

		Fonts.drawText(
			isSelWepDisabled() ? FontType.Black : FontType.Blue,
			"EDIT LOADOUT", startX, optionPos[0], selected: selectY == 0
		);
		Fonts.drawText(
			isSelArmorDisabled() ? FontType.Black : FontType.Blue,
			"UPGRADE MENU", startX, optionPos[1], selected: selectY == 1
		);
		Fonts.drawText(
			FontType.Blue, "CHARACTER MOVELIST", startX, optionPos[2], selected: selectY == 2
		);
		Fonts.drawText(
			isSelCharDisabled() ? FontType.Black : FontType.Blue,
			"SWITCH CHARACTER", startX, optionPos[3], selected: selectY == 3
		);
		Fonts.drawText(
			isMatchOptionsDisabled() ? FontType.Black : FontType.Blue,
			"MATCH OPTIONS", startX, optionPos[4], selected: selectY == 4
		);
		Fonts.drawText(FontType.Blue, "CONTROLS", startX, optionPos[5], selected: selectY == 5);
		Fonts.drawText(FontType.Blue, "SETTINGS", startX, optionPos[6], selected: selectY == 6);
		Fonts.drawText(FontType.Blue, "LEAVE MATCH", startX, optionPos[7], selected: selectY == 7);
		Fonts.drawTextEX(FontType.Blue, "[OK]: Choose, [ESC]: Cancel", Global.halfScreenW, 198, Alignment.Center);

		Color outline = new Color(41, 41, 41);
		MasteryTracker mastery = Global.level.mainPlayer.mastery;

		drawLevelBar(
			$"ATK", Global.screenW - 76, 8,
			FontType.RedSmall, new Color(255, 115, 127),
			mastery.damageLevelStacks, MasteryTracker.getStacksPerLevel(mastery.damageLevel),
			mastery.damageExp, mastery.damageLvLimit, mastery.damageLevel
		);
		drawLevelBar(
			$"DEF", Global.screenW - 76, 23,
			FontType.BlueSmall, new Color(66, 206, 239),
			mastery.defenseLevelStacks, MasteryTracker.getStacksPerLevel(mastery.defenseLevel),
			mastery.defenseExp, mastery.defenseLvLimit, mastery.defenseLevel
		);
		drawLevelBar(
			$"SP", Global.screenW - 76, 38,
			FontType.GreenSmall, new Color(123, 231, 148),
			mastery.supportLevelStacks, MasteryTracker.getStacksPerLevel(mastery.supportLevel),
			mastery.supportExp, mastery.supportLvLimit, mastery.supportLevel
		);
		drawLevelBar(
			$"MAP", Global.screenW - 76, 53,
			FontType.PurpleSmall, new Color(189, 115, 214),
			mastery.mapLevelStacks, MasteryTracker.getStacksPerLevel(mastery.mapLevel),
			mastery.mapExp, mastery.mapLvLimit, mastery.mapLevel
		);
	}

	public void drawLevelBar(
		string text, float posX, float posY, FontType font, Color barColor,
		int stacks, int segments, float exp, float cap, int level
	) {
		Color outline = new Color(41, 41, 41);
		Fonts.drawText(font, text, posX, posY);
		Fonts.drawText(font, $"L{level}", posX + 66, posY, Alignment.Right);
		DrawWrappers.DrawRectWH(
			posX, posY + 9, 66, 6, true, outline, 0, ZIndex.HUD, false
		);
		int spacing = 0;
		if (segments > 1) {
			spacing = segments - 1; 
		}
		int currentOffset = 0;
		int barSize = MathInt.Round((64f - spacing) / segments);
		for (int i = 0; i <= stacks - 1; i++) {
			DrawWrappers.DrawRectWH(
				posX + 1 + currentOffset,
				posY + 10, barSize, 4, true, barColor, 0, ZIndex.HUD, false
			);
			currentOffset += barSize + 1;
		}
		float progress = exp / cap;
		float finalSize = barSize;
		if (segments == stacks + 1) {
			finalSize = 64 - currentOffset;
		}
		finalSize *= progress;
		DrawWrappers.DrawRectWH(
			posX + 1 + currentOffset, posY + 10,
			finalSize, 4, true, barColor, 0, ZIndex.HUD, false
		);
	}
}
