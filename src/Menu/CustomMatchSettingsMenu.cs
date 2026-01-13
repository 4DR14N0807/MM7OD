using System.Collections.Generic;
using System.Text.Json.Serialization;
using ProtoBuf;

namespace MMXOnline;

[ProtoContract]
public class CustomMatchSettings {
	[ProtoMember(2)] public int startCurrency;
	[ProtoMember(5)] public int healthModifier;
	[ProtoMember(9)] public int startETanks;
	[ProtoMember(10)] public int startWTanks;
	[ProtoMember(11)] public int maxETanks;
	[ProtoMember(12)] public int maxWTanks;
	[ProtoMember(15)] public int currencyGain;
	[ProtoMember(16)] public int respawnTime;
	[ProtoMember(17)] public bool pickupItems;

	[JsonIgnore] public bool hyperModeMatch;
	[JsonIgnore] public int startHeartTanks;
	[JsonIgnore] public int startSubTanks;
	[JsonIgnore] public int sameCharNum;
	[JsonIgnore] public int redSameCharNum;
	[JsonIgnore] public int maxHeartTanks;
	[JsonIgnore] public int heartTankHp;
	[JsonIgnore] public int heartTankCost;
	[JsonIgnore] public int damageModifier;

	public CustomMatchSettings() {
	}

	public static CustomMatchSettings getDefaults() {
		return new CustomMatchSettings {
			hyperModeMatch = false,
			startCurrency = 25,
			startHeartTanks = 0,
			startETanks = 0,
			startWTanks = 0,
			healthModifier = 8,
			damageModifier = 1,
			maxETanks = 2,
			maxWTanks = 1,
			currencyGain = 1,
			respawnTime = 0,
			pickupItems = true,
			heartTankHp = 1,
			heartTankCost = 2,
		};
	}
}

public class CustomMatchSettingsMenu : IMainMenu {
	public int selectArrowPosY;
	public const int startX = 30;
	public int startY = 40;
	public const int lineH = 10;
	public const uint fontSize = 24;
	public IMainMenu prevMenu;
	public bool inGame;
	public int Page;
	public bool isOffline;
	private FontType fontOption = FontType.Blue; 
	public List<MenuOption> menuOptions = new List<MenuOption>();

	SavedMatchSettings savedMatchSettings => (
		isOffline ?
		SavedMatchSettings.mainOffline :
		SavedMatchSettings.mainOnline
	);

	public CustomMatchSettingsMenu(IMainMenu prevMenu, bool inGame, bool isOffline) {
		this.prevMenu = prevMenu;
		this.inGame = inGame;
		int currentY = startY;
		this.isOffline = isOffline;
		menuOptions.Add(
			new MenuOption(
				startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(
						ref savedMatchSettings.customMatchSettings.startCurrency,
						0, 900, true, valueToAdd: 5
					);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						fontOption,
						"START BOLTS" + ": " + //"Start " + Global.nameCoins + ": " +
						savedMatchSettings.customMatchSettings.startCurrency.ToString(),
						pos.x, pos.y, selected: selectArrowPosY == 0
					);
				}
			)
		);
		//Currency Gain Custom Setting
		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(
						ref savedMatchSettings.customMatchSettings.currencyGain, 1, 5, true
					);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"CURRENCY GAIN MODIFIER: x" +
						((savedMatchSettings.customMatchSettings.currencyGain + 1) * 0.5f),
						pos.x, pos.y, selected: selectArrowPosY == 1
					);
				}
			)
		);
		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.startETanks, 0, 5, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						fontOption,
						"START E-TANKS: " +
						savedMatchSettings.customMatchSettings.startETanks.ToString(),
						pos.x, pos.y, selected: selectArrowPosY == 2
					);
				}
			)
		);
		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.maxETanks, 0, 5, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						fontOption,
						"MAX E-TANKS: " +
						savedMatchSettings.customMatchSettings.maxETanks.ToString(),
						pos.x, pos.y, selected: selectArrowPosY == 3
					);
				}
			)
		);
		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.startWTanks, 0, 5, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						fontOption,
						"START W-TANKS: " +
						savedMatchSettings.customMatchSettings.startWTanks.ToString(),
						pos.x, pos.y, selected: selectArrowPosY == 4
					);
				}
			)
		);
		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.maxWTanks, 0, 5, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						fontOption,
						"MAX W-TANKS: " +
						savedMatchSettings.customMatchSettings.maxWTanks.ToString(),
						pos.x, pos.y, selected: selectArrowPosY == 5
					);
				}
			)
		);
		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.healthModifier, 1, 20);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						fontOption,
						"HEALTH MODIFIER: x" + 
						((savedMatchSettings.customMatchSettings.healthModifier  / 8f)).ToString(),
						pos.x, pos.y, selected: selectArrowPosY == 6
					);
				}
			)
		);
		//Respawn Time Custom Setting
		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.respawnTime, 0, 12, true);
				},
				(Point pos, int index) => {
					string text = savedMatchSettings.customMatchSettings.respawnTime.ToString();
					if (savedMatchSettings.customMatchSettings.respawnTime == 0) {
						text = "No";
					}
					Fonts.drawText(
						FontType.Blue,
						"RESPAWN TIME MODIFIER: " +
						text,
						pos.x, pos.y, selected: selectArrowPosY == 7
					);
				}
			)
		);
		//
		menuOptions.Add(
			new MenuOption(
				startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightBool(ref savedMatchSettings.customMatchSettings.pickupItems);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"PICK UP ITEMS: " +
						Helpers.boolYesNo(savedMatchSettings.customMatchSettings.pickupItems),
						pos.x, pos.y, selected: selectArrowPosY == 8
					);
				}
			)
		);
	}

	public string getSameCharString(int charNum) {
		return charNum switch {
			10 => "Megaman",
			11 => "Protoman",
			12 => "Bass",
			<10 => "No",
			_ => "ERROR"
		};
	}

	public void update() {
		Helpers.menuUpDown(ref selectArrowPosY, 0, menuOptions.Count - 1);
		if (Global.input.isPressedMenu(Control.MenuBack)) {
			if (savedMatchSettings.customMatchSettings.maxHeartTanks <
				savedMatchSettings.customMatchSettings.startHeartTanks
			) {
				Menu.change(
					new ErrorMenu(["Error: Max heart tanks can't be", "less than start heart tanks."], this)
				);
				return;
			}
			if (savedMatchSettings.customMatchSettings.maxETanks <
				savedMatchSettings.customMatchSettings.startETanks
			) {
				Menu.change(
					new ErrorMenu(["Error: Max ETanks can't be", "less than start ETanks."], this)
				);
				return;
			}
			if (savedMatchSettings.customMatchSettings.maxWTanks <
				savedMatchSettings.customMatchSettings.startWTanks
			) {
				Menu.change(
					new ErrorMenu(["Error: Max WTanks can't be", "less than start WTanks."], this)
				);
				return;
			}

			Menu.change(prevMenu);
		}

		menuOptions[selectArrowPosY].update();
	}

	public void render() {
		if (!inGame) {
			DrawWrappers.DrawTextureHUD(Global.textures["severbrowser"], 0, 0);
			/*DrawWrappers.DrawTextureHUD(
				Global.textures["cursor"], menuOptions[selectArrowPosY].pos.x - 8,
				menuOptions[selectArrowPosY].pos.y - 1
			);*/
			Global.sprites["cursor"].drawToHUD(0, menuOptions[selectArrowPosY].pos.x - 8,
				menuOptions[selectArrowPosY].pos.y + 3);
		} else {
			DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
			/*Global.sprites["cursor"].drawToHUD(
				0, menuOptions[selectArrowPosY].pos.x - 8, menuOptions[selectArrowPosY].pos.y + 5
			);*/
			Global.sprites["cursor"].drawToHUD(0, menuOptions[selectArrowPosY].pos.x - 8,
				menuOptions[selectArrowPosY].pos.y - 4);
		}

		Fonts.drawText(
			FontType.OrangeMenu, "Custom Match Options",
			Global.halfScreenW, 20, alignment: Alignment.Center
		);

		int i = 0;
		foreach (var menuOption in menuOptions) {
			menuOption.render(menuOption.pos, i);
			i++;
		}

		Fonts.drawTextEX(
			FontType.Orange, "[MLEFT]/[MRIGHT]: Change setting, [BACK]: Back",
			Global.screenW * 0.5f, Global.screenH - 26, Alignment.Center
		);
	}
}
