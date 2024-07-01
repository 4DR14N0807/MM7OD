using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;

public class AddBotMenu : IMainMenu {
	public int selectArrowPosY;
	public IMainMenu previous;

	public static int botCharNum = 5;
	public static int botTeamNum = -1;

	public List<Point> optionPoses = new List<Point>()
	{
			new Point(30, 100),
			new Point(30, 120),
		};
	public int ySep = 10;

	public AddBotMenu(IMainMenu mainMenu) {
		previous = mainMenu;
	}

	public void update() {
		if (Global.level.gameMode.isTeamMode) {
			Helpers.menuUpDown(ref selectArrowPosY, 0, 1);
		}
		if (selectArrowPosY == 0) {
			if (Global.input.isPressedMenu(Control.MenuLeft)) {
				botCharNum--;
				if (botCharNum == 4) {
					botCharNum = -1;
				}
				else if (botCharNum <= -1) {
					botCharNum = 7;
				}
			} else if (Global.input.isPressedMenu(Control.MenuRight)) {
				botCharNum++;
				if (botCharNum == 0) {
					botCharNum = 5;
				}
				else if (botCharNum > 7) {
					botCharNum = -1;
				}
			}
		}
		if (selectArrowPosY == 1 && teamOptionEnabled()) {
			if (Global.input.isPressedMenu(Control.MenuLeft)) {
				botTeamNum--;
				if (botTeamNum < -1) { botTeamNum = -1; }
			} else if (Global.input.isPressedMenu(Control.MenuRight)) {
				botTeamNum++;
				if (botTeamNum >= Global.level.teamNum) { botTeamNum = Global.level.teamNum - 1; }
			}
		}

		if (Global.input.isPressedMenu(Control.MenuConfirm)) {
			if (Global.serverClient != null) {
				RPC.addBot.sendRpc(botCharNum, botTeamNum);
			} else {
				int id = 0;
				int charNum = botCharNum;
				int alliance = botTeamNum;
				if (charNum == -1) {
					charNum = Helpers.randomRange(5, 7);
				}
				if (alliance == -1) {
					if (Global.level.gameMode.isTeamMode) {
						alliance = Server.getMatchInitAutobalanceTeam(Global.level.players, Global.level.teamNum);
					}
				}
				for (int i = 0; i < Global.level.players.Count + 1; i++) {
					if (!Global.level.players.Any(p => p.id == i)) {
						id = i;
						if (!Global.level.gameMode.isTeamMode) {
							alliance = id;
						}
						break;
					}
				}
				var cpu = new Player(
					"CPU" + id.ToString(), id, charNum, null, true, true, alliance, new Input(true), null
				);
				Global.level.players.Add(cpu);
				Global.level.gameMode.chatMenu.addChatEntry(new ChatEntry("Added bot " + cpu.name + ".", null, null, true));
			}
			Menu.change(previous);
		} else if (Global.input.isPressedMenu(Control.MenuBack)) {
			Menu.change(previous);
		}
	}

	public void render() {
		DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
		Global.sprites["cursor"].drawToHUD(0, 15, optionPoses[selectArrowPosY].y + 5);

		Fonts.drawText(FontType.BlueMenu, "Add Bot", Global.halfScreenW, 15, Alignment.Center);

		string botCharStr = botCharNum switch {
			-1 => "(Random)",
			0 => "Mega Man X",
			1 => "Zero",
			2 => "Vile",
			3 => "Axl",
			4 => "Sigma",
			5 => "Mega Man",
			6 => "Proto Man",
			7 => "Bass",
			_ => "Error"
		};

		Fonts.drawText(
			FontType.Grey, "CHARACTER: " + botCharStr,
			optionPoses[0].x, optionPoses[0].y, selected: selectArrowPosY == 0
		);
		if (Global.level.gameMode.isTeamMode) {
			string botTeamStr = "(Auto)";
			if (botTeamNum >= 0) {
				botTeamStr = GameMode.getTeamName(botTeamNum);
			}
			Fonts.drawText(
				teamOptionEnabled() ? FontType.Grey : FontType.Grey, "TEAM: " + botTeamStr,
				optionPoses[1].x, optionPoses[1].y,
				selected: selectArrowPosY == 1
			);
		}
		Fonts.drawTextEX(
			FontType.Grey, "Left/Right: Change, [OK]: Add, [BACK]: Back",
			Global.screenW * 0.5f, 200, Alignment.Center
		);
	}

	public bool teamOptionEnabled() {
		if (!Global.level.gameMode.isTeamMode) return false;
		if (Global.serverClient == null) return true;
		if (Global.level?.server != null && Global.level.server.hidden) return true;
		return false;
	}
}
