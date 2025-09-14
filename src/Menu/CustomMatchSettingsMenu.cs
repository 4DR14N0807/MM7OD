using System.Collections.Generic;
using ProtoBuf;

namespace MMXOnline;

[ProtoContract]
public class CustomMatchSettings {
	[ProtoMember(1)] public bool hyperModeMatch;
	[ProtoMember(2)] public int startCurrency = 0;
	[ProtoMember(3)] public int startHeartTanks;
	[ProtoMember(4)] public int startSubTanks;
	[ProtoMember(5)] public int healthModifier;
	[ProtoMember(5)] public int damageModifier;
	[ProtoMember(6)] public int sameCharNum;
	[ProtoMember(7)] public int redSameCharNum;
	[ProtoMember(8)] public int maxHeartTanks;
	[ProtoMember(9)] public int startETanks;
	[ProtoMember(10)] public int startWTanks;
	[ProtoMember(11)] public int maxETanks;
	[ProtoMember(12)] public int maxWTanks;
	[ProtoMember(13)] public int heartTankHp;
	[ProtoMember(14)] public int heartTankCost;
	[ProtoMember(15)] public int currencyGain;
	[ProtoMember(16)] public int respawnTime;
	[ProtoMember(17)] public bool pickupItems;

	public CustomMatchSettings() {
	}

	public static CustomMatchSettings getDefaults() {
		return new CustomMatchSettings {
			hyperModeMatch = false,
			startCurrency = 10,
			startHeartTanks = 0,
			startETanks = 0,
			startWTanks = 0,
			healthModifier = 8,
			damageModifier = 1,
			maxETanks = 2,
			maxWTanks = 1,
			currencyGain = 8,
			respawnTime = 0,
			pickupItems = true,
			heartTankHp = 1,
			heartTankCost = 2,
		};
	}
}

public class CustomMatchSettingsMenu : IMainMenu {
	public int selectArrowPosY;
	public int selectArrowPosY2;
	public int selectArrowPosY3;
	public const int startX = 30;
	public int startY = 40;
	public const int lineH = 10;
	public const int startX2 = 30;
	public int startY2 = 40;
	public const int lineH2 = 10;
	public const int startX3 = 30;
	public int startY3 = 40;
	public const int lineH3 = 10;
	public const uint fontSize = 24;
	public IMainMenu prevMenu;
	public bool inGame;
	public int Page = 1;
	public bool isOffline;
	private FontType fontOption = FontType.Blue; 
	public List<MenuOption> menuOptions = new List<MenuOption>();
	public List<MenuOption> menuOptions2 = new List<MenuOption>();
	public List<MenuOption> menuOptions3 = new List<MenuOption>();


	SavedMatchSettings savedMatchSettings {
		get { return isOffline ? SavedMatchSettings.mainOffline : SavedMatchSettings.mainOnline; }
	}
	CustomMatchSettings cSettings => savedMatchSettings.customMatchSettings;

	public CustomMatchSettingsMenu(IMainMenu prevMenu, bool inGame, bool isOffline) {
		this.prevMenu = prevMenu;
		this.inGame = inGame;
		int currentY = startY;
		int currentY2 = startY2;
		int currentY3 = startY3;
		this.isOffline = isOffline;
		/*menuOptions.Add(
			new MenuOption(
				startX, currentY,
				() => {
					Helpers.menuLeftRightBool(ref cSettings.hyperModeMatch);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.RedishOrange,
						"Hypermode Match : " +
						Helpers.boolYesNo(cSettings.hyperModeMatch),
						pos.x, pos.y, selected: selectArrowPosY == index
					);
				}
			)
		);*/

		menuOptions.Add(
			new MenuOption(
				startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.startCurrency, 0, 1000, true, valueToAdd: 5);
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
		/*menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.heartTankHp, 1, 8, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Heart tank HP: " +
						savedMatchSettings.customMatchSettings.heartTankHp.ToString(),
						pos.x, pos.y, selected: selectArrowPosY == 2
					);
				}
			)
		);*/

		/*menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.startHeartTanks, 0, 8, true);
				},
				(Point pos, int index) => {
					
				}
			)
		);*/

		/*menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.maxHeartTanks, 0, 8, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Max heart tanks: " +
						savedMatchSettings.customMatchSettings.maxHeartTanks.ToString(),
						pos.x, pos.y, selected: selectArrowPosY == 4
					);
				}
			)
		);*/

		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.startETanks, 0, 4, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						fontOption,
						"START E-TANKS: " +
						savedMatchSettings.customMatchSettings.startETanks.ToString(),
						pos.x, pos.y, selected: selectArrowPosY == 1
					);
				}
			)
		);

		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.maxETanks, 0, 4, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						fontOption,
						"MAX E-TANKS: " +
						savedMatchSettings.customMatchSettings.maxETanks.ToString(),
						pos.x, pos.y, selected: selectArrowPosY == 2
					);
				}
			)
		);

		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.startWTanks, 0, 4, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						fontOption,
						"START W-TANKS: " +
						savedMatchSettings.customMatchSettings.startWTanks.ToString(),
						pos.x, pos.y, selected: selectArrowPosY == 3
					);
				}
			)
		);

		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.maxWTanks, 0, 4, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						fontOption,
						"MAX W-TANKS: " +
						savedMatchSettings.customMatchSettings.maxWTanks.ToString(),
						pos.x, pos.y, selected: selectArrowPosY == 4
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
						"HEALTH MODIFIER: " +
						((savedMatchSettings.customMatchSettings.healthModifier  / 8f) * 100).ToString() + 
						"%",
						pos.x, pos.y, selected: selectArrowPosY == 5
					);
				}
			)
		);
		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.damageModifier, 1, 4, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						fontOption,
						"DAMAGE MODIFIER: " +
						(savedMatchSettings.customMatchSettings.damageModifier * 100).ToString() + "%",
						pos.x, pos.y, selected: selectArrowPosY == 6
					);
				}
			)
		);
		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.sameCharNum, 10, 12);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						fontOption,
						"MONO CHARACTER: " +
						getSameCharString(savedMatchSettings.customMatchSettings.sameCharNum),
						pos.x, pos.y, selected: selectArrowPosY == 7
					);
				}
			)
		);

		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref savedMatchSettings.customMatchSettings.redSameCharNum, 10, 12);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						fontOption,
						"RED MONO CHARACTER: " +
						getSameCharString(savedMatchSettings.customMatchSettings.redSameCharNum),
						pos.x, pos.y, selected: selectArrowPosY == 8
					);
				}
			)
		);
		//Currency Gain Custom Setting
		menuOptions.Add(
			new MenuOption(
				startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(ref cSettings.currencyGain, 1, 10, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Green,
						Global.nameCoins + " gain modifier: " +
						cSettings.currencyGain.ToString(),
						pos.x, pos.y, selected: selectArrowPosY == index
					);
				}
			)
		);
		menuOptions.Add(
			new MenuOption(startX, currentY += lineH,
				() => {
					Helpers.menuLeftRightInc(
						ref savedMatchSettings.customMatchSettings.currencyGain, 1, 80, true
					);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Currency Gain modifier: " +
						((savedMatchSettings.customMatchSettings.currencyGain / 8f) * 100) + "%",
						pos.x, pos.y, selected: selectArrowPosY == 9
					);
				}
			)
		);
		//Respawn Time Custom Setting
		menuOptions2.Add(
			new MenuOption(
				startX2, currentY2 += lineH2,
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
						"Respawn Time modifier: " +
						text,
						pos.x, pos.y, selected: selectArrowPosY == 10
					);
				}
			)
		);
		menuOptions2.Add(
			new MenuOption(
				startX2, currentY2 += lineH2,
				() => {
					Helpers.menuLeftRightInc(ref cSettings.assistTime, 0, 6, true);
				},
				(Point pos, int index) => {
					Fonts.drawText(
						FontType.Blue,
						"Pick up items: " +
						Helpers.boolYesNo(savedMatchSettings.customMatchSettings.pickupItems),
						pos.x, pos.y, selected: selectArrowPosY == 11
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
		if (Global.input.isPressedMenu(Control.Special1)) {
			Page++;
			if (Page > 3) Page = 1;
		}
		if (Page == 1) {
			menuOptions[selectArrowPosY].update();
			Helpers.menuUpDown(ref selectArrowPosY, 0, menuOptions.Count - 1);
		} else if (Page == 2) {
			menuOptions2[selectArrowPosY2].update();
			Helpers.menuUpDown(ref selectArrowPosY2, 0, menuOptions2.Count - 1);
		}
		else if (Page == 3) {
			menuOptions3[selectArrowPosY3].update();
			Helpers.menuUpDown(ref selectArrowPosY3, 0, menuOptions3.Count - 1);
		}

		if (Global.input.isPressedMenu(Control.MenuBack)) {
			if (cSettings.maxHeartTanks < cSettings.startHeartTanks) {
				Menu.change(new ErrorMenu(new string[] { "Error: Max heart tanks can't be", "less than start heart tanks." }, this));
				return;
			}

			if (savedMatchSettings.customMatchSettings.maxETanks < savedMatchSettings.customMatchSettings.startETanks) {
				Menu.change(new ErrorMenu(new string[] { "Error: Max ETanks can't be", "less than start ETanks." }, this));
				return;
			}
			if (savedMatchSettings.customMatchSettings.maxWTanks < savedMatchSettings.customMatchSettings.startWTanks) {
				Menu.change(new ErrorMenu(new string[] { "Error: Max WTanks can't be", "less than start WTanks." }, this));
				return;
			}

			Menu.change(prevMenu);
		}
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
		if (Page == 1)
		foreach (var menuOption in menuOptions) {
			menuOption.render(menuOption.pos, i);
			i++;
		}
		if (Page == 2)
		foreach (var menuOption2 in menuOptions2) {
			menuOption2.render(menuOption2.pos, i);
			i++;
		}
		if (Page == 3)
		foreach (var menuOption3 in menuOptions3) {
			menuOption3.render(menuOption3.pos, i);
			i++;
		}
	}
	public void drawText() {
		Fonts.drawText(
			FontType.Yellow, "Custom Match Options",
			Global.halfScreenW, 20, alignment: Alignment.Center
		);
		Fonts.drawText(
			FontType.Yellow, "Page: " + Page,
			Global.halfScreenW+150, 20, alignment: Alignment.Center
		);
		Fonts.drawTextEX(
			FontType.Orange, "[MLEFT]/[MRIGHT]: Change setting, [BACK]: Back",
			Global.screenW * 0.5f, Global.screenH - 26, Alignment.Center
		);
	}
	public void Cursor() {
		if (Page == 1) {
			if (!inGame) {
				DrawWrappers.DrawTextureHUD(Global.textures["severbrowser"], 0, 0);
				DrawWrappers.DrawTextureHUD(
					Global.textures["cursor"], menuOptions[selectArrowPosY].pos.x - 8,
					menuOptions[selectArrowPosY].pos.y - 1
				);
			} else {
				DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
				Global.sprites["cursor"].drawToHUD(
					0, menuOptions[selectArrowPosY].pos.x - 8, menuOptions[selectArrowPosY].pos.y + 5
				);
			}
		} else if (Page == 2) {
			if (!inGame) {
				DrawWrappers.DrawTextureHUD(Global.textures["severbrowser"], 0, 0);
				DrawWrappers.DrawTextureHUD(
					Global.textures["cursor"], menuOptions2[selectArrowPosY2].pos.x - 8,
					menuOptions2[selectArrowPosY2].pos.y - 1
				);
			} else {
				DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
				Global.sprites["cursor"].drawToHUD(
					0, menuOptions2[selectArrowPosY2].pos.x - 8, menuOptions2[selectArrowPosY2].pos.y + 5
				);
			}
		}
		else if (Page == 3) {
			if (!inGame) {
				DrawWrappers.DrawTextureHUD(Global.textures["severbrowser"], 0, 0);
				DrawWrappers.DrawTextureHUD(
					Global.textures["cursor"], menuOptions3[selectArrowPosY3].pos.x - 8,
					menuOptions3[selectArrowPosY3].pos.y - 1
				);
			} else {
				DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
				Global.sprites["cursor"].drawToHUD(
					0, menuOptions3[selectArrowPosY3].pos.x - 8, menuOptions3[selectArrowPosY3].pos.y + 5
				);
			}
		}
	}
}
