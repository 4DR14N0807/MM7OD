using System;
using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;

public class DevConsole {
	public static bool showConsole = false;
	private static bool showLogOnly = false;
	public static List<string> consoleLog = new List<string>();
	public static string log(string message, bool showConsole = false) {
		if (consoleLog.Count > 16) {
			consoleLog.RemoveAt(0);
		}
		string prefix = Global.frameCount.ToString() + ": ";
		if (!Global.debug) prefix = "";
		consoleLog.Add(prefix + message);
		if (showConsole) {
			showLogOnly = true;
		}
		return message;
	}

	public static void toggleShow() {
		if (showConsole) {
			hide();
		} else {
			show();
		}
	}

	public static void show() {
		showConsole = true;
		Menu.chatMenu.openChat();
	}

	public static void hide() {
		showConsole = false;
		showLogOnly = false;
		Menu.chatMenu.closeChat();
	}

	public static string toggleShowLogOnly() {
		hide();
		showLogOnly = true;
		consoleLog.Clear();
		return "";
	}

	public static void drawConsole() {
		if (showConsole || showLogOnly) {
			int posX = MathInt.Round(Global.level.camX);
			int posY = MathInt.Round(Global.level.camY);
			DrawWrappers.DrawTexture(
				Global.textures["pausemenu"],
				0, 0, 384, 216, posX, posY, ZIndex.HUD + 2000000
			);
			for (int i = 0; i < consoleLog.Count; i++) {
				string line = consoleLog[i];
				Fonts.drawText(FontType.White, line, 20, 20 + (i * 10));
			}
		}
	}

	public static string setMatchTime(string[] args) {
		float time = 0;
		if (args.Length == 0) {
			time = 5;
		} else {
			if (!float.TryParse(args[0], out float par)) {
				return "Error: Error parsing arg 1.";
			}
			time = par;
		}
		return $"Match time set to {time}s";
	}


	public static string aiSwitch2(AITrainingBehavior aib) {
		AI.trainingBehavior = aib;
		return $"AI behaviour changed to {aib}";
	}

	public static string aiSwitch(string[] args) {
		if (args.Length == 0 || !int.TryParse(args[0], out _)) {
			return "Use: /aiswitch [#slot] [a]";
		}
		string log = "";
		int slot = int.Parse(args[0]);
		Global.level.otherPlayer.changeWeaponSlot(slot - 1);
		if (args.Contains("a")) {
			AI.trainingBehavior = AITrainingBehavior.Attack;
			log += $"AI behaviour changed to {AI.trainingBehavior}.\n";
		}
		log += $"Changed AI slot to {slot}";
		return log;
	}

	public static string currencyCommand(string[] args) {
		if (args.Length == 0) {
			return "Error: Needs 1 argument.";
		}
		if (args[0] == "max") {
			args[0] = "9999";
			return $"{Global.nameCoins} set to max.";
		}
		int currency = int.Parse(args[0]);
		Global.level.mainPlayer.currency = currency;

		return $"{Global.nameCoins} set to {args[0]}.";
	}

	public static string setHealth(string[] args) {
		if (args.Length == 0) {
			return "Error: Needs 1 argument.";
		}
		if (Global.level.mainPlayer.character?.currentMaverick != null) {
			Global.level.mainPlayer.character.currentMaverick.health = int.Parse(args[0]);
			return $"Puppet HP set to {args[0]}";
		}
		Global.level.mainPlayer.health = int.Parse(args[0]);
		return $"HP set to {args[0]}";
	}

	public static string selfDMG(string[] args) {
		if (args.Length == 0) {
			return "Error: Needs 1 argument.";
		}
		Global.level.mainPlayer?.character?.applyDamage
		(float.Parse(args[0]), Global.level.mainPlayer, null, null, (int)ProjIds.SelfDmg);

		return $"Applied {args[0]} damage.";
	}

	public static string setMusicNearEnd() {
		Global.music?.setNearEndCheat();
		return "";
	}

	public static string printChecksum() {
		if (Global.level?.levelData?.isCustomMap == true) {
			return log(Global.level.levelData.checksum);
		}
		return "Error: Current map is not a custom one.";
	}

	public static string addDnaCore(string[] args) {
		int count = 10;
		if (args.Length > 0) {
			count = int.Parse(args[0]);
		}
		for (int i = 0; i < count; i++) {
			Character? chr = Global.level.players.FirstOrDefault(p => p != Global.level.mainPlayer)?.character;
			if (chr != null) {
				Global.level.mainPlayer.weapons.Add(new DNACore(chr, Global.level.mainPlayer));
			}
		}
		return "";
	}

	public static string showOrHideHitboxes(string[] args) {
		Global.showHitboxes = !Global.showHitboxes;
		return "";
	}

	public static string showOrHideGrid(string[] args) {
		Global.showGridHitboxes = !Global.showGridHitboxes;
		return "";
	}

	public static string showOrHideTGrid(string[] args) {
		Global.showTerrainGridHitboxes = !Global.showTerrainGridHitboxes;
		return "";
	}

	public static string printClientPort(string[] args) {
		if (Global.serverClient != null) {
			return log(Global.serverClient.client.Port.ToString());
		}
		return log("No server client detected");
	}

	public static string printServerPort(string[] args) {
		if (Global.localServer != null) {
			return log(Global.localServer.s_server.Port.ToString());
		}
		return log("No server host detected");
	}

	public static string printRadminIP(string[] args) {
		if (Global.localServer != null && Global.radminIP != "") {
			return log(Global.radminIP + ":" + Global.localServer.s_server.Port);
		}
		return log("No server host detected");
	}

	public static string becomeMoth() {
		var mmc = Global.level?.mainPlayer?.currentMaverick as MorphMothCocoon;
		if (mmc != null) {
			mmc.selfDestructTime = Global.spf;
		}
		return "";
	}

	public static void win() {
		if (Global.level.gameMode is FFADeathMatch) {
			Global.level.mainPlayer.kills = Global.level.gameMode.playingTo;
		} else if (Global.level.gameMode is TeamDeathMatch) {
			Global.level.gameMode.teamPoints[0] = (byte)Global.level.gameMode.playingTo;
		}
	}

	public static void lose() {
		if (Global.level.gameMode is FFADeathMatch) {
			Global.level.otherPlayer.kills = Global.level.gameMode.playingTo;
		} else if (Global.level.gameMode is TeamDeathMatch) {
			Global.level.gameMode.teamPoints[1] = (byte)Global.level.gameMode.playingTo;
		}
	}

	public static string aiRevive() {
		if (Global.debug) {
			Global.shouldAiAutoRevive = true;
			Global.level.otherPlayer.character?.applyDamage(
				Damager.ohkoDamage, Global.level.otherPlayer, Global.level.otherPlayer.character, null, null
			);
			return "";
		}
		return "Failed, debug mode is not active.";
	}

	public static string aiMash(string[] args) {
		int mashType = 0;
		if (args.Length > 0) {
			mashType = int.Parse(args[0]);
		}
		mashType = Helpers.clamp(mashType, 0, 2);
		Global.level.otherPlayer.character.ai.mashType = mashType;

		return $"Mash type set to {mashType}";
	}

	public static void spawnRideChaser() {
		var mp = Global.level.mainPlayer;
		if (mp != null) new RideChaser(mp, mp.character.pos, 0, null, true);
	}

	public static void toggleFTD() {
		if (Global.level.server.netcodeModel == NetcodeModel.FavorAttacker) {
			Global.level.server.netcodeModel = NetcodeModel.FavorDefender;
		} else {
			Global.level.server.netcodeModel = NetcodeModel.FavorAttacker;
		}
	}

	public static void toggleInvulnFrames(int time) {
		var mc = Global.level.mainPlayer.character;
		mc.invulnTime = time;
	}

	public static void changeTeam() {
		if (!Global.level.gameMode.isTeamMode) return;

		int team = Global.level.mainPlayer.alliance == GameMode.redAlliance ? GameMode.blueAlliance : GameMode.redAlliance;
		Global.serverClient?.rpc(RPC.switchTeam, RPCSwitchTeam.getSendMessage(Global.level.mainPlayer.id, team));
		Global.level.mainPlayer.newAlliance = team;
		Global.level.mainPlayer.forceKill();
		Menu.exit();
	}

	public static string aiDebug(bool changeToSpec) {
		Global.showAIDebug = !Global.showAIDebug;
		if (changeToSpec) {
			Global.level.setMainPlayerSpectate();
		}
		return "";
	}

	public static string aiGiga() {
		if (Global.level.otherPlayer.character is MegamanX) {
			Global.level.otherPlayer.weapons.Add(new GigaCrush());
			Global.level.otherPlayer.character.changeState(new GigaCrushCharState(), true);
			return "";
		}
		return "Error: Other player is not X.";
	}

	public static List<Command> commands = new List<Command>() {
		// Offline only, undocumented
		new Command("log", (args) => toggleShowLogOnly(), false),
		new Command("moth", (args) => becomeMoth()),
		new Command("airevive", (args) => aiRevive()),
		new Command("aigiga", (args) => aiGiga()),
		//new Command("rc", (args) => spawnRideChaser()),
		new Command("aidebug", (args) => aiDebug(false)),
		new Command("aispec", (args) => aiDebug(true)),
		// Offline only
		new Command("hitbox", (args) => showOrHideHitboxes(args), false),
		new Command("clientport", (args) => printClientPort(args), false),
		new Command("serverport", (args) => printServerPort(args), false),
		new Command("radminip", (args) => printRadminIP(args), false),
		new Command("grid", (args) => showOrHideGrid(args), false),
		new Command("tgrid", (args) => showOrHideTGrid(args), false),
		new Command("dumpnetids", (args) => {
			Helpers.WriteToFile("netIdDump.txt", Global.level.getNetIdDump());
			return "";
		}),
		new Command(
			"dumpkillfeed",
			(args) => {
				Helpers.WriteToFile(
					"killFeedDump.txt",
					string.Join(Environment.NewLine, Global.level.gameMode.killFeedHistory)
				);
				return "";
			}, false
		),
		new Command("invuln", (args) => {
			Global.level.mainPlayer.character.invulnTime = 60;
			return "";
		}),
		new Command("ult", (args) => {
			if (Global.level.mainPlayer.character is MegamanX mmx) {
				mmx.hasUltimateArmor = true;
				return "Error: Main player is not X.";
			}
			return "";
		}),
		new Command("hp", (args) => setHealth(args)),
		new Command("dmg", (args) => selfDMG(args)),
		new Command("freeze", (args) => {
			Global.level.mainPlayer.character?.freeze(1, 0, 255);
			return "";
		}),
		new Command("hurt", (args) => {
			Global.level.mainPlayer.character?.setHurt(-1, Global.defFlinch, false);
			return "";
		}),
		new Command("trhealth", (args) => {
			Global.spawnTrainingHealth = !Global.spawnTrainingHealth;
			return "";
		}),
		new Command("checksum", (args) => printChecksum(), false),
		new Command("dna", (args) => addDnaCore(args)),
		new Command("timeleft", (args) => setMatchTime(args)),
		new Command("aiattack", (args) => aiSwitch2(AITrainingBehavior.Attack)),
		new Command("aijump", (args) => aiSwitch2(AITrainingBehavior.Jump)),
		new Command("aiguard", (args) => aiSwitch2(AITrainingBehavior.Guard)),
		new Command("aicrouch", (args) => aiSwitch2(AITrainingBehavior.Crouch)),
		new Command("aistop", (args) => aiSwitch2(AITrainingBehavior.Idle)),
		new Command("aikill", (args) => {
			Global.level.otherPlayer?.forceKill();
			return "";
		}),
		new Command("aiswitch", aiSwitch),
		new Command("aimash", (args) => aiMash(args)),
		new Command("bolt", currencyCommand),
		new Command("die", (args) => {
			Global.level.mainPlayer.forceKill();
			return "";
		}),
		new Command("raflight", (args) => {
			Global.level.rideArmorFlight = !Global.level.rideArmorFlight;
			return "";
		}),
		// Online
		new Command("diagnostics", (args) => {
			Global.showDiagnostics = !Global.showDiagnostics;
			return "";
		}, false),
		new Command("diag", (args) => {
			Global.showDiagnostics = !Global.showDiagnostics;
			return "";
		}, false),
		new Command("clear", (args) => { consoleLog.Clear(); return ""; }, false),
		new Command("musicend", (args) => setMusicNearEnd()),
		// GMTODO: remove
		// Gacel: Not. This could be usefull for bug reports with flags.
		new Command(
			"dumpflagdata",
			(args) => Helpers.WriteToFile("flagDataDump.txt", Global.level.getFlagDataDump()) ?? "",
			offlineOnly: false
		),
		/*
		#if DEBUG
		new Command("autofire", (args) => Global.autoFire = !Global.autoFire),
		new Command("breakpoint", (args) => Global.breakpoint = !Global.breakpoint),
		new Command("r", (args) => (
			Global.level.mainPlayer.kills = Global.level.gameMode.playingTo - 1),
		),
		new Command("1morekill", (args) => Global.level.mainPlayer.kills = Global.level.gameMode.playingTo - 1),
		new Command("win", (args) => win()),
		new Command("lose", (args) => lose()),
		new Command("changeteam", (args) => changeTeam()),
		new Command("ftd", (args) => toggleFTD()),
		new Command("invuln", (args) => toggleInvulnFrames(10)),
		#endif
		*/
	};

	public static string runCommand(string commandStr) {
		List<string> pieces = commandStr.Split(' ').ToList();
		string command = pieces[0];
		var args = new List<string>();
		try {
			args = pieces.GetRange(1, pieces.Count - 1);
		} catch { }

		var commandObj = commands.FirstOrDefault(c => c.name == command.ToLowerInvariant());
		if (commandObj != null) {
			if (!commandObj.offlineOnly || Global.serverClient == null) {
				try {
					string localLog = log("Ran command \"" + command + "\"");
					commandObj.action.Invoke(args.ToArray());
					if (args.Contains("-v")) {
						hide();
					}
					return localLog;
				} catch {
					return log("Command \"" + command + "\" failed");
				}
			} else {
				return log("Command \"" + command + "\" is only available offline");
			}
		}
		return log("Command \"" + command + "\" does not exist.");
	}
}

public class Command {
	public string name;
	public bool offlineOnly;
	public Func<string[], string> action;

	public Command(string name, Func<string[], string> action, bool offlineOnly = true) {
		this.name = name;
		this.offlineOnly = offlineOnly;
		this.action = action;
	}
}
