﻿using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SFML.Graphics;
using SFML.System;

namespace MMXOnline;

public class GameMode {
	public const string Deathmatch = "deathmatch";
	public const string TeamDeathmatch = "team deathmatch";
	public const string CTF = "ctf";
	public const string ControlPoint = "control point";
	public const string Elimination = "elimination";
	public const string TeamElimination = "team elimination";
	public const string KingOfTheHill = "king of the hill";
	public const string Race = "race";
	public static List<string> allGameModes = new List<string>() {
		Deathmatch, TeamDeathmatch, CTF, KingOfTheHill,
		ControlPoint, Elimination, TeamElimination
	};

	public const int blueAlliance = 0;
	public const int redAlliance = 1;
	public const int neutralAlliance = 10;

	public bool isTeamMode = false;
	public float overTime = 0;
	public float secondsBeforeLeave = 7;
	public float? setupTime;
	public float? remainingTime;
	public float? startTimeLimit;
	public int playingTo;
	public bool drawingScoreboard;

	public bool noContest;

	public byte[] teamPoints = new byte[6];
	public byte[] teamAltPoints = new byte[6];
	public string[] teamNames = {
		"Blue",
		"Red",
		"Green",
		"Purple",
		"Yellow",
		"Orange"
	};
	public FontType[] teamFonts = {
		FontType.BlueSmall,
		FontType.RedSmall,
		FontType.GreenSmall,
		FontType.PurpleSmall,
		FontType.YellowSmall,
		FontType.OrangeSmall
	};

	public VoteKick? currentVoteKick;
	public float voteKickCooldown;

	public string dpsString = "";
	public Level level;
	public float eliminationTime;
	public float localElimTimeInc;  // Used to "client side predict" the elimination time increase.
	public byte virusStarted;
	public byte safeZoneSpawnIndex;

	bool loggedStatsOnce;
	float goTime;

	public Player mainPlayer { get { return level.mainPlayer; } }

	public RPCMatchOverResponse? matchOverResponse;
	public bool isOver { get { return matchOverResponse != null; } }

	public int lastTimeInt;
	public int lastSetupTimeInt;
	public float periodicHostSyncTime;
	public float syncValueTime;

	bool changedEndMenuOnce;
	bool changedEndMenuOnceHost;

	public ChatMenu chatMenu;

	public List<KillFeedEntry> killFeed = new List<KillFeedEntry>();
	public List<string> killFeedHistory = new List<string>();

	bool removedGates;
	public HostMenu? nextMatchHostMenu;

	float flashTime;
	float flashCooldown;

	public float hudErrorMsgTime;
	string hudErrorMsg = "";

	public Player? hudTopLeftPlayer;
	public Player? hudTopRightPlayer;
	public Player? hudLeftPlayer;
	public Player? hudRightPlayer;
	public Player? hudBotLeftPlayer;
	public Player? hudBotRightPlayer;

	bool hudPositionsAssigned;
	int currentLineH;

	List<(Point pos, string name)> navPoints = new();

	public enum HUDHealthPosition {
		Left,
		Right,
	}

	public Point safeZonePoint {
		get {
			return level.spawnPoints[safeZoneSpawnIndex].pos;
		}
	}
	public Rect safeZoneRect {
		get {
			if (virusStarted == 0) {
				return new Rect(0, 0, level.width, level.height);
			} else if (virusStarted == 1) {
				float t = eliminationTime - (startTimeLimit ?? eliminationTime);
				if (t < 0) t = 0;
				float timePct = t / 60;
				return new Rect(
					timePct * (safeZonePoint.x - 150),
					timePct * (safeZonePoint.y - 112),
					level.width - (timePct * (level.width - (safeZonePoint.x + 150))),
					level.height - (timePct * (level.height - (safeZonePoint.y + 112)))
				);
			} else if (virusStarted == 2) {
				float t = eliminationTime - (startTimeLimit ?? eliminationTime) - 60;
				if (t < 0) t = 0;
				float timePct = t / 300;
				return new Rect(
					(safeZonePoint.x - 150) + (timePct * 150),
					(safeZonePoint.y - 112) + (timePct * 112),
					(safeZonePoint.x + 150) - (timePct * 150),
					(safeZonePoint.y + 112) - (timePct * 112)
				);
			} else {
				return new Rect(safeZonePoint.x, safeZonePoint.y, safeZonePoint.x, safeZonePoint.y);
			}
		}
	}

	public static bool isStringTeamMode(string selectedGameMode) {
		if (selectedGameMode == CTF ||
			selectedGameMode == TeamDeathmatch ||
			selectedGameMode == ControlPoint ||
			selectedGameMode == TeamElimination ||
			selectedGameMode == KingOfTheHill ||
			selectedGameMode.StartsWith("tm_")
		) {
			return true;
		}
		return false;
	}

	public static string abbreviatedMode(string mode) {
		if (mode == TeamDeathmatch) return "tdm";
		else if (mode == CTF) return "ctf";
		else if (mode == ControlPoint) return "cp";
		else if (mode == Elimination) return "elim";
		else if (mode == TeamElimination) return "t.elim";
		else if (mode == KingOfTheHill) return "koth";
		else if (mode == Race) return "race";
		else return "dm";
	}

	public bool useTeamSpawns() {
		return (this is CTF) || (this is ControlPoints) || (this is KingOfTheHill);
	}

	public float getAmmoModifier() {
		/*
		if (level.is1v1())
		{
			if (Global.level.server.playTo == 1) return 0.25f;
			if (Global.level.server.playTo == 2) return 0.5f;
		}
		return 1;
		*/
		return 1;
	}

	public static int[] getAllianceCounts(List<Player> players, int teamNum) {
		int[] teamSizes = new int[teamNum];
		foreach (Player player in players) {
			if (!player.isSpectator && player.alliance >= 0 && player.alliance < teamNum) {
				teamSizes[player.alliance]++;
			}
		}
		return teamSizes;
	}

	public static int[] getAllianceCounts(List<ServerPlayer> players, int teamNum) {
		int[] teamSizes = new int[teamNum];
		foreach (ServerPlayer serverPlayer in players) {
			if (!serverPlayer.isSpectator && serverPlayer.alliance >= 0 && serverPlayer.alliance < teamNum) {
				teamSizes[serverPlayer.alliance]++;
			}
		}
		return teamSizes;
	}

	public GameMode(Level level, int? timeLimit) {
		this.level = level;
		if (timeLimit != null) {
			remainingTime = timeLimit.Value * 60;
			startTimeLimit = remainingTime;
		}
		chatMenu = new ChatMenu();
	}

	static List<ChatEntry> getTestChatHistory() {
		var test = new List<ChatEntry>();
		for (int i = 0; i < 30; i++) {
			test.Add(new ChatEntry("chat entry " + i.ToString(), "gm19", null, false));
		}
		return test;
	}

	public void removeAllGates() {
		if (!removedGates) removedGates = true;
		else return;

		for (int i = Global.level.gates.Count - 1; i >= 0; i--) {
			Global.level.removeGameObject(Global.level.gates[i]);
			Global.level.gates.RemoveAt(i);
		}
		if (Global.level.isRace()) {
			foreach (var player in Global.level.players) {
				if (player.character != null && player.character.ownedByLocalPlayer) {
					player.character.invulnTime = 1;
				}
			}
		}
	}

	public virtual void update() {
		Helpers.decrementTime(ref hudErrorMsgTime);

		if (Global.isHost) {
			if (level.isNon1v1Elimination() && remainingTime != null && remainingTime.Value <= 0) {
				if (virusStarted < 3) {
					virusStarted++;
					if (virusStarted == 1) remainingTime = 60;
					else if (virusStarted == 2) remainingTime = 300;
				}
			}
		} else {
			if (level.isNon1v1Elimination()) {
				if (localElimTimeInc < 1) {
					eliminationTime += Global.spf;
					localElimTimeInc += Global.spf;
				}

				float phase1Time = (startTimeLimit ?? 0);
				float phase2Time = (startTimeLimit ?? 0) + 60;

				if (eliminationTime <= phase1Time) virusStarted = 0;
				else if (eliminationTime >= phase1Time && eliminationTime < phase2Time) virusStarted = 1;
				else if (eliminationTime >= phase2Time) virusStarted = 2;
			}
		}

		if (currentVoteKick != null) {
			currentVoteKick.update();
		}
		if (voteKickCooldown > 0) {
			voteKickCooldown -= Global.spf;
			if (voteKickCooldown < 0) voteKickCooldown = 0;
		}

		if (level.mainPlayer.isSpectator && !Menu.inMenu) {
			if (Global.input.isPressedMenu(Control.Left)) {
				level.specPlayer = level.getNextSpecPlayer(-1);
			} else if (Global.input.isPressedMenu(Control.Right)) {
				level.specPlayer = level.getNextSpecPlayer(1);
			}
		}

		for (var i = this.killFeed.Count - 1; i >= 0; i--) {
			var killFeed = this.killFeed[i];
			killFeed.time += 1;
			if (killFeed.time > 60 * 3) {
				this.killFeed.Remove(killFeed);
			}
		}

		checkIfWin();

		if (Global.isHost && Global.serverClient != null) {
			periodicHostSyncTime += Global.spf;
			if (periodicHostSyncTime >= 0.5f) {
				periodicHostSyncTime = 0;
				RPC.periodicHostSync.sendRpc();
			}

			if (Global.level.movingPlatforms.Count > 0) {
				syncValueTime += Global.spf;
				if (syncValueTime > 0.06f) {
					syncValueTime = 0;
					RPC.syncValue.sendRpc(Global.level.syncValue);
				}
			}
		}

		if ((Global.level.mainPlayer.isAxl || Global.level.mainPlayer.isDisguisedAxl) && Options.main.useMouseAim && overTime < secondsBeforeLeave && !Menu.inMenu && !Global.level.mainPlayer.isSpectator) {
			Global.window.SetMouseCursorVisible(false);
			Global.window.SetMouseCursorGrabbed(true);
			Global.isMouseLocked = true;
		} else {
			Global.window.SetMouseCursorVisible(true);
			Global.window.SetMouseCursorGrabbed(false);
			Global.isMouseLocked = false;
		}

		if (!isOver) {
			if (setupTime == 0 && Global.isHost) {
				// Just in case packets were dropped, keep syncing "0" time
				if (Global.frameCount % 30 == 0) {
					Global.serverClient?.rpc(RPC.syncSetupTime, 0, 0);
				}
			}

			if (setupTime > 0 && Global.isHost) {
				int time = MathInt.Round(setupTime.Value);
				byte[] timeBytes = BitConverter.GetBytes((ushort)time);
				if (setupTime > 0) {
					setupTime -= Global.spf;
					if (setupTime <= 0) {
						setupTime = 0;
						removeAllGates();
					}
				}
				if (setupTime.Value < lastSetupTimeInt) {
					Global.serverClient?.rpc(RPC.syncSetupTime, timeBytes);
				}
				lastSetupTimeInt = MathInt.Floor(setupTime.Value);
			} else if (remainingTime != null && Global.isHost) {
				int time = MathInt.Round(remainingTime.Value);
				byte[] timeBytes = BitConverter.GetBytes((ushort)time);
				int elimTime = MathInt.Round(eliminationTime);
				byte[] elimTimeBytes = BitConverter.GetBytes((ushort)elimTime);

				if (remainingTime > 0) {
					remainingTime -= Global.spf;
					eliminationTime += Global.spf;
					if (remainingTime <= 0) {
						remainingTime = 0;
						if (elimTime > 0) Global.serverClient?.rpc(RPC.syncGameTime, 0, 0, elimTimeBytes[0], elimTimeBytes[1]);
						else Global.serverClient?.rpc(RPC.syncGameTime, 0, 0);
					}
				}

				if (remainingTime.Value < lastTimeInt) {
					if (remainingTime.Value <= 10) Global.playSound("text");
					if (elimTime > 0) Global.serverClient?.rpc(RPC.syncGameTime, timeBytes[0], timeBytes[1], elimTimeBytes[0], elimTimeBytes[1]);
					else Global.serverClient?.rpc(RPC.syncGameTime, timeBytes[0], timeBytes[1]);
				}

				lastTimeInt = MathInt.Floor(remainingTime.Value);
			} else if (level.isNon1v1Elimination() && !Global.isHost) {
				remainingTime -= Global.spf;
			}
		}

		bool isWarpIn = level.mainPlayer.character != null && level.mainPlayer.character.isWarpIn();

		Helpers.decrementTime(ref UpgradeMenu.eTankDelay);
		Helpers.decrementTime(ref UpgradeMenu.wTankDelay);
		Helpers.decrementFrames(ref BluesUpgradeMenu.lTankDelay);

		if (!isOver) {
			if (!Menu.inMenu && ((level.mainPlayer.warpedIn && !isWarpIn) || Global.level.mainPlayer.isSpectator) && Global.input.isPressedMenu(Control.MenuPause) && !chatMenu.recentlyExited) {
				if (mainPlayer.character is Axl axl) {
					axl.resetToggle();
				}
				Menu.change(new InGameMainMenu());
			} else if (Menu.inMenu && Global.input.isPressedMenu(Control.MenuPause) && !isBindingControl()) {
				Menu.exit();
			}
		} else if (Global.serverClient != null) {
			if (!Global.isHost && !level.is1v1()) {
				if (!Menu.inMenu && Global.input.isPressedMenu(Control.MenuPause) && !chatMenu.recentlyExited) {
					if (mainPlayer.character is Axl axl) {
						axl.resetToggle();
					}
					Menu.change(new InGameMainMenu());
				} else if (Menu.inMenu && Global.input.isPressedMenu(Control.MenuPause) && !isBindingControl()) {
					Menu.exit();
				}
			}

			if (overTime <= secondsBeforeLeave) {

			} else {
				if (Global.isHost) {
					if ((Menu.mainMenu is HostMenu || Menu.mainMenu is SelectCharacterMenu) && Global.input.isPressedMenu(Control.MenuPause) && !chatMenu.recentlyExited) {
						if (mainPlayer.character is Axl axl) {
							axl.resetToggle();
						}
						Menu.change(new InGameMainMenu());
					} else if (Menu.inMenu && Global.input.isPressedMenu(Control.MenuPause) && !chatMenu.recentlyExited) {
						if (nextMatchHostMenu != null) Menu.change(nextMatchHostMenu);
					}
					if (!Menu.inMenu) {
						if (nextMatchHostMenu != null) Menu.change(nextMatchHostMenu);
					}
				} else {
					if (!Menu.inMenu && level.is1v1()) {
						Menu.change(
							new SelectCharacterMenu(
								null, level.is1v1(), false, true, true,
								level.gameMode.isTeamMode, Global.isHost, () => { })
							);
					}
				}
			}
		}
	}

	private bool isBindingControl() {
		if (Menu.mainMenu is ControlMenu cm) {
			return cm.isBindingControl();
		}
		return false;
	}

	public void checkIfWin() {
		if (!isOver) {
			if (Global.isHost) {
				checkIfWinLogic();

				if (noContest) {
					matchOverResponse = new RPCMatchOverResponse() {
						winningAlliances = new HashSet<int>() { },
						winMessage = "No contest!",
						loseMessage = "No contest!",
						loseMessage2 = "Host ended match."
					};
				}

				if (isOver) {
					onMatchOver();
					Global.serverClient?.rpc(RPC.matchOver, JsonConvert.SerializeObject(matchOverResponse));
				}
			}
		} else {
			overTime += Global.spf;
			if (overTime > secondsBeforeLeave) {
				if (Global.serverClient != null) {
					if (Global.isHost) {
						if (!changedEndMenuOnceHost) {
							changedEndMenuOnceHost = true;
							nextMatchHostMenu = new HostMenu(null, level.server, false, level.server.isLAN);
							Menu.change(nextMatchHostMenu);
						}
					} else {
						if (!changedEndMenuOnce) {
							changedEndMenuOnce = true;
							if (level.is1v1()) {
								Menu.change(new SelectCharacterMenu(null, level.is1v1(), false, true, true, level.gameMode.isTeamMode, Global.isHost, () => { }));
							}
						}
					}
				} else {
					if (Global.input.isPressedMenu(Control.MenuPause)) {
						Global.leaveMatchSignal = new LeaveMatchSignal(LeaveMatchScenario.MatchOver, null, null);
					}
				}
			}
		}
	}

	public virtual void checkIfWinLogic() {
	}

	public void checkIfWinLogicTeams() {
		int winningAlliance = -1;
		for (int i = 0; i < Global.level.teamNum; i++) {
			if (Global.level.gameMode.teamPoints[i] >= playingTo) {
				if (winningAlliance == -1) {
					winningAlliance = i;
				} else {
					winningAlliance = -3;
				}
			}
		}
		if (winningAlliance == -1 && remainingTime <= 0) {
			int lastScore = 0;
			bool closeMatch = false;
			for (int i = 0; i < Global.level.teamNum; i++) {
				if (Global.level.gameMode.teamPoints[i] > lastScore) {
					winningAlliance = i;
					closeMatch = false;
					if (Global.level.gameMode.teamPoints[i] - 1 == lastScore) {
						closeMatch = true;
					}
				} else if (Global.level.gameMode.teamPoints[i] == lastScore) {
					winningAlliance = -2;
					closeMatch = true;
				}
			}
			if (this is CTF && closeMatch) {
				if (level.redFlag.pickedUpOnce) {
					return;
				}
				if (level.blueFlag.pickedUpOnce) {
					return;
				}
			}
		}
		if (winningAlliance == -3) {
			matchOverResponse = new RPCMatchOverResponse() {
				winningAlliances = new HashSet<int>() { },
				winMessage = "Draw!",
				loseMessage = "Draw!"
			};
		} else if (winningAlliance == -2) {
			matchOverResponse = new RPCMatchOverResponse() {
				winningAlliances = new HashSet<int>() { },
				winMessage = "Stalemate!",
				loseMessage = "Stalemate!"
			};
		} else if (winningAlliance >= 0) {
			matchOverResponse = new RPCMatchOverResponse() {
				winningAlliances = new HashSet<int>() { winningAlliance },
				winMessage = "Victory!",
				winMessage2 = $"{teamNames[winningAlliance]} team wins",
				loseMessage = "You lost!",
				loseMessage2 = $"{teamNames[winningAlliance]} team wins"
			};
		}
	}

	public virtual void render() {
		if (level.mainPlayer == null) return;
		if (DevConsole.showConsole) {
			return;
		}

		Player? drawPlayer = null;
		if (!Global.level.mainPlayer.isSpectator) {
			drawPlayer = Global.level.mainPlayer;
		} else {
			drawPlayer = level.specPlayer;
		}
		if (drawPlayer != null) {
			if (Global.level.mainPlayer == drawPlayer) {
				renderHealthAndWeapons();
			} else {
				renderHealthAndWeapon(drawPlayer, HUDHealthPosition.Left);
			}
			// Currency
			if (!Global.level.is1v1()) {
				Point basePos = new(Global.screenW - 96, 27);
				if (level.levelData.isTraining()) {
					basePos = new Point(10, 106);
					if (Global.level.mainPlayer.lastCharacter is Blues) {
						basePos.y += 18;
					}
				}
				Fonts.drawText(
					FontType.WhiteSmall,
					"x", basePos.x + 9, basePos.y, Alignment.Left
				);
				Fonts.drawText(
					FontType.WhiteSmall,
					" " + drawPlayer.currency.ToString(), basePos.x + 40, basePos.y, Alignment.Right
				);
				Global.sprites["pickup_bolt_small"].drawToHUD(0, basePos.x + 4, basePos.y + 4);
			}
			if (drawPlayer.character is Rock rock && rock.boughtSuperAdaptorOnce) {
				drawGigaWeaponCooldown((int)RockWeaponSlotIds.ArrowSlash, rock.arrowSlashCooldown / 90, y: 125);
				drawGigaWeaponCooldown((int)RockWeaponSlotIds.LegBreaker, rock.legBreakerCooldown / 90, 35, 125);
			}
			if (drawPlayer.weapons == null) {
				return;
			}
			if (drawPlayer.weapons!.Count > 1) {
				drawWeaponSwitchHUD(drawPlayer);
			}
		}
		if (shouldDrawRadar() && !Menu.inMenu) {
			drawRadar();
		}
		navPoints.Clear();
		if (!Global.level.is1v1()) {
			drawKillFeed();
		}
		if (Global.level.isTraining()) {
			drawDpsIfSet(5);
		} else {
			drawTopHUD();
		}
		if (isOver) {
			drawWinScreen();
		} else {
			drawRespawnHUD();
		}

		drawingScoreboard = false;
		if (!Menu.inControlMenu && level.mainPlayer.input.isHeldMenu(Control.Scoreboard)) {
			drawingScoreboard = true;
			drawScoreboard();
		}
		if (level.isAfkWarn()) {
			Fonts.drawText(
				FontType.RedishOrange, "Warning: Time before AFK Kick: " + Global.level.afkWarnTimeAmount(),
				Global.halfScreenW, 50, Alignment.Center
			);
		} else if (Global.serverClient != null && Global.serverClient.isLagging() && hudErrorMsgTime == 0) {
			Fonts.drawText(
				FontType.WhiteSmall, Helpers.controlText("Veneco Detectado."),
				Global.halfScreenW, 50, Alignment.Center
			);
		} else if (hudErrorMsgTime > 0) {
			Fonts.drawText(
				FontType.BlueMenu, hudErrorMsg,
				Global.halfScreenW, 50, Alignment.Center
			);
		}
		if (currentVoteKick != null) {
			currentVoteKick.render();
		} else if (level.mainPlayer.isSpectator && !Menu.inMenu) {
			if (level.specPlayer == null) {
				Fonts.drawText(
					FontType.BlueMenu, "Now spectating: (No player to spectate)",
					 Global.halfScreenW, 190, Alignment.Center
				);
			} else {
				string deadMsg = level.specPlayer.character == null ? " (Dead)" : "";
				Fonts.drawText(
					FontType.BlueMenu, "Now spectating: " + level.specPlayer.name + deadMsg,
					Global.halfScreenW, 180, Alignment.Center
				);
			}
		} else if (level.mainPlayer.aiTakeover) {
			Fonts.drawText(
				FontType.OrangeMenu, "AI Takeover active. Press F12 to stop.",
				Global.halfScreenW, 180, Alignment.Center
			);
		}
		drawDiagnostics();

		if (level.isNon1v1Elimination() && virusStarted > 0) {
			drawObjectiveNavpoint("Safe Zone", safeZonePoint);
		}

		if (level.mainPlayer.isX && level.mainPlayer.character?.charState is XReviveStart xrs) {
			Character chr = level.mainPlayer.character;

			float boxHeight = xrs.boxHeight;
			float boxEndY = Global.screenH - 5;
			float boxStartY = boxEndY - boxHeight;

			if (chr.pos.y - level.camCenterY > 0) {
				boxStartY = 5;
				boxEndY = 5 + boxHeight;
				boxStartY += xrs.boxOffset;
				boxEndY += xrs.boxOffset;
			} else {
				boxStartY -= xrs.boxOffset;
				boxEndY -= xrs.boxOffset;
			}

			DrawWrappers.DrawRect(5, boxStartY, Global.screenW - 5, boxEndY, true, new Color(0, 0, 0, 224), 0, ZIndex.HUD, false);

			Fonts.drawText(
				FontType.Blue, xrs.dialogLine1, 55, boxStartY + boxHeight * 0.33f
			);
			Fonts.drawText(
				FontType.Blue, xrs.dialogLine2, 55, boxStartY + boxHeight * 0.55f
			);

			if (xrs.dialogLine1.Length > 0) {
				int index = 0;
				if (xrs.state == 1 || xrs.state == 3) {
					index = Global.isOnFrameCycle(15) ? 1 : 0;
				}
				Global.sprites["drlight_portrait"].drawToHUD(index, 15, boxStartY + boxHeight * 0.5f);
			}
		}
	}

	public void setHUDErrorMessage(Player player, string message, bool playSound = true, bool resetCooldown = false) {
		if (player != level.mainPlayer) return;
		if (resetCooldown) hudErrorMsgTime = 0;
		if (hudErrorMsgTime == 0) {
			hudErrorMsg = message;
			hudErrorMsgTime = 2;
			if (playSound) {
				Global.playSound("error");
			}
		}
	}

	public bool shouldDrawRadar() {
		return true;
	}

	void drawRadar() {
		if (Global.level.is1v1() || Global.level.isTraining() || Global.level.mainPlayer.isSpectator) {
			return;
		}
		Global.radarRenderTexture.Clear(new Color(0, 0, 0, 0));
		Global.radarRenderTextureB.Clear();
		RenderStates states = new RenderStates(Global.radarRenderTexture.Texture);
		RenderStates statesB = new RenderStates(Global.radarRenderTextureB.Texture);
		RenderStates statesB2 = new RenderStates(Global.radarRenderTextureB.Texture);
		states.BlendMode = new BlendMode(
			BlendMode.Factor.SrcAlpha,
			BlendMode.Factor.OneMinusSrcAlpha, BlendMode.Equation.Add
		) {
			AlphaEquation = BlendMode.Equation.Max
		};
		statesB.BlendMode = new BlendMode(
			BlendMode.Factor.SrcAlpha, BlendMode.Factor.OneMinusSrcAlpha, BlendMode.Equation.Add
		) {
			AlphaEquation = BlendMode.Equation.Max
		};
		statesB2.BlendMode = new BlendMode(
			BlendMode.Factor.SrcAlpha, BlendMode.Factor.OneMinusSrcAlpha, BlendMode.Equation.Min
		);

		float mapScale = 16;
		float offsetX = MathF.Round(Global.level.camCenterX / 16f) - 21;
		float offsetY = MathF.Round(Global.level.camCenterY / 16f) - 12;
		float camX = Global.level.camCenterX;
		float camY = Global.level.camCenterY;
		if (Global.level.mainPlayer.character != null) {
			if (MathF.Abs(Global.level.camCenterX - Global.level.mainPlayer.character.pos.x) < 16) {
				offsetX = MathF.Round(Global.level.mainPlayer.character.pos.x / 16f) - 21;
				camX = Global.level.mainPlayer.character.pos.x;
			}
		}

		List<(float x, float y, float r)> revealedSpots = new();
		revealedSpots.Add((camX, camY, 16 * 10));

		if (isTeamMode) {
			Player[] allyPlayersAlive = level.players.Where(
				p => !p.isSpectator && p.deaths < playingTo && p.alliance == Global.level.mainPlayer.alliance
			).ToArray();
			foreach (Player player in allyPlayersAlive) {
				if (player.character == null) {
					continue;
				}
				revealedSpots.Add((
					player.character.pos.x,
					player.character.pos.y,
					16 * 6)
				);
			}
		}

		float scaledW = 42;
		float scaledH = 24;
		float scaledMapW = MathF.Round(Global.level.levelData.width / 16f);
		float scaledMapH = MathF.Round(Global.level.levelData.height / 16f);

		float radarX = MathF.Floor(Global.screenW - 10 - scaledW);
		float radarY = MathF.Floor(10);

		// The "fog of war" rect
		RectangleShape rect = new RectangleShape(new Vector2f(scaledW + 20, scaledH + 20));
		rect.Position = new Vector2f(0, 0);
		rect.FillColor = new Color(0, 0, 0, 128);
		Global.radarRenderTextureB.Draw(rect, statesB2);

		// The visible area circles
		foreach (var spot in revealedSpots) {
			float pxPos = MathF.Round(spot.x / mapScale) - offsetX;
			float pyPos = MathF.Round(spot.y / mapScale) - offsetY;
			float radius = spot.r / mapScale;
			CircleShape circle1 = new CircleShape(radius);
			circle1.FillColor = new Color(0, 0, 0, 0);
			circle1.Position = new Vector2f(pxPos - radius, pyPos - radius);
			Global.radarRenderTextureB.Draw(circle1, statesB2);
		}

		var sprite = new SFML.Graphics.Sprite(Global.radarRenderTextureB.Texture);
		Global.radarRenderTextureB.Display();
		Global.radarRenderTextureC.Clear(new Color(33, 33, 74));
		Global.radarRenderTextureC.Display();
		Global.radarRenderTextureC.Draw(sprite);
		var spriteBackground = new SFML.Graphics.Sprite(Global.radarRenderTextureC.Texture);

		foreach (GameObject gameObject in Global.level.gameObjects) {
			if (gameObject is not Geometry geometry) {
				continue;
			}
			Color blockColor = new Color(128, 128, 255);
			if (gameObject is not Wall and not KillZone and not Ladder) {
				continue;
			}
			if (gameObject is KillZone) {
				blockColor = new Color(255, 64, 64);
			}
			if (gameObject is Ladder) {
				blockColor = new Color(255, 200, 0);
			}
			Shape shape = geometry.collider.shape;
			float pxPos = shape.minX / mapScale;
			float pyPos = shape.minY / mapScale + 1;
			float mxPos = shape.maxX / mapScale - pxPos;
			float myPos = shape.maxY / mapScale - pyPos + 1;

			if (mxPos <= 1) {
				mxPos = 1;
			}
			if (mxPos <= 1) {
				mxPos = 1;
			}
			if (pxPos <= 0) {
				pxPos -= 20;
				mxPos += 20;
			}
			if (pyPos <= 1) {
				pyPos -= 20;
				myPos += 20;
			}
			if (pxPos + mxPos >= scaledMapW) {
				mxPos = 1000;
			}
			if (pyPos + myPos >= scaledMapH) {
				myPos = 1000;
			}
			if (pyPos + myPos >= scaledMapH) {
				myPos = 1000;
			}
			pxPos -= offsetX;
			pyPos -= offsetY;

			RectangleShape wRect = new RectangleShape();
			wRect.FillColor = blockColor;
			wRect.Position = new Vector2f(pxPos, pyPos);
			wRect.Size = new Vector2f(mxPos, myPos);
			Global.radarRenderTexture.Draw(wRect);
		}
		Global.radarRenderTexture.Display();
		var sprite2 = new SFML.Graphics.Sprite(Global.radarRenderTexture.Texture);

		Global.radarRenderTextureB.Clear(new Color(0, 0, 0, 0));
		RenderStates statesL = new RenderStates(Global.radarRenderTextureB.Texture);
		ShaderWrapper? outlineShader = Helpers.cloneShaderSafe("map_outline");
		if (outlineShader != null) {
			outlineShader.SetUniform("textureSize", new SFML.Graphics.Glsl.Vec2(42, 26));
			statesL.Shader = outlineShader.getShader();
		}
		Global.radarRenderTextureB.Draw(sprite2, statesL);
		Global.radarRenderTextureB.Display();
		var spriteFG = new SFML.Graphics.Sprite(Global.radarRenderTextureB.Texture);

		Global.radarRenderTexture.Clear();
		Global.radarRenderTexture.Draw(spriteBackground);
		Global.radarRenderTexture.Draw(spriteFG);
		var spriteFinal = new SFML.Graphics.Sprite(Global.radarRenderTexture.Texture);
		spriteFinal.Position = new Vector2f(radarX, radarY);

		Global.window.SetView(DrawWrappers.hudView);
		Global.window.Draw(spriteFinal);
		sprite.Dispose();
		sprite2.Dispose();
		spriteFG.Dispose();
		spriteBackground.Dispose();
		spriteFinal.Dispose();

		
		// Nav points.
		foreach (var navPoint in navPoints) {
			Color color = new Color(255, 255, 255);
			Color outColor = new Color(255, 255, 255, 128);
			float xPos = MathF.Round(navPoint.pos.x / mapScale) - offsetX;
			float yPos = MathF.Round(navPoint.pos.y / mapScale) - offsetY - 1 ;

			if (navPoint.name == "RFlag") {
				color = new Color(255, 64, 64);
				outColor = new Color(255, 64, 64, 128);
			}
			if (navPoint.name == "BFlag") {
				color = new Color(64, 64, 255);
				outColor = new Color(64, 64, 255, 128);
			}
			Line line = new Line(new Point(scaledW / 2f, scaledH / 2f), new Point(xPos, yPos));
			Rect camRect = new Rect(0, 0, scaledW - 1, scaledH);
			List<CollideData> intersectionPoints = camRect.getShape().getLineIntersectCollisions(line);
			if (intersectionPoints.Count > 0 && intersectionPoints[0].hitData?.hitPoint != null) {
				Point intersectPoint = intersectionPoints[0].hitData.hitPoint.GetValueOrDefault();
				xPos = intersectPoint.x;
				yPos = intersectPoint.y;
			}

			float dxPos = radarX + MathF.Round(xPos);
			float dyPos = radarY + MathF.Round(yPos);
			DrawWrappers.DrawRectWH(
				dxPos, dyPos,
				1, 2,
				true, color, 1,
				ZIndex.HUD, false, outColor
			);
		}

		// Players.
		foreach (var player in level.nonSpecPlayers()) {
			if (player.character == null || player.character.destroyed) continue;
			if (player.isMainPlayer && player.isDead) continue;

			float xPos = player.character.pos.x / mapScale;
			float yPos = player.character.pos.y / mapScale;
			float xPosF = player.character.pos.x;
			float yPosF = player.character.pos.y;

			Color color;
			if (!isTeamMode) {
				if (player.isMainPlayer) {
					color = Color.Green;
				} else if (player.alliance == level.mainPlayer.alliance) {
					color = Color.Yellow;
				}
				else {
					color = Color.Red;
				}
			} else {
				color = (player.alliance) switch  {
					0 => new Color(0, 255, 255), // Blue
					1 => new Color(255, 64, 64), // Red
					2 => new Color(128, 255, 128), // Green
					3 => new Color(160, 128, 255), // Purple 
					4 => new Color(255, 255, 128), // Yellow
					5 => new Color(255, 128, 128), // Orange.
					_ => Color.White,
				};
			}

			foreach (var spot in revealedSpots) {
				if (player.isMainPlayer || player.alliance == Global.level.mainPlayer.alliance ||
					new Point(xPosF, yPosF).distanceTo(new Point(spot.x, spot.y)) < spot.r
				) {
					float dxPos = radarX + MathF.Round(xPos) - offsetX;
					float dyPos = radarY + MathF.Round(yPos) - 1 - offsetY;
					if (dxPos < radarX || dxPos > radarX + scaledW ||
						dyPos < radarY || dyPos > radarY + scaledH
					) {
						continue;
					}
					DrawWrappers.DrawRectWH(
						dxPos, dyPos,
						1, 2,
						true, color, 0,
						ZIndex.HUD, isWorldPos: false
					);
					break;
				}
			}
		}

		// Radar rectangle itself (with border)
		DrawWrappers.DrawRectWH(
			radarX, radarY,
			scaledW, scaledH,
			true, Color.Transparent, 1,
			ZIndex.HUD, isWorldPos: false,
			outlineColor: Color.White
		);
		DrawWrappers.DrawRectWH(
			radarX-1, radarY-1,
			scaledW+2, scaledH+2,
			true, Color.Transparent, 1,
			ZIndex.HUD, isWorldPos: false,
			outlineColor: Color.Black
		);
	}

	public static List<Player> getOrderedPlayerList() {
		List<Player> playerList = Global.level.players.Where(p => !p.isSpectator).ToList();
		playerList.Sort((a, b) => {
			if (a.kills > b.kills) {
				return -1;
			}
			if (a.kills < b.kills) {
				return 1;
			}
			if (a.kills == b.kills) {
				if (a.deaths < b.deaths) {
					return -1;
				}
				if (a.deaths < b.deaths) {
					return 0;
				}
				return 1;
			}
			return 0;
		});
		return playerList;
	}

	public void assignPlayerHUDPositions() {
		var nonSpecPlayers = level.players.FindAll(p => p.is1v1Combatant && p != mainPlayer);
		if (mainPlayer != null) {
			nonSpecPlayers.Insert(0, mainPlayer);
		}

		// Two player case: just arrange left and right trivially
		if (nonSpecPlayers.Count <= 2) {
			hudLeftPlayer = nonSpecPlayers.ElementAtOrDefault(0);
			hudRightPlayer = nonSpecPlayers.ElementAtOrDefault(1);
		}
		// Three player case with mainPlayer
		else if (nonSpecPlayers.Count == 3 && mainPlayer != null) {
			// Not a team mode: put main player on left, others on right
			if (!isTeamMode) {
				hudLeftPlayer = nonSpecPlayers[0];
				hudTopRightPlayer = nonSpecPlayers[1];
				hudBotRightPlayer = nonSpecPlayers[2];
			}
			// If team mode, group main player on left with first ally.
			else {
				int mainPlayerAlliance = mainPlayer.alliance;
				var mainPlayerAllies = nonSpecPlayers.FindAll(p => p != mainPlayer && p.alliance == mainPlayer.alliance);
				if (mainPlayerAllies.Count == 0) {
					hudLeftPlayer = nonSpecPlayers[0];
					hudTopRightPlayer = nonSpecPlayers[1];
					hudBotRightPlayer = nonSpecPlayers[2];
				} else {
					hudTopLeftPlayer = nonSpecPlayers[0];
					hudBotLeftPlayer = mainPlayerAllies[0];
					hudRightPlayer = nonSpecPlayers.FirstOrDefault(p => p != nonSpecPlayers[0] && p != mainPlayerAllies[0]);
				}
			}
		} else {
			// Four players with main player and team mode: group main player with any allies on left if they exist
			if (nonSpecPlayers.Count == 4 && mainPlayer != null && isTeamMode) {
				int allyIndex = nonSpecPlayers.FindIndex(p => p != mainPlayer && p.alliance == mainPlayer.alliance);
				if (allyIndex != -1) {
					var temp = nonSpecPlayers[2];
					nonSpecPlayers[2] = nonSpecPlayers[allyIndex];
					nonSpecPlayers[allyIndex] = temp;
				}
			}

			hudTopLeftPlayer = nonSpecPlayers.ElementAtOrDefault(0);
			hudTopRightPlayer = nonSpecPlayers.ElementAtOrDefault(1);
			hudBotLeftPlayer = nonSpecPlayers.ElementAtOrDefault(2);
			hudBotRightPlayer = nonSpecPlayers.ElementAtOrDefault(3);
		}
	}

	public void renderHealthAndWeapons() {
		bool is1v1OrTraining = level.is1v1() || level.levelData.isTraining();
		if (!is1v1OrTraining) {
			renderHealthAndWeapon(level.mainPlayer, HUDHealthPosition.Left);
		} else {
			renderHealthAndWeapon(level.mainPlayer, HUDHealthPosition.Left);
			Player? rightPlayer = Global.level.players.FirstOrDefault(
				(Player player) => player.character != null && player != level.mainPlayer
			);
			if (rightPlayer != null) {
				renderHealthAndWeapon(rightPlayer, HUDHealthPosition.Right);
			}
		}
	}

	public void renderHealthAndWeapon(Player? player, HUDHealthPosition position) {
		if (player == null) return;
		if (level.is1v1() && player.deaths >= playingTo) return;

		player.lastCharacter?.renderHUD(new Point(), position);
	}

	public static Point getHUDHealthPosition(HUDHealthPosition position, bool isHealth) {
		float x = 0;
		if (position is HUDHealthPosition.Left) {
			x = isHealth ? 16 : 32;
		} else {
			x = isHealth ? Global.screenW - 17 : Global.screenW - 33;
		}
		return new Point(x, 88);
	}

	public static Point getHUDBuffPosition(HUDHealthPosition position) {
		int offset = 2;
		if (position == GameMode.HUDHealthPosition.Right) {
			offset = -1;
		}
		return getHUDHealthPosition(position, true).addxy(offset, 9);
	}

	public bool renderHealth(Player player, HUDHealthPosition position, bool isMech) {
		bool mechBarExists = false;

		string spriteName = "hud_health_base";
		float health = player.health;
		float maxHealth = player.maxHealth;
		float damageSavings = 0;

		if (player.character != null && player.health > 0 && player.health < player.maxHealth) {
			damageSavings = MathInt.Floor(player.character.damageSavings);
		}

		if (player.currentMaverick != null) {
			health = player.currentMaverick.health;
			maxHealth = player.currentMaverick.maxHealth;
			damageSavings = 0;
		}

		int frameIndex = 0;
		if (player.charNum == (int)CharIds.Blues) {
			frameIndex = 1;
		}

		var hudHealthPosition = getHUDHealthPosition(position, true);
		float baseX = hudHealthPosition.x;
		float baseY = hudHealthPosition.y;

		if (player.isBlues) {
			baseY += 17;
		}

		float twoLayerHealth = 0;
		if (isMech && player.character?.rideArmor != null && player.character.rideArmor.raNum != 5) {
			spriteName = "hud_health_base_mech";
			health = player.character.rideArmor.health;
			maxHealth = player.character.rideArmor.maxHealth;
			twoLayerHealth = player.character.rideArmor.goliathHealth;
			frameIndex = player.character.rideArmor.raNum;
			baseX = getHUDHealthPosition(position, false).x;
			mechBarExists = false;
			if (player.weapon.drawAmmo) {
				baseX += 15;
			}
			damageSavings = 0;
		}
		if (isMech && player.character?.rideArmorPlatform != null) {
			spriteName = "hud_health_base_mech";
			health = player.character.rideArmorPlatform.health;
			maxHealth = player.character.rideArmorPlatform.maxHealth;
			twoLayerHealth = player.character.rideArmorPlatform.goliathHealth;
			frameIndex = player.character.rideArmorPlatform.raNum;
			baseX = getHUDHealthPosition(position, false).x;
			if (player.weapon.drawAmmo) {
				baseX += 15;
			}
			mechBarExists = false;
			damageSavings = 0;
		}

		if (isMech && player.character?.rideChaser != null) {
			spriteName = "hud_health_base_bike";
			health = player.character.rideChaser.health;
			maxHealth = player.character.rideChaser.maxHealth;
			frameIndex = 0;
			baseX = getHUDHealthPosition(position, false).x;
			mechBarExists = true;
			damageSavings = 0;
		}

		//maxHealth /= player.getHealthModifier();
		//health /= player.getHealthModifier();
		//damageSavings /= player.getHealthModifier();

		baseY += 25;
		var healthBaseSprite = spriteName;
		Global.sprites[healthBaseSprite].drawToHUD(frameIndex, baseX, baseY);
		baseY -= 16;
		int barIndex = 0;

		for (var i = 0; i < MathF.Ceiling(player.getMaxHealth()); i++) {
			float trueHP = player.getMaxHealth() - (player.evilEnergyStacks * player.hpPerStack); 
			// Draw HP
			if (i < MathF.Ceiling(health)) {
				Global.sprites["hud_health_full"].drawToHUD(barIndex, baseX, baseY);
			} else if (i < MathInt.Ceiling(health) + damageSavings) {
				Global.sprites["hud_health_full"].drawToHUD(4, baseX, baseY);
			} 
			//Evil Energy lost HP
			else if (i >= trueHP && player.character != null && !player.character.destroyed) {
				float t = player.evilEnergyTime / 10f;
				int color = Global.frameCount % t <= 10 ? 1 : 2;
				Global.sprites["hud_energy_full"].drawToHUD(color, baseX, baseY);
			} else {
				Global.sprites["hud_health_empty"].drawToHUD(0, baseX, baseY);
			}
			// 2-layer health
			if (twoLayerHealth > 0 && i < MathF.Ceiling(twoLayerHealth)) {
				Global.sprites["hud_health_full"].drawToHUD(2, baseX, baseY);
			}
			baseY -= 2;
		}
		Global.sprites["hud_health_top"].drawToHUD(0, baseX, baseY);

		return mechBarExists;
	}

	const int grayAmmoIndex = 30;
	public static void renderAmmo(
		float baseX, float baseY, int baseIndex,
		int barIndex, float ammo, float grayAmmo = 0, float maxAmmo = 32,
		bool allowSmall = true, string barSprite = "hud_weapon_full", string baseSprite = "hud_weapon_base",
		int eeStacks = 0
	) {
		if (baseIndex >= 0) {
			Global.sprites[baseSprite].drawToHUD(baseIndex, baseX, baseY);
		} else if (baseIndex == -2) {
			Global.sprites["hud_core_base"].drawToHUD(0, baseX, baseY);
		} else if (baseIndex == -3) {
			Global.sprites["hud_energy_base"].drawToHUD(eeStacks, baseX, baseY);
		}
		baseY -= 16;

		for (var i = 0; i < MathF.Ceiling(maxAmmo); i++) {
			if (i < Math.Ceiling(ammo)) {
				if (ammo < grayAmmo) {
					Global.sprites["hud_weapon_full"].drawToHUD(grayAmmoIndex, baseX, baseY);
				}
				else {
					Global.sprites[barSprite].drawToHUD(barIndex, baseX, baseY);
				}
			} else {
				Global.sprites["hud_health_empty"].drawToHUD(0, baseX, baseY);
			}
			baseY -= 2;
		}
		if (baseIndex == -3) {
			Global.sprites["hud_energy_top"].drawToHUD(eeStacks, baseX, baseY);
		} else {
			Global.sprites["hud_health_top"].drawToHUD(0, baseX, baseY);
		}
	}

	public bool shouldDrawWeaponAmmo(Player player, Weapon weapon) {
		if (weapon == null) return false;
		if (weapon.weaponSlotIndex == 0) return false;
		if (!weapon.drawAmmo) return false;
		if (weapon is HyperNovaStrike && level.isHyper1v1()) return false;
		if (weapon is RockBuster buster) return false;
		if (weapon is SARocketPunch) return false;

		return true;
	}

	public void renderWeapon(Player player, HUDHealthPosition position) {
		var hudHealthPosition = getHUDHealthPosition(position, false);
		float baseX = hudHealthPosition.x;
		float baseY = hudHealthPosition.y;
		bool forceSmallBarsOff = false;

		// This runs once per character.
		Weapon? weapon = player.lastHudWeapon;
		if (player.character != null) {
			weapon = player.weapon;
			player.lastHudWeapon = weapon;
		}
		if (player.character is Bass bass && bass.isSuperBass) {
			int energy = bass.evilEnergy[0];
			int energy2 = bass.evilEnergy[1];
			int maxEnergy = Bass.MaxEvilEnergy;
			int stacks = player.pendingEvilEnergyStacks;
			bool charging = bass.charState is EnergyCharge or EnergyIncrease;

			// Level 1 Evil Energy Bar.
			int color = energy >= maxEnergy ? 3 : 1;
			color = Global.frameCount % 6 >= 3 ? 4 : color;
 
			renderAmmo(
				baseX, baseY, -3, color, MathF.Ceiling(energy),
				maxAmmo: maxEnergy, barSprite: "hud_energy_full"
			); 

			//Bar Base skull eyes
			if ((charging || energy2 >= maxEnergy) && Global.frameCount % 3 == 0) {
				Global.sprites["hud_energy_eyes"].drawToHUD(stacks, baseX, baseY + 25);
			}

			//Level 2 Evil Energy Bar.
			if (energy2 > 0) {
				int yPos = MathInt.Ceiling(9 + baseY);
				int color2 = energy2 >= maxEnergy ? 7 : 5;
				color2 = Global.frameCount % 6 >= 3 ? 8 : color2;

				for (int i = 0; i < energy2; i++) {
					Global.sprites["hud_energy_full"].drawToHUD(color2, baseX, yPos);
					yPos -= 2;
				}
			}
		}

		// Return if there is no weapon to render.
		if (weapon == null) {
			return;
		}

		// Small Bars option.
		float ammoDisplayMultiplier = 1;
		if (weapon?.allowSmallBar == true && Options.main.enableSmallBars && !forceSmallBarsOff) {
			ammoDisplayMultiplier = 0.5f;
		}

		if (shouldDrawWeaponAmmo(player, weapon)) {
			baseY += 25;
			string baseBarName = player.isRock ? "hud_weapon_base" : "hud_weapon_base_bass";
			string fullBarName = player.isRock ? "hud_weapon_full" : "hud_weapon_full_bass";
			Global.sprites[baseBarName].drawToHUD(weapon.weaponBarBaseIndex, baseX, baseY);
			baseY -= 16;
			for (int i = 0; i < MathF.Ceiling(weapon.maxAmmo * ammoDisplayMultiplier); i += (int)weapon.ammoDisplayScale) {
				var floorOrCeiling = Math.Ceiling(weapon.ammo * ammoDisplayMultiplier);
				// Weapons that cost the whole bar go here, so they don't show up as full but still grayed out
				if (weapon.drawRoundedDown || weapon is RekkohaWeapon || weapon is GigaCrush) {
					floorOrCeiling = Math.Floor(weapon.ammo * ammoDisplayMultiplier);
				}
				if (i < floorOrCeiling) {
					int spriteIndex = weapon.weaponBarIndex;
					if (weapon.drawGrayOnLowAmmo && weapon.ammo < weapon.getAmmoUsage(0) ||
						(weapon is GigaCrush && !weapon.canShoot(0, player)) ||
						(weapon is HyperNovaStrike && !weapon.canShoot(0, player)) ||
						(weapon is HyperCharge hb && !hb.canShootIncludeCooldown(level.mainPlayer))) {
						spriteIndex = grayAmmoIndex;
					}
					if (spriteIndex >= Global.sprites["hud_weapon_full"].frames.Length) {
						spriteIndex = 0;
					}
					Global.sprites[fullBarName].drawToHUD(spriteIndex, baseX, baseY);
				} else {
					Global.sprites["hud_health_empty"].drawToHUD(0, baseX, baseY);
				}
				baseY -= 2;
			}
			Global.sprites["hud_health_top"].drawToHUD(0, baseX, baseY);
		}
		//if (shouldDrawWeaponAmmo(player, weapon) && player.isIris) {
		//	Global.sprites["iris_hud"].drawToHUD(0, 25, 125);
		//}
	}

	public void addKillFeedEntry(KillFeedEntry killFeed, bool sendRpc = false) {
		killFeedHistory.Add(killFeed.rawString());
		this.killFeed.Insert(0, killFeed);
		if (this.killFeed.Count > 10) this.killFeed.Pop();
		if (sendRpc) {
			killFeed.sendRpc();
		}
	}

	public float killFeedOffset() {
		return 57;
	}

	public FontType getKillFeedTeamFonts(int team) {
		return team switch {
			0 => FontType.BlueSmall,
			1 => FontType.RedSmall,
			2 => FontType.GreenSmall,
			3 => FontType.PurpleSmall,
			4 => FontType.YellowSmall,
			5 => FontType.OrangeSmall,
			_ => FontType.WhiteSmall
		};
	}

	public void drawKillFeed() {
		float fromRight = Global.screenW - 8;
		int yDist = 12;
		float fromTop = killFeedOffset();

		for (var i = 0; i < this.killFeed.Count && i < 3; i++) {
			var killFeed = this.killFeed[i];

			string victimName = killFeed.victim?.name ?? "";
			if (killFeed.maverickKillFeedIndex != null) {
				victimName = " (" + victimName + ")";
			}

			FontType victimColor = FontType.WhiteSmall;
			FontType killerColor = FontType.WhiteSmall;
			FontType assisterColor = FontType.WhiteSmall;

			if (killFeed.victim != null && killFeed.killer != null) {
				if (!isTeamMode) {
					if (killFeed.killer == Global.level.mainPlayer && killFeed.victim == killFeed.killer) {
						victimColor = FontType.WhiteSmall;
						killerColor = FontType.BlueSmall;
						assisterColor = FontType.PurpleSmall;
					} else if (killFeed.killer == Global.level.mainPlayer) {
						victimColor = FontType.RedSmall;
						killerColor = FontType.BlueSmall;
						assisterColor = FontType.PurpleSmall;
					}
					else if (killFeed.assister != null && killFeed.assister == Global.level.mainPlayer) {
						victimColor = FontType.RedSmall;
						killerColor = FontType.PurpleSmall;
						assisterColor = FontType.BlueSmall;
					}
					else if (killFeed.victim == Global.level.mainPlayer) {
						victimColor = FontType.BlueSmall;
						killerColor = FontType.RedSmall;
						assisterColor = FontType.OrangeSmall;
					} else {
						victimColor = FontType.RedSmall;
						killerColor = FontType.GreenSmall;
						assisterColor = FontType.PurpleSmall;
					}
				} else {
					victimColor = getKillFeedTeamFonts(killFeed.victim.teamAlliance ?? 100);
					killerColor = getKillFeedTeamFonts(killFeed.killer.teamAlliance ?? 100);
					if (killFeed.assister != null) {
						assisterColor = getKillFeedTeamFonts(killFeed.assister.teamAlliance ?? 100);
					}
				}
			}

			string msg = "";
			string killersMsg = "";
			string assistMsg = "";
			if (killFeed.killer != null && killFeed.killer != killFeed.victim) {
				var killerMessage = "";
				killerMessage = killFeed.killer.name;

				if (killFeed.assister != null && killFeed.assister != killFeed.victim && (
					!isTeamMode || killFeed.assister.alliance != killFeed.killer.alliance
				)) {
					assistMsg = killFeed.assister.name;
				}
				killersMsg = killerMessage;
				msg = killersMsg + " " + victimName;
				if (assistMsg != "") {
					msg = assistMsg + "&" + killersMsg + " " + victimName;
				}
			} else if (killFeed.victim != null && killFeed.customMessage == null) {
				if (killFeed.maverickKillFeedIndex != null) {
					msg = killFeed.victim.name + "'s Maverick died";
				} else {
					msg = victimName + " died";
				}
			} else {
				msg = killFeed.customMessage;
			}

			if (killFeed.killer == level.mainPlayer ||
				killFeed.victim == level.mainPlayer ||
				killFeed.assister == level.mainPlayer
			) {
				int msgLen = Fonts.measureText(FontType.WhiteSmall, msg);
				if (killFeed.killer == null || killFeed.killer == killFeed.victim) {
					msgLen -= 2;
				}
				int msgHeight = 10;
				DrawWrappers.DrawRect(
					fromRight - msgLen - 3,
					fromTop + 5 + (i * yDist) - msgHeight / 2,
					fromRight + 1,
					fromTop + 4 + msgHeight / 2 + (i * yDist),
					true, new Color(0, 0, 0, 128), 1, ZIndex.HUD,
					isWorldPos: false, outlineColor: Color.White
				);
			}

			if (killFeed.killer != null && killFeed.killer != killFeed.victim) {
				int nameLen = Fonts.measureText(FontType.WhiteSmall, victimName + " ");
				Fonts.drawText(
					victimColor, victimName, fromRight, fromTop + (i * yDist), Alignment.Right
				);
				Fonts.drawText(
					killerColor, killersMsg, fromRight - nameLen - 2, fromTop + (i * yDist), Alignment.Right
				);
				if (assistMsg != "") {
					int nameLen2 = Fonts.measureText(
						FontType.WhiteSmall, $"{killersMsg} {victimName}"
					);
					Fonts.drawText(
						FontType.WhiteSmall, "&", fromRight - nameLen2 - 3, fromTop + (i * yDist), Alignment.Right
					);
					Fonts.drawText(
						assisterColor, assistMsg, fromRight - nameLen2 - 12, fromTop + (i * yDist), Alignment.Right
					);
				}
				int weaponIndex = killFeed.weaponIndex ?? 0;
				weaponIndex = (
					weaponIndex < Global.sprites["hud_killfeed_weapon"].frames.Length ? weaponIndex : 0
				);
				Global.sprites["hud_killfeed_weapon"].drawToHUD(
					0, fromRight - nameLen + 5, fromTop + (i * yDist) + 4
				);
			} else {
				Fonts.drawText(
					victimColor, msg, fromRight, fromTop + (i * yDist), Alignment.Right
				);
			}
		}
	}

	public void drawSpectators() {
		var spectatorNames = level.players.Where(p => p.isSpectator).Select((p) => {
			bool isHost = p.serverPlayer?.isHost ?? false;
			return p.name + (isHost ? " (Host)" : "");
		});
		string spectatorStr = string.Join(",", spectatorNames);
		if (!string.IsNullOrEmpty(spectatorStr)) {
			Fonts.drawText(FontType.BlueMenu, "Spectators: " + spectatorStr, 15, 200);
		}
	}

	public void drawDiagnostics() {
		if (Global.showDiagnostics) {
			double? downloadedBytes = 0;
			double? uploadedBytes = 0;

			if (Global.serverClient?.client?.ServerConnection?.Statistics != null) {
				downloadedBytes = Global.serverClient.client.ServerConnection.Statistics.ReceivedBytes;
				uploadedBytes = Global.serverClient.client.ServerConnection.Statistics.SentBytes;
			}

			int topLeftX = 10;
			int topLeftY = 24;
			int w = 120;
			int lineHeight = 9;

			DrawWrappers.DrawRect(
				topLeftX - 2,
				topLeftY + lineHeight - 2,
				topLeftX + w,
				topLeftY + currentLineH + lineHeight - 1,
				true, Helpers.MenuBgColor, 1, ZIndex.HUD - 10, isWorldPos: false
			);

			currentLineH = 0;

			bool showNetStats = Global.debug;
			if (showNetStats) {
				if (downloadedBytes != null) {
					string downloadMb = (downloadedBytes.Value / 1000000.0).ToString("0.00");
					string downloadKb = (downloadedBytes.Value / 1000.0).ToString("0.00");
					Fonts.drawText(
						FontType.Grey,
						"Bytes received: " + downloadMb + " mb" + " (" + downloadKb + " kb)",
						topLeftX, topLeftY + (currentLineH += lineHeight)
					);
				}
				if (uploadedBytes != null) {
					string uploadMb = (uploadedBytes.Value / 1000000.0).ToString("0.00");
					string uploadKb = (uploadedBytes.Value / 1000.0).ToString("0.00");
					Fonts.drawText(
						FontType.Grey,
						"Bytes sent: " + uploadMb + " mb" + " (" + uploadKb + " kb)",
						topLeftX, topLeftY + (currentLineH += lineHeight)
					);
				}

				double avgPacketIncrease = Global.lastFramePacketIncreases.Count == 0 ? 0 : Global.lastFramePacketIncreases.Average();
				Fonts.drawText(
					FontType.Grey,
					"Packet rate: " + (avgPacketIncrease * 60f).ToString("0") + " bytes/second", topLeftX, topLeftY + (currentLineH += lineHeight)
				);
				Fonts.drawText(
					FontType.Grey,
					"Packet rate: " + avgPacketIncrease.ToString("0") + " bytes/frame", topLeftX, topLeftY + (currentLineH += lineHeight)
				);
			}

			double avgPacketsReceived = Global.last10SecondsPacketsReceived.Count == 0 ? 0 : Global.last10SecondsPacketsReceived.Average();
			Fonts.drawText(
				FontType.Grey,
				"Ping Packets / sec: " + avgPacketsReceived.ToString("0.0"),
				topLeftX, topLeftY + (currentLineH += lineHeight)
			);
			Fonts.drawText(
				FontType.Grey,
				"Start GameObject Count: " + level.startGoCount, topLeftX, topLeftY + (currentLineH += lineHeight)
			);
			Fonts.drawText(
				FontType.Grey,
				"Current GameObject Count: " + level.gameObjects.Count, topLeftX, topLeftY + (currentLineH += lineHeight)
			);

			Fonts.drawText(
				FontType.Grey,
				"GridItem Count: " +
				level.startGridCount + "-" + level.getGridCount(),
				topLeftX, topLeftY + (currentLineH += lineHeight)
			);
			Fonts.drawText(
				FontType.Grey,
				"TGridItem Count: " +
				level.startTGridCount + "-" + level.getTGridCount(),
				topLeftX, topLeftY + (currentLineH += lineHeight)
			);

			Fonts.drawText(
				FontType.Grey,
				"Sound Count: " + Global.sounds.Count, topLeftX, topLeftY + (currentLineH += lineHeight)
			);

			/*Fonts.drawText(
				FontType.Grey,
				"List Counts: " + Global.level.getListCounts(), topLeftX, topLeftY + (currentLineH += lineHeight)
			);

			float avgFrameProcessTime = Global.lastFrameProcessTimes.Count == 0 ? 0 : Global.lastFrameProcessTimes.Average();

			Fonts.drawText(
				FontType.Grey,
				"Avg frame process time: " + avgFrameProcessTime.ToString("0.00") + " ms", topLeftX, topLeftY + (currentLineH += lineHeight)
			);
			*/
			//float graphYHeight = 20;
			//drawDiagnosticsGraph(Global.lastFrameProcessTimes, topLeftX, topLeftY + (currentLineH += lineHeight) + graphYHeight, 1);

		}
	}

	public void drawDiagnosticsGraph(List<float> values, float startX, float startY, float yScale) {
		for (int i = 1; i < values.Count; i++) {
			DrawWrappers.DrawLine(startX + i - 1, startY + (values[i - 1] * yScale), startX + i, startY + (values[i] * yScale), Color.Green, 0.5f, ZIndex.HUD, false);
		}
	}

	public void drawWeaponSwitchHUD(Player player) {
		if (player.isZero && !player.isDisguisedAxl) return;

		if (player.isSelectingRA()) {
			drawRideArmorIcons();
		}

		if (player.character is Vile vilePilot &&
			vilePilot.rideArmor != null &&
			vilePilot.rideArmor == vilePilot.linkedRideArmor
			&& vilePilot.rideArmor.raNum == 2
		) {
			int x = 10, y = 155;
			int napalmNum = player.loadout.vileLoadout.napalm;
			if (napalmNum < 0) napalmNum = 0;
			if (napalmNum > 2) napalmNum = 0;
			Global.sprites["hud_hawk_bombs"].drawToHUD(
				napalmNum, x, y, alpha: vilePilot.napalmWeapon.shootCooldown == 0 ? 1 : 0.5f
			);
			Fonts.drawText(
				FontType.Grey, "x" + vilePilot.rideArmor.hawkBombCount.ToString(), x + 10, y - 4
			);
		}

		if (player.character?.rideArmor != null || player.character?.rideChaser != null) {
			return;
		}

		var iconW = 8;
		var iconH = 8;
		var width = 15;

		var startX = getWeaponSlotStartX(player, ref iconW, ref iconH, ref width);
		var startY = Global.screenH - 12;

		int gigaWeaponX = 18;
		if (player.isRock && Options.main.rushSpecial) {
			Weapon? rushWeapon = player.weapons.FirstOrDefault((Weapon w) => w is RushWeapon);
			if (rushWeapon != null) {
				drawWeaponSlot(rushWeapon, gigaWeaponX, 97);
				gigaWeaponX += 18;
			}
		}
		if (player.character is MegamanX mmx && mmx.hasFgMoveEquipped() && mmx.canAffordFgMove()) {
			int x = gigaWeaponX, y = 159;
			Global.sprites["hud_weapon_icon"].drawToHUD(mmx.hasHadoukenEquipped() ? 112 : 113, x, y);
			float cooldown = Helpers.progress(player.fgMoveAmmo, 1920f);
			drawWeaponSlotCooldown(x, y, cooldown);
		}

		if (player.isAxl && player.weapons[0].type > 0) {
			int x = 10, y = 156;
			int index = 0;
			if (player.weapons[0].type == (int)AxlBulletWeaponType.MetteurCrash) index = 0;
			if (player.weapons[0].type == (int)AxlBulletWeaponType.BeastKiller) index = 1;
			if (player.weapons[0].type == (int)AxlBulletWeaponType.MachineBullets) index = 2;
			if (player.weapons[0].type == (int)AxlBulletWeaponType.DoubleBullets) index = 3;
			if (player.weapons[0].type == (int)AxlBulletWeaponType.RevolverBarrel) index = 4;
			if (player.weapons[0].type == (int)AxlBulletWeaponType.AncientGun) index = 5;
			Global.sprites["hud_axl_ammo"].drawToHUD(index, x, y);
			int currentAmmo = MathInt.Ceiling(player.weapons[0].ammo);
			int totalAmmo = MathInt.Ceiling(player.axlBulletTypeAmmo[player.weapons[0].type]);
			Fonts.drawText(
				FontType.Grey, totalAmmo.ToString(), x + 10, y - 4
			);
		}

		if (player.isGridModeEnabled()) {
			if (player.gridModeHeld == true) {
				var gridPoints = player.gridModePoints();
				for (var i = 0; i < player.weapons.Count && i < 9; i++) {
					Point pos = gridPoints[i];
					var weapon = player.weapons[i];
					var x = Global.halfScreenW + (pos.x * 20);
					var y = Global.screenH - 30 + pos.y * 20;

					drawWeaponSlot(weapon, x, y);
				}
			}

			/*
			// Draw giga crush/hyper buster
			if (player.weapons.Count == 10)
			{
				int x = 10, y = 146;
				Weapon weapon = player.weapons[9];

				drawWeaponSlot(weapon, x, y);

				//Global.sprites["hud_weapon_icon"].drawToHUD(weapon.weaponSlotIndex, x, y);
				//DrawWrappers.DrawRectWH(
					//x - 8, y - 8, 16, 16 - MathF.Floor(16 * (weapon.ammo / weapon.maxAmmo)),
					//true, new Color(0, 0, 0, 128), 1, ZIndex.HUD, false
				//);
			}
			*/
			return;
		}

		for (var i = 0; i < player.weapons.Count; i++) {
			var weapon = player.weapons[i];
			var x = startX + (i * width);
			var y = startY;
			if (weapon is HyperCharge hb) {
				bool canShootHyperBuster = hb.canShootIncludeCooldown(player);
				Color lineColor = canShootHyperBuster ? Color.White : Helpers.Gray;

				float slotPosX = startX + (player.hyperChargeSlot * width);
				int yOff = -1;

				// Stretch black
				DrawWrappers.DrawRect(
					slotPosX, y - 9 + yOff, x, y - 12 + yOff,
					true, Color.Black, 1, ZIndex.HUD, false
				);
				// Right
				DrawWrappers.DrawRect(
					x - 1, y - 7, x + 2, y - 12 + yOff,
					true, Color.Black, 1, ZIndex.HUD, false
				);
				DrawWrappers.DrawRect(
					x, y - 8, x + 1, y - 11 + yOff,
					true, lineColor, 1, ZIndex.HUD, false
				);
				// Left
				DrawWrappers.DrawRect(
					slotPosX - 1, y - 7, slotPosX + 2, y - 12 + yOff,
					true, Color.Black, 1, ZIndex.HUD, false
				);
				DrawWrappers.DrawRect(
					slotPosX, y - 8, slotPosX + 1, y - 11 + yOff,
					true, lineColor, 1, ZIndex.HUD, false
				);
				// Stretch white
				DrawWrappers.DrawRect(
					slotPosX, y - 10 + yOff, x, y - 11 + yOff,
					true, lineColor, 1, ZIndex.HUD, false
				);
				break;
			}
		}
		int offsetX = 0;
		for (var i = 0; i < player.weapons.Count; i++) {
			var weapon = player.weapons[i];
			var x = startX + (i * width) + offsetX;
			var y = startY;
			if (player.isX && Options.main.gigaCrushSpecial && weapon is GigaCrush) {
				offsetX -= width;
				continue;
			}
			if (player.isX && Options.main.novaStrikeSpecial && weapon is HyperNovaStrike) {
				offsetX -= width;
				continue;
			}
			if (player.isRock && Options.main.rushSpecial && weapon is RushWeapon) {
				offsetX -= width;
				continue;
			}
			if (level.mainPlayer.weapon == weapon && !level.mainPlayer.isSelectingCommand()) {
				DrawWrappers.DrawRectWH(
					x - 7, y - 8, 14, 15, false,
					Color.Black, 1, ZIndex.HUD, false
				);
				drawWeaponSlot(weapon, x, y-1, true);
			} else {
				drawWeaponSlot(weapon, x, y);
			}
		}

		if (player == mainPlayer && mainPlayer.isSelectingCommand()) {
			drawMaverickCommandIcons();
		}
	}

	public void drawGigaWeaponCooldown(int slotIndex, float cooldown, int x = 18, int y = 97) {
		string iconName = mainPlayer.isRock ? "hud_weapon_icon" : "hud_blues_weapon_icon";
		Global.sprites[iconName].drawToHUD(slotIndex, x, y);
		drawWeaponSlotCooldown(x, y, cooldown);
	}

	public void drawWeaponSlot(Weapon weapon, float x, float y, bool selected = false) {
		string jsonName = mainPlayer.isBass ? "hud_weapon_icon_bass" : "hud_weapon_icon";
		if (weapon is MechMenuWeapon && !mainPlayer.isSpectator && level.mainPlayer.character?.linkedRideArmor != null) {
			int index = 37 + level.mainPlayer.character.linkedRideArmor.raNum;
			if (index == 42) index = 119;
			Global.sprites["hud_weapon_icon"].drawToHUD(index, x, y);
		} else if (weapon is MechMenuWeapon && level.mainPlayer.isSelectingRA()) {
			return;
		} else if (weapon is not AbsorbWeapon) {
			Global.sprites[jsonName].drawToHUD(weapon.weaponSlotIndex, x, y);
		}
		if (selected) {
			if (!weapon.canShoot(0, mainPlayer)) {
				drawWeaponStateOverlay(x, y, 2);
			} else if (weapon.shootCooldown > 0 && weapon.fireRate > 10 && weapon.drawCooldown) {
				drawWeaponStateOverlay(x, y, 1);
			} else if (selected) {
				drawWeaponStateOverlay(x, y, 0);
			}
		}
		if (weapon.ammo < weapon.maxAmmo && weapon.drawAmmo) {
			drawWeaponSlotAmmo(x, y, weapon.ammo / weapon.maxAmmo);
		}

		if (weapon is MechaniloidWeapon mew) {
			if (mew.mechaniloidType == MechaniloidType.Tank && level.mainPlayer.tankMechaniloidCount() > 0) {
				drawWeaponText(x, y, level.mainPlayer.tankMechaniloidCount().ToString());
			} else if (mew.mechaniloidType == MechaniloidType.Hopper && level.mainPlayer.hopperMechaniloidCount() > 0) {
				drawWeaponText(x, y, level.mainPlayer.hopperMechaniloidCount().ToString());
			} else if (mew.mechaniloidType == MechaniloidType.Bird && level.mainPlayer.birdMechaniloidCount() > 0) {
				drawWeaponText(x, y, level.mainPlayer.birdMechaniloidCount().ToString());
			} else if (mew.mechaniloidType == MechaniloidType.Fish && level.mainPlayer.fishMechaniloidCount() > 0) {
				drawWeaponText(x, y, level.mainPlayer.fishMechaniloidCount().ToString());
			}
		}

		if (weapon is MagicCard && level.mainPlayer.character is Bass bass && bass.cardsCount > 0) {
			drawWeaponText(x, y - 18, bass.cardsCount.ToString());
		}

		if (weapon is BlastLauncher && level.mainPlayer.axlLoadout.blastLauncherAlt == 1 && level.mainPlayer.grenades.Count > 0) {
			drawWeaponText(x, y, level.mainPlayer.grenades.Count.ToString());
		}

		if (weapon is DNACore dnaCore && level.mainPlayer.weapon == weapon && level.mainPlayer.input.isHeld(Control.Special1, level.mainPlayer)) {
			drawTransformPreviewInfo(dnaCore, x, y);
		}
		 
		if (weapon is SigmaMenuWeapon) {
			drawWeaponSlotCooldown(x, y, weapon.shootCooldown / 4);
		}

		if (Global.debug && Global.quickStart && weapon is AxlWeapon aw2 && weapon is not DNACore) {
			drawWeaponSlotCooldownBar(x, y, aw2.shootCooldown / aw2.fireRate);
			drawWeaponSlotCooldownBar(x, y, aw2.altShotCooldown / aw2.altFireCooldown, true);
		}

		MaverickWeapon? mw = weapon as MaverickWeapon;
		if (mw != null) {
			float maxHealth = level.mainPlayer.getMaverickMaxHp();
			if (level.mainPlayer.isSummoner()) {
				float mHealth = mw.maverick?.health ?? mw.lastHealth;
				float mMaxHealth = mw.maverick?.maxHealth ?? maxHealth;
				if (!mw.summonedOnce) mHealth = 0;
				drawWeaponSlotAmmo(x, y, mHealth / mMaxHealth);
				drawWeaponSlotCooldown(x, y, mw.shootCooldown / MaverickWeapon.summonerCooldown);
			} else if (level.mainPlayer.isPuppeteer()) {
				float mHealth = mw.maverick?.health ?? mw.lastHealth;
				float mMaxHealth = mw.maverick?.maxHealth ?? maxHealth;
				if (!mw.summonedOnce) mHealth = 0;
				drawWeaponSlotAmmo(x, y, mHealth / mMaxHealth);
			} else if (level.mainPlayer.isStriker()) {
				float mHealth = mw.maverick?.health ?? mw.lastHealth;
				float mMaxHealth = mw.maverick?.maxHealth ?? maxHealth;
				if (level.mainPlayer.isStriker() && level.mainPlayer.mavericks.Count > 0 && mw.maverick == null) {
					mHealth = 0;
				}

				drawWeaponSlotAmmo(x, y, mHealth / mMaxHealth);
				drawWeaponSlotCooldown(x, y, mw.cooldown / MaverickWeapon.strikerCooldown);
			} else if (level.mainPlayer.isTagTeam()) {
				float mHealth = mw.maverick?.health ?? mw.lastHealth;
				float mMaxHealth = mw.maverick?.maxHealth ?? maxHealth;
				if (!mw.summonedOnce) mHealth = 0;
				drawWeaponSlotAmmo(x, y, mHealth / mMaxHealth);
				drawWeaponSlotCooldown(x, y, mw.cooldown / MaverickWeapon.tagTeamCooldown);
			}

			if (mw is ChillPenguinWeapon) {
				for (int i = 0; i < mainPlayer.iceStatues.Count; i++) {
					Global.sprites["hud_ice_statue"].drawToHUD(0, x - 3 + (i * 6), y + 10);
				}
			}

			if (mw is DrDopplerWeapon ddw && ddw.ballType == 1) {
				Global.sprites["hud_doppler_weapon"].drawToHUD(ddw.ballType, x + 4, y + 4);
			}

			if (mw is WireSpongeWeapon && level.mainPlayer.seeds.Count > 0) {
				drawWeaponText(x, y, level.mainPlayer.seeds.Count.ToString());
			}

			if (mw is BubbleCrabWeapon && mw.maverick is BubbleCrab bc && bc.crabs.Count > 0) {
				drawWeaponText(x, y, bc.crabs.Count.ToString());
			}
		}

		/*if (level.mainPlayer.weapon == weapon && !level.mainPlayer.isSelectingCommand()) {
			drawWeaponSlotSelected(x, y);
		}*/

		if (weapon is AxlWeapon && Options.main.axlLoadout.altFireArray[Weapon.wiToFi(weapon.index)] == 1) {
			//Helpers.drawWeaponSlotSymbol(x - 8, y - 8, "²");
		}

		if (weapon is SigmaMenuWeapon) {
			if ((level.mainPlayer.isPuppeteer() || level.mainPlayer.isSummoner()) && level.mainPlayer.currentMaverickCommand == MaverickAIBehavior.Follow) {
				Helpers.drawWeaponSlotSymbol(x - 8, y - 8, "ª");
			}

			/*
			string commandModeSymbol = null;
			//if (level.mainPlayer.isSummoner()) commandModeSymbol = "SUM";
			if (level.mainPlayer.isPuppeteer()) commandModeSymbol = "PUP";
			if (level.mainPlayer.isStriker()) commandModeSymbol = "STK";
			if (level.mainPlayer.isTagTeam()) commandModeSymbol = "TAG";
			if (commandModeSymbol != null)
			{
				Helpers.drawTextStd(commandModeSymbol, x - 7, y + 4, Alignment.Left, fontSize: 12);
			}
			*/
		}

		if (mw != null) {
			if (mw.currencyHUDAnimTime > 0) {
				float animProgress = mw.currencyHUDAnimTime / MaverickWeapon.currencyHUDMaxAnimTime;
				float yOff = animProgress * 20;
				float alpha = Helpers.clamp01(1 - animProgress);
				Global.sprites["pickup_bolt_small"].drawToHUD(0, x - 6, y - yOff - 10, alpha);
				//DrawWrappers.DrawText("+1", x - 6, y - yOff - 10, Alignment.Center, )
				Fonts.drawText(FontType.RedishOrange, "+1", x - 4, y - yOff - 15, Alignment.Left);
			}
		}

		if (weapon is AbsorbWeapon aw) {
			var sprite = Global.sprites[aw.absorbedProj.sprite.name];

			float w = sprite.frames[0].rect.w();
			float h = sprite.frames[0].rect.h();

			float scaleX = Helpers.clampMax(10f / w, 1);
			float scaleY = Helpers.clampMax(10f / h, 1);

			Global.sprites["hud_weapon_icon"].draw(weapon.weaponSlotIndex, Global.level.camX + x, Global.level.camY + y, 1, 1, null, 1, 1, 1, ZIndex.HUD);
			Global.sprites[aw.absorbedProj.sprite.name].draw(0, Global.level.camX + x, Global.level.camY + y, 1, 1, null, 1, scaleX, scaleY, ZIndex.HUD);
		}
	}

	private void drawWeaponStateOverlay(float x, float y, int type) {
		Color cooldownColour = type switch {
			1 => new Color(255, 212, 128, 128),
			2 => new Color(255, 128, 128, 128),
			_ => new Color(128, 255, 128, 128),
		};
		DrawWrappers.DrawRectWH(
			x - 6f, y - 6f,
			12f, 12f, filled: false,
			cooldownColour, 1,
			1000000L, isWorldPos: false
		);
	}

	private void drawWeaponText(float x, float y, string text) {
		Fonts.drawText(
			FontType.Yellow, text, x + 1, y + 8, Alignment.Center
		);
	}

	private void drawWeaponSlotSelected(float x, float y) {
		DrawWrappers.DrawRectWH(x - 7, y - 7, 14, 14, false, new Color(0, 224, 0), 1, ZIndex.HUD, false);
	}

	public static void drawWeaponSlotAmmo(float x, float y, float val) {
		DrawWrappers.DrawRectWH(x - 8, y - 8, 16, 16 - MathF.Floor(16 * val), true, new Color(0, 0, 0, 128), 1, ZIndex.HUD, false);
	}

	public static void drawWeaponSlotCooldownBar(float x, float y, float val, bool isAlt = false) {
		if (val <= 0) return;
		val = Helpers.clamp01(val);

		float yPos = -8.5f;
		if (isAlt) yPos = 8.5f;
		DrawWrappers.DrawLine(x - 8, y + yPos, x + 8, y + yPos, Color.Black, 1, ZIndex.HUD, false);
		DrawWrappers.DrawLine(x - 8, y + yPos, x - 8 + (val * 16), y + yPos, Color.Yellow, 1, ZIndex.HUD, false);
	}

	public static void drawWeaponSlotCooldown(float x, float y, float val) {
		if (val <= 0) return;
		val = Helpers.clamp01(val);

		int sliceStep = 1;
		if (Options.main.particleQuality == 0) sliceStep = 4;
		if (Options.main.particleQuality == 1) sliceStep = 2;

		int gridLen = 16 / sliceStep;
		List<Point> points = new List<Point>(gridLen * 4);

		int startX = 0;
		int startY = -8;

		int xDir = -1;
		int yDir = 0;

		for (int i = 0; i < gridLen * 4; i++) {
			points.Add(new Point(x + startX, y + startY));
			startX += sliceStep * xDir;
			startY += sliceStep * yDir;

			if (xDir == -1 && startX == -8) {
				xDir = 0;
				yDir = 1;
			}
			if (yDir == 1 && startY == 8) {
				yDir = 0;
				xDir = 1;
			}
			if (xDir == 1 && startX == 8) {
				xDir = 0;
				yDir = -1;
			}
			if (yDir == -1 && startY == -8) {
				xDir = -1;
				yDir = 0;
			}
		}

		var slices = new List<List<Point>>(points.Count);
		for (int i = 0; i < points.Count; i++) {
			Point nextPoint = i + 1 >= points.Count ? points[0] : points[i + 1];
			slices.Add(new List<Point>() { new Point(x, y), points[i], nextPoint });
		}

		for (int i = 0; i < (int)(val * slices.Count); i++) {
			DrawWrappers.DrawPolygon(slices[i], new Color(0, 0, 0, 164), true, ZIndex.HUD, false);
		}
	}

	public void drawTransformPreviewInfo(DNACore dnaCore, float x, float y) {
		float sx = x - 50;
		float sy = y - 100;

		float leftX = sx + 15;

		DrawWrappers.DrawRect(sx, sy, x + 50, y - 18, true, new Color(0, 0, 0, 224), 1, ZIndex.HUD, false);
		Global.sprites["cursor"].drawToHUD(0, x, y - 13);
		int sigmaForm = dnaCore.loadout?.sigmaLoadout?.sigmaForm ?? 0;

		sy += 5;
		Fonts.drawText(FontType.RedishOrange, dnaCore.name, x, sy);
		sy += 30;
		if (dnaCore.charNum == 0) {
			if (dnaCore.ultimateArmor) {
				Global.sprites["menu_megaman"].drawToHUD(5, x, sy + 4);
			} else if (dnaCore.armorFlag == ushort.MaxValue) {
				Global.sprites["menu_megaman"].drawToHUD(4, x, sy + 4);
			} else {
				Global.sprites["menu_megaman_armors"].drawToHUD(0, x, sy + 4);
				int[] armorVals = MegamanX.getArmorVals(dnaCore.armorFlag);
				int boots = armorVals[2];
				int body = armorVals[0];
				int helmet = armorVals[3];
				int arm = armorVals[1];

				if (helmet == 1) Global.sprites["menu_megaman_armors"].drawToHUD(1, x, sy + 4);
				if (helmet == 2) Global.sprites["menu_megaman_armors"].drawToHUD(2, x, sy + 4);
				if (helmet >= 3) Global.sprites["menu_megaman_armors"].drawToHUD(3, x, sy + 4);

				if (body == 1) Global.sprites["menu_megaman_armors"].drawToHUD(4, x, sy + 4);
				if (body == 2) Global.sprites["menu_megaman_armors"].drawToHUD(5, x, sy + 4);
				if (body >= 3) Global.sprites["menu_megaman_armors"].drawToHUD(6, x, sy + 4);

				if (arm == 1) Global.sprites["menu_megaman_armors"].drawToHUD(7, x, sy + 4);
				if (arm == 2) Global.sprites["menu_megaman_armors"].drawToHUD(8, x, sy + 4);
				if (arm >= 3) Global.sprites["menu_megaman_armors"].drawToHUD(9, x, sy + 4);

				if (boots == 1) Global.sprites["menu_megaman_armors"].drawToHUD(10, x, sy + 4);
				if (boots == 2) Global.sprites["menu_megaman_armors"].drawToHUD(11, x, sy + 4);
				if (boots >= 3) Global.sprites["menu_megaman_armors"].drawToHUD(12, x, sy + 4);

				if (helmet == 15) Global.sprites["menu_chip"].drawToHUD(0, x, sy - 16 + 4);
				if (body == 15) Global.sprites["menu_chip"].drawToHUD(0, x - 2, sy - 5 + 4);
				if (arm == 15) Global.sprites["menu_chip"].drawToHUD(0, x - 9, sy - 2 + 4);
				if (boots == 15) Global.sprites["menu_chip"].drawToHUD(0, x - 12, sy + 10);
			}
		} else if (dnaCore.charNum == 1) {
			int index = 0;
			if (dnaCore.hyperMode == DNACoreHyperMode.BlackZero) index = 1;
			if (dnaCore.hyperMode == DNACoreHyperMode.AwakenedZero) index = 2;
			if (dnaCore.hyperMode == DNACoreHyperMode.NightmareZero) index = 3;
			Global.sprites["menu_zero"].drawToHUD(index, x, sy + 1);
		} else if (dnaCore.charNum == 2) {
			int index = 0;
			if (dnaCore.hyperMode == DNACoreHyperMode.VileMK2) index = 1;
			if (dnaCore.hyperMode == DNACoreHyperMode.VileMK5) index = 2;
			Global.sprites["menu_vile"].drawToHUD(index, x, sy + 2);
			if (dnaCore.frozenCastle) {
				Fonts.drawText(FontType.DarkBlue, "F", x - 25, sy);
			}
			if (dnaCore.speedDevil) {
				Fonts.drawText(FontType.DarkPurple, "S", x + 20, sy);
			}
		} else if (dnaCore.charNum == 3) {
			Global.sprites["menu_axl"].drawToHUD(dnaCore.hyperMode == DNACoreHyperMode.WhiteAxl ? 1 : 0, x, sy + 4);
		} else if (dnaCore.charNum == 4) {
			Global.sprites["menu_sigma"].drawToHUD(sigmaForm, x, sy + 10);
		}

		sy += 35;

		var weapons = new List<Weapon>();
		for (int i = 0; i < dnaCore.weapons.Count && i < 6; i++) {
			weapons.Add(dnaCore.weapons[i]);
		}
		if (dnaCore.charNum == (int)CharIds.Zero) {
			if (dnaCore.hyperMode == DNACoreHyperMode.NightmareZero) {
				weapons.Add(new DarkHoldWeapon() { ammo = dnaCore.rakuhouhaAmmo });
			} else {
				weapons.Add(RakuhouhaWeapon.getWeaponFromIndex(dnaCore.loadout?.zeroLoadout.gigaAttack ?? 0));
			}
		}
		if (dnaCore.charNum == (int)CharIds.Sigma) {
			if (sigmaForm == 0) weapons.Add(new Weapon() {
				weaponSlotIndex = 111,
				ammo = dnaCore.rakuhouhaAmmo,
				maxAmmo = 20,
			});
			if (sigmaForm == 1) weapons.Add(new Weapon() {
				weaponSlotIndex = 110,
				ammo = dnaCore.rakuhouhaAmmo,
				maxAmmo = 28,
			});
		}
		int counter = 0;
		float wx = 1 + x - ((weapons.Count - 1) * 8);
		foreach (var weapon in weapons) {
			float slotX = wx + (counter * 15);
			float slotY = sy;
			Global.sprites["hud_weapon_icon"].drawToHUD(weapon.weaponSlotIndex, slotX, slotY);
			float ammo = weapon.ammo;
			if (weapon is RakuhouhaWeapon || weapon is RekkohaWeapon || weapon is CFlasher || weapon is DarkHoldWeapon) ammo = dnaCore.rakuhouhaAmmo;
			if (weapon is not MechMenuWeapon) {
				DrawWrappers.DrawRectWH(slotX - 8, slotY - 8, 16, 16 - MathF.Floor(16 * (ammo / weapon.maxAmmo)), true, new Color(0, 0, 0, 128), 1, ZIndex.HUD, false);
			}
			counter++;
		}
	}

	public float getWeaponSlotStartX(Player player, ref int iconW, ref int iconH, ref int width) {
		int weaponCountOff = player.weapons.Count - 1;
		if (mainPlayer.isX && Options.main.gigaCrushSpecial) {
			weaponCountOff = player.weapons.Count((Weapon w) => w is not GigaCrush) - 1;
		}
		int weaponOffset = 0;
		float halfSize = width / 2f;
		if (weaponCountOff + 1 >= 20) {
			width -= weaponCountOff + 1 - 20;
			halfSize = width / 2f;
		}
		if (weaponCountOff > 0) {
			weaponOffset = MathInt.Floor(halfSize * weaponCountOff);
		}

		return Global.halfScreenW - weaponOffset;
	}

	public void drawMaverickCommandIcons() {
		int mwIndex = level.mainPlayer.weapons.IndexOf(level.mainPlayer.weapon);
		float height = 15;
		int width = 20;
		var iconW = 8;
		var iconH = 8;

		float startX = getWeaponSlotStartX(mainPlayer, ref iconW, ref iconH, ref width) + (mwIndex * 20);
		float startY = Global.screenH - 12;

		for (int i = 0; i < MaverickWeapon.maxCommandIndex; i++) {
			float x = startX;
			float y = startY - ((i + 1) * height);
			int index = i;
			Global.sprites["hud_maverick_command"].drawToHUD(index, x, y);
			/*
			if (i == 1)
			{
				Global.sprites["hud_maverick_command"].drawToHUD(3, x - height, y);
				Global.sprites["hud_maverick_command"].drawToHUD(4, x + height, y);
			}
			*/
		}

		for (int i = 0; i < MaverickWeapon.maxCommandIndex + 1; i++) {
			float x = startX;
			float y = startY - (i * height);
			if (level.mainPlayer.maverickWeapon.selCommandIndex == i && level.mainPlayer.maverickWeapon.selCommandIndexX == 1) {
				DrawWrappers.DrawRectWH(x - iconW, y - iconH, iconW * 2, iconH * 2, false, new Color(0, 224, 0), 1, ZIndex.HUD, false);
			}
		}

		/*
		if (level.mainPlayer.maverickWeapon.selCommandIndexX == 0)
		{
			DrawWrappers.DrawRectWH(startX - height - iconW, startY - (height * 2) - iconH, iconW * 2, iconH * 2, false, new Color(0, 224, 0), 1, ZIndex.HUD, false);
		}
		if (level.mainPlayer.maverickWeapon.selCommandIndexX == 2)
		{
			DrawWrappers.DrawRectWH(startX + height - iconW, startY - (height * 2) - iconH, iconW * 2, iconH * 2, false, new Color(0, 224, 0), 1, ZIndex.HUD, false);
		}
		*/
	}

	public void drawRideArmorIcons() {
		int raIndex = mainPlayer.weapons.FindIndex(w => w is MechMenuWeapon);


		float startX = 168;
		if (raIndex == 0) startX = 148;
		if (raIndex == 1) startX = 158;
		if (raIndex == -1) {
			startX = 11;
		}

		float startY = Global.screenH - 12;
		float height = 15;
		Vile vile = level.mainPlayer?.character as Vile;
		bool isMK2 = vile?.isVileMK2 == true;
		bool isMK5 = vile?.isVileMK5 == true;
		bool isMK2Or5 = isMK2 || isMK5;
		int maxIndex = isMK2Or5 ? 5 : 4;

		for (int i = 0; i < maxIndex; i++) {
			float x = startX;
			float y = startY - (i * height);
			int iconIndex = 37 + i;
			if (i == 4 && isMK5) iconIndex = 119;
			Global.sprites["hud_weapon_icon"].drawToHUD(iconIndex, x, y);
		}

		for (int i = 0; i < maxIndex; i++) {
			float x = startX;
			float y = startY - (i * height);
			if (i == 4 && (!isMK2Or5 || level.mainPlayer.currency < 10)) {
				DrawWrappers.DrawRectWH(x - 8, y - 8, 16, 16, true, new Color(0, 0, 0, 128), 1, ZIndex.HUD, false);
			}
		}

		for (int i = 0; i < maxIndex; i++) {
			float x = startX;
			float y = startY - (i * height);
			if (level.mainPlayer.selectedRAIndex == i) {
				DrawWrappers.DrawRectWH(x - 7, y - 7, 14, 14, false, new Color(0, 224, 0), 1, ZIndex.HUD, false);
			}
		}
	}

	public Color getPingColor(Player player) {
		Color pingColor = Helpers.getPingColor(player.getPingOrStartPing(), level.server.netcodeModel == NetcodeModel.FavorAttacker ? level.server.netcodeModelPing : Global.defaultThresholdPing);
		if (pingColor == Color.Green) pingColor = Color.White;
		return pingColor;
	}

	public void drawNetcodeData() {
		int top2 = -3;
		if (!Global.level.server.isP2P) {
			Fonts.drawText(
				FontType.WhiteSmall, Global.level.server.region.name,
				Global.screenW - 12, top2 + 14, Alignment.Right
			);
		} else {
			Fonts.drawText(
				FontType.WhiteSmall, "P2P Server",
				Global.screenW - 12, top2 + 14, Alignment.Right
			);
		}

		string netcodePingStr = "";
		if (level.server.netcodeModel == NetcodeModel.FavorAttacker) {
			netcodePingStr = "<" + level.server.netcodeModelPing.ToString();
		}
		Fonts.drawText(
			FontType.WhiteSmall, netcodePingStr,
			Global.screenW - 12, top2 + 22, Alignment.Right
		);
		Global.sprites["hud_netcode"].drawToHUD((int)level.server.netcodeModel, Global.screenW - 50, top2 + 29);
		if (Global.level.server.isLAN) {
			Fonts.drawText(
				FontType.WhiteSmall, "IP: " + Global.level.server.ip,
				Global.screenW - 12, top2 + 32, Alignment.Right
			);
		}
	}

	public virtual FontType getPingFont(int? ping) {
		if (ping < level.server.netcodeModelPing) return FontType.White;
		return FontType.Red;
	}

	public virtual void drawScoreboard() {
		int padding = 16;
		int top = 16;
		int col1x = padding;
		int col2x = (int)Math.Floor(Global.screenW * 0.33);
		int col3x = (int)Math.Floor(Global.screenW * 0.475);
		int col4x = (int)Math.Floor(Global.screenW * 0.65);
		int col5x = (int)Math.Floor(Global.screenW * 0.85);
		int lineY = padding + 20;
		var labelTextY = 48;
		int line2Y = lineY + 12;
		int topPlayerY = line2Y + 2;
		DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);

		string modeText = this switch {
			MMXOnline.FFADeathMatch => $"FFA (to {playingTo})",
			MMXOnline.Elimination => "Elimination",
			MMXOnline.Race => "Race",
			_ => Global.level.server.gameMode
		};
		Fonts.drawText(FontType.BlueMenu, modeText, padding, 12);
		drawMapName(padding, top + 10);
		if (Global.serverClient != null) {
			Fonts.drawText(
				FontType.OrangeMenu, "Match: " + Global.level.server.name, padding + 100, 12
			);
			drawNetcodeData();
		}

		DrawWrappers.DrawLine(
			padding - 2, labelTextY - 2, Global.screenW - padding + 2, labelTextY - 2, new Color(232, 232, 232, 224), 1, ZIndex.HUD, false
		);
		Fonts.drawText(FontType.OrangeMenu, "Player", col1x + 6, labelTextY, Alignment.Left);
		Fonts.drawText(FontType.OrangeMenu, "Char", col2x + 6, labelTextY, Alignment.Left);
		Fonts.drawText(FontType.OrangeMenu, "Kills", col3x + 6, labelTextY, Alignment.Left);
		Fonts.drawText(FontType.OrangeMenu, this is Elimination ? "Lives" : "Deaths", col4x + 6, labelTextY, Alignment.Left);

		if (Global.serverClient != null) {
			Fonts.drawText(FontType.OrangeMenu, "Ping", col5x + 6, labelTextY, Alignment.Left);
		}
		DrawWrappers.DrawLine(
			padding - 2, labelTextY + 15, Global.screenW - padding + 2, labelTextY + 15, Color.White, 1, ZIndex.HUD, false
		);
		var rowH = 10;
		var players = getOrderedPlayerList();
		if (this is Race race) {
			players = race.getSortedPlayers();
		}
		for (var i = 0; i < players.Count && i <= 14; i++) {
			var player = players[i];
			var color = getCharFont(player);
			var pingColor = getPingFont(player.getPingOrStartPing());

			if (Global.serverClient != null && player.serverPlayer.isHost) {
				Fonts.drawText(
					FontType.Yellow, "H", col1x - 4, labelTextY + 18 + i * rowH
				);
			} else if (Global.serverClient != null && player.serverPlayer.isBot) {
				Fonts.drawText(
					FontType.Grey, "B", col1x - 4, labelTextY + 18 + i * rowH
				);
			}

			Fonts.drawText(color, player.name, col1x + 6, labelTextY + 18 + (i) * rowH, Alignment.Left);
			Fonts.drawText(color, player.kills.ToString(), col3x + 6, labelTextY + 18 + (i) * rowH, Alignment.Left);
			Fonts.drawText(
				color, player.getDeathScore().ToString(),
				col4x + 6, labelTextY + 18 + (i) * rowH, Alignment.Left
			);

			if (Global.serverClient != null) {
				Fonts.drawText(pingColor, player.getDisplayPing(), col5x + 6, labelTextY + 18 + (i) * rowH, Alignment.Left);
			}

			//Global.sprites[getCharIcon(player)].drawToHUD(player.realCharNum, col2x + 4, labelTextY + 18 + i * rowH);
		}
		//drawSpectators();
	}

	public void  drawTeamScoreboard() {
		int padding = 16;
		int top = 16;
		var hPadding = padding + 5;
		var col1x = padding + 5;
		var playerNameX = padding + 15;
		var col2x = col1x - 11;
		var col3x = Global.screenW * 0.28f;
		var col4x = Global.screenW * 0.35f;
		var col5x = Global.screenW * 0.4225f;
		var teamLabelY = padding + 35;
		var lineY = teamLabelY + 10;
		var labelY = lineY + 5;
		var line2Y = labelY + 10;
		var topPlayerY = line2Y + 5;
		var halfwayX = Global.halfScreenW - 2;
		DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);

		string textMode = this switch {
			MMXOnline.CTF => $" CTF (to {playingTo})",
			MMXOnline.TeamDeathMatch => $"Team Deathmatch (to {playingTo})",
			MMXOnline.TeamElimination => "Team Elimination",
			_ => Global.level.server.gameMode
		};
		Fonts.drawText(FontType.BlueMenu, textMode, padding, 12);
		drawMapName(padding, top + 10);

		if (Global.serverClient != null) {
			Fonts.drawText(
				FontType.OrangeMenu, Global.level.server.name, 198, 12
			);
			drawNetcodeData();
		}

		int redPlayersStillAlive = 0;
		int bluePlayersStillAlive = 0;
		if (this is TeamElimination) {
			redPlayersStillAlive = level.players.Where(
				p => !p.isSpectator && p.deaths < playingTo && p.alliance == redAlliance
			).Count();
			bluePlayersStillAlive = level.players.Where(
				p => !p.isSpectator && p.deaths < playingTo && p.alliance == blueAlliance
			).Count();
		}

		//Blue
		string blueText = this switch {
			ControlPoints => "Blue: Attack",
			MMXOnline.KingOfTheHill => "Blue",
			MMXOnline.TeamElimination => $"Alive: {bluePlayersStillAlive}",
			_ => $"Blue: {Global.level.gameMode.teamPoints[0]}"
		};
		string redText = this switch {
			ControlPoints => "Red: Defend",
			MMXOnline.KingOfTheHill => "Red",
			MMXOnline.TeamElimination => $"Alive: {bluePlayersStillAlive}",
			_ => $"Red: {Global.level.gameMode.teamPoints[1]}"
		};
		(int x, int y)[] positions = {
			(4, 46),
			(248, 46),
			(126, 46),
			(4, 130),
			(248, 130),
			(126, 130),
		};
		drawTeamMiniScore(positions[0], 0, FontType.Blue, blueText);
		drawTeamMiniScore(positions[1], 1, FontType.Red, redText);
		for (int i = 2; i < Global.level.teamNum; i++) {
			drawTeamMiniScore(
				positions[i], i,
				teamFonts[i], $"{teamNames[i]}: {Global.level.gameMode.teamPoints[i]}"
			);
		}
		drawSpectators();
	}

	public void drawTeamMiniScore((int x, int y) pos, int alliance, FontType color, string title) {
		int playersStillAlive = 0;
		bool isTE = false;
		if (this is TeamElimination) {
			isTE = true;
			playersStillAlive = level.players.Where(
				p => !p.isSpectator && p.deaths < playingTo && p.alliance == alliance
			).Count();
		}
		int[] rows = new int[] { pos.y, pos.y + 10, pos.y + 24 };
		int[] cols = new int[] { pos.x, pos.x + 72, pos.x + 88, pos.x + 104 };
		DrawWrappers.DrawRect(
			pos.x + 9, pos.y + 19, pos.x + 120, pos.y + 20, true,
			new Color(255, 255, 255, 128), 0, ZIndex.HUD, false
		);

		Fonts.drawText(color, title, cols[0] + 12, rows[0]);
		Fonts.drawText(FontType.Orange, "Player", cols[0] + 12, rows[1]);
		Fonts.drawText(FontType.Orange, "K", cols[1], rows[1]);
		Fonts.drawText(FontType.Orange, isTE ? "L" : "D", cols[2], rows[1]);
		if (Global.serverClient != null) {
			Fonts.drawText(FontType.Orange, "P", cols[3], rows[1]);
		}
		// Player draw
		Player[] players = level.players.Where(p => p.alliance == alliance && !p.isSpectator).ToArray();
		for (var i = 0; i < players.Length && i <= 14; i++) {
			Player player = players[i];
			int posY = rows[2] + i * 10;
			FontType charColor = getCharFont(player);

			if (Global.serverClient != null && player.serverPlayer.isHost) {
				Fonts.drawText(FontType.Yellow, "H", cols[0], posY);
			} else if (Global.serverClient != null && player.serverPlayer.isBot) {
				Fonts.drawText(FontType.Grey, "B", cols[0], posY);
			}

			//Global.sprites[getCharIcon(player)].drawToHUD(player.realCharNum, cols[0] + 5, posY - 2);
			Fonts.drawText(charColor, player.name, cols[0] + 12, posY);
			Fonts.drawText(FontType.Blue, player.kills.ToString(), cols[1], posY);
			Fonts.drawText(FontType.Red, player.getDeathScore().ToString(), cols[2], posY);

			if (Global.serverClient != null) {
				Fonts.drawText(FontType.Grey, player.getTeamDisplayPing(), cols[3], posY);
			}
		}
	}

	private void drawMapName(int x, int y) {
		string displayName = "Map: " + level.levelData.displayName.Replace("_mirrored", "");
		Fonts.drawText(FontType.BlueMenu, displayName, x, 27, Alignment.Left);
		if (level.levelData.isMirrored) {
			int size = Fonts.measureText(Fonts.getFontSrt(FontType.BlueMenu), displayName);
			Global.sprites["hud_mirror_icon"].drawToHUD(0, x + size + 9, y + 5);
		}
	}

	public Color getCharColor(Player player) {
		if (player == level.mainPlayer) return Color.Green;
		return Color.White;
	}

	public float getCharAlpha(Player player) {
		if (player.isDead && !isOver) {
			return 0.5f;
		} else if (player.eliminated()) {
			return 0.5f;
		}
		return 1;
	}

	public FontType getCharFont(Player player) {
		if (player.isDead && !isOver) {
			return FontType.White;
		} else if (player.eliminated()) {
			return FontType.White;
		} else if (player.isRock) {
			return FontType.Blue;
		} else if (player.isBlues) {
			return FontType.Red;
		} else if (player.isBass) {
			return FontType.Yellow;
		}
		return FontType.Grey;
	}

	public string getCharIcon(Player player) {
		return "char_icon";
		//if (isOver) return "char_icon";
		//return player.isDead ? "char_icon_dead" : "char_icon";
	}

	public static string getTeamName(int alliance) {
		return alliance switch {
			0 => "Blue",
			1 => "Red",
			2 => "Green",
			3 => "Purple",
			4 => "Yellow",
			5 => "Orange",
			_ => "Error"
		};
	}

	public Color getTimeColor() {
		if (remainingTime <= 10) {
			return Color.Red;
		}
		return Color.White;
	}

	public void drawTimeIfSet(int yPos) {
		FontType fontColor = FontType.WhiteSmall;
		string timeStr = "";
		if (setupTime > 0) {
			var timespan = new TimeSpan(0, 0, MathInt.Ceiling(setupTime.Value));
			timeStr = timespan.ToString(@"m\:ss");
			fontColor = FontType.OrangeSmall;
		} else if (setupTime == 0 && goTime < 1) {
			goTime += Global.spf;
			timeStr = "GO!";
			fontColor = FontType.RedSmall;
		} else if (remainingTime != null) {
			if (remainingTime <= 10) {
				fontColor = FontType.OrangeSmall;
			}
			var timespan = new TimeSpan(0, 0, MathInt.Ceiling(remainingTime.Value));
			timeStr = timespan.ToString(@"m\:ss");
			if (!level.isNon1v1Elimination() || virusStarted >= 2) {
				timeStr += " Left";
			}
			if (isOvertime()) {
				timeStr = "Overtime!";
				fontColor = FontType.RedSmall;
			}
		} else {
			return;
		}
		Fonts.drawText(fontColor, timeStr, Global.screenW - 8, yPos, Alignment.Right);
	}

	public bool isOvertime() {
		return (this is ControlPoints || this is KingOfTheHill || this is CTF) && remainingTime != null && remainingTime.Value == 0 && !isOver;
	}

	public void drawDpsIfSet(int yPos) {
		if (!string.IsNullOrEmpty(dpsString)) {
			Fonts.drawText(
				FontType.BlueMenu, dpsString, 5, yPos
			);
		}
	}

	public void drawVirusTime(int yPos) {
		var timespan = new TimeSpan(0, 0, MathInt.Ceiling(remainingTime ?? 0));
		string timeStr = "Roboenza: " + timespan.ToString(@"m\:ss");
		Fonts.drawText(FontType.PurpleSmall, timeStr, Global.screenW - 8, yPos, Alignment.Right);
	}

	public void drawWinScreen() {
		string text = "";
		string subtitle = "";

		if (playerWon(level.mainPlayer)) {
			text = matchOverResponse.winMessage;
			subtitle = matchOverResponse.winMessage2;
		} else {
			text = matchOverResponse.loseMessage;
			subtitle = matchOverResponse.loseMessage2;
		}

		// Title
		float titleY = Global.halfScreenH;
		// Subtitle
		float subtitleY = titleY + 16;
		// Offsets.
		float hh = 8;
		float hh2 = 16;
		if (string.IsNullOrEmpty(subtitle)) {
			subtitleY = titleY;
		}
		int offset = MathInt.Floor(((subtitleY + hh2) - (titleY - hh)) / 2);
		titleY -= offset;
		subtitleY -= offset;

		// Box
		DrawWrappers.DrawRect(
			0, titleY - hh,
			Global.screenW, subtitleY + hh2,
			true, new Color(0, 0, 0, 192), 1, ZIndex.HUD,
			isWorldPos: false, outlineColor: Color.White
		);

		// Title
		Fonts.drawText(
			FontType.Grey, text.ToUpperInvariant(),
			Global.halfScreenW, titleY, Alignment.Center
		);

		// Subtitle
		Fonts.drawText(
			FontType.Grey, subtitle,
			Global.halfScreenW, subtitleY, Alignment.Center
		);

		if (overTime >= secondsBeforeLeave) {
			if (Global.serverClient == null) {
				Fonts.drawText(
					FontType.OrangeMenu, Helpers.controlText("Press [ESC] to return to menu"),
					Global.halfScreenW, subtitleY + hh2 + 16, Alignment.Center
				);
			}
		}
	}

	public virtual void drawTopHUD() {

	}

	public void drawRespawnHUD() {
		if (level.mainPlayer.character != null && level.mainPlayer.readyTextOver && level.mainPlayer.canReviveBlues()) {
			Fonts.drawTextEX(
				FontType.Red, Helpers.controlText(
						$"[CMD]: Revive as Break Man ({Blues.reviveCost} {Global.nameCoins})"
					),
				Global.screenW / 2, 10 + Global.screenH / 2, Alignment.Center
			);
		}

		if (level.mainPlayer.randomTip == null) return;
		if (level.mainPlayer.isSpectator) return;

		if (level.mainPlayer.character == null && level.mainPlayer.readyTextOver) {
			string respawnStr = (
				(level.mainPlayer.respawnTime > 0) ?
				"Respawn in " + Math.Round(level.mainPlayer.respawnTime).ToString() :
				Helpers.controlText("Press [OK] to respawn")
			);

			if (level.mainPlayer.eliminated()) {
				Fonts.drawText(
					FontType.Red, "You were eliminated!",
					Global.screenW / 2, -15 + Global.screenH / 2, Alignment.Center
				);
				Fonts.drawText(
					FontType.BlueMenu, "Spectating in " + Math.Round(level.mainPlayer.respawnTime).ToString(),
					Global.screenW / 2, Global.screenH / 2, Alignment.Center
				);
			} else if (level.mainPlayer.canReviveVile()) {
				if (level.mainPlayer.lastDeathWasVileMK2) {
					Fonts.drawText(
						FontType.BlueMenu, respawnStr,
						Global.screenW / 2, -10 + Global.screenH / 2, Alignment.Center
					);
					string reviveText = Helpers.controlText(
						$"[SPC]: Revive as Vile V (5 {Global.nameCoins})"
					);
					Fonts.drawText(
						FontType.Green, reviveText,
						Global.screenW / 2, 10 + Global.screenH / 2, Alignment.Center
					);
				} else {
					Fonts.drawText(
						FontType.BlueMenu, respawnStr,
						Global.screenW / 2, -10 + Global.screenH / 2, Alignment.Center
					);
					string reviveText = Helpers.controlText(
						$"[SPC]: Revive as MK-II (5 {Global.nameCoins})"
					);
					Fonts.drawText(
						FontType.DarkBlue, reviveText,
						Global.screenW / 2, 10 + Global.screenH / 2, Alignment.Center
					);
					string reviveText2 = Helpers.controlText(
						$"[CMD]: Revive as Vile V (5 {Global.nameCoins})"
					);
					Fonts.drawText(
						FontType.Green, reviveText2,
						Global.screenW / 2, 22 + Global.screenH / 2, Alignment.Center
					);
				}
			} else if (level.mainPlayer.canReviveSigma(out _)) {
				Fonts.drawText(
					FontType.BlueMenu, respawnStr,
					Global.screenW / 2, -10 + Global.screenH / 2, Alignment.Center
				);
				string hyperType = "Kaiser";
				string reviveText = (
					$"[CMD]: Revive as {hyperType} Sigma ({Player.reviveSigmaCost.ToString()} {Global.nameCoins})"
				);
				Fonts.drawTextEX(
					FontType.Green, reviveText,
					Global.screenW / 2, 10 + Global.screenH / 2, Alignment.Center
				);
			} else {
				Fonts.drawText(
					FontType.BlueMenu, respawnStr,
					Global.screenW / 2, Global.screenH / 2, Alignment.Center
				);
			}

			if (!Menu.inMenu) {
				DrawWrappers.DrawRect(0, Global.halfScreenH + 40, Global.screenW, Global.halfScreenH + 40 + (14 * level.mainPlayer.randomTip.Length), true, new Color(0, 0, 0, 224), 0, ZIndex.HUD, false);
				for (int i = 0; i < level.mainPlayer.randomTip.Length; i++) {
					var line = level.mainPlayer.randomTip[i];
					if (i == 0) line = "Tip: " + line;
					Fonts.drawText(
						FontType.WhiteSmall, line,
						Global.screenW / 2, (Global.screenH / 2) + 45 + (12 * i), Alignment.Center
					);
				}
			}
		}
	}

	public bool playerWon(Player player) {
		if (!isOver) return false;
		if (matchOverResponse.winningAlliances == null) return false;
		return matchOverResponse.winningAlliances.Contains(player.alliance);
	}

	public void onMatchOver() {
		if (level.mainPlayer != null && playerWon(level.mainPlayer)) {
			Global.changeMusic(Global.level.levelData.getWinTheme());
		} else if (level.mainPlayer != null && !playerWon(level.mainPlayer)) {
			Global.changeMusic(Global.level.levelData.getLooseTheme());
		}
		if (Menu.inMenu) {
			Menu.exit();
		}
		logStats();
	}

	public void matchOverRpc(RPCMatchOverResponse matchOverResponse) {
		if (this.matchOverResponse == null) {
			this.matchOverResponse = matchOverResponse;
			onMatchOver();
		}
	}

	public void logStats() {
		if (loggedStatsOnce) return;
		loggedStatsOnce = true;

		if (Global.serverClient == null) {
			return;
		}
		if (level.isTraining()) {
			return;
		}
		bool is1v1 = level.is1v1();
		var nonSpecPlayers = Global.level.nonSpecPlayers();
		int botCount = nonSpecPlayers.Count(p => p.isBot);
		int nonBotCount = nonSpecPlayers.Count(p => !p.isBot);
		if (botCount >= nonBotCount) return;
		Player mainPlayer = level.mainPlayer;
		string mainPlayerCharName = getLoggingCharNum(mainPlayer, is1v1);

		if (this is FFADeathMatch && !mainPlayer.isSpectator && isFairDeathmatch(mainPlayer)) {
			long val = playerWon(mainPlayer) ? 100 : 0;
			Logger.logEvent(
				"dm_win_rate", mainPlayerCharName, val, forceLog: true
			);
			Logger.logEvent(
				"dm_unique_win_rate_" + mainPlayerCharName,
				Global.deviceId + "_" + mainPlayer.name, val, forceLog: true
			);
		}

		if (is1v1 && !mainPlayer.isSpectator && !isMirrorMatchup()) {
			long val = playerWon(mainPlayer) ? 100 : 0;
			Logger.logEvent("1v1_win_rate", mainPlayerCharName, val, forceLog: true);
		}

		if (!is1v1 && (mainPlayer.kills > 0 || mainPlayer.deaths > 0 || mainPlayer.assists > 0)) {
			Logger.logEvent("kill_stats_v2", mainPlayerCharName + ":kills", mainPlayer.kills, forceLog: true);
			Logger.logEvent("kill_stats_v2", mainPlayerCharName + ":deaths", mainPlayer.deaths, forceLog: true);
			Logger.logEvent("kill_stats_v2", mainPlayerCharName + ":assists", mainPlayer.assists, forceLog: true);
		}

		if (!is1v1 && Global.isHost) {
			RPC.logWeaponKills.sendRpc();
			if (isTeamMode && !level.levelData.isMirrored && (this is CTF || this is ControlPoints)) {
				long val;
				if (matchOverResponse.winningAlliances.Contains(blueAlliance)) val = 100;
				else if (matchOverResponse.winningAlliances.Contains(redAlliance)) val = 0;
				else {
					Logger.logEvent(
						"map_stalemate_rates",
						level.levelData.name + ":" + level.server.gameMode,
						100, false, true
					);
					return;
				}
				Logger.logEvent(
					"map_win_rates",
					level.levelData.name + ":" + level.server.gameMode,
					val, false, true
				);
				Logger.logEvent(
					"map_stalemate_rates", level.levelData.name + ":" + level.server.gameMode, 0, false, true
				);
			}
		}
	}

	public bool isMirrorMatchup() {
		var nonSpecPlayers = Global.level.nonSpecPlayers();
		if (nonSpecPlayers.Count != 2) return false;
		if (nonSpecPlayers[0].charNum != nonSpecPlayers[1].charNum) {
			return true;
		} else {
			if (nonSpecPlayers[0].charNum == 0 && nonSpecPlayers[0].armorFlag != nonSpecPlayers[1].armorFlag) {
				return true;
			}
			return false;
		}
	}

	public bool isFairDeathmatch(Player mainPlayer) {
		int kills = mainPlayer.charNumToKills.GetValueOrDefault(mainPlayer.realCharNum);
		if (kills < mainPlayer.kills / 2) return false;
		if (kills < 10) return false;
		return true;
	}

	public string getLoggingCharNum(Player player, bool is1v1) {
		int charNum = player.realCharNum;
		string charName;
		if (charNum == 0) {
			charName = "X";
			if (is1v1) {
				if (player.legArmorNum == 1) charName += "1";
				else if (player.legArmorNum == 2) charName += "2";
				else if (player.legArmorNum == 3) charName += "3";
			}
		} else if (charNum == 1) charName = "Zero";
		else if (charNum == 2) charName = "Vile";
		else if (charNum == 3) {
			if (Options.main.axlAimMode == 2) charName = "AxlCursor";
			else if (Options.main.axlAimMode == 1) charName = "AxlAngular";
			else charName = "AxlDirectional";
		} else if (charNum == 4) {
			if (Options.main.sigmaLoadout.commandMode == 0) charName = "SigmaSummoner";
			else if (Options.main.sigmaLoadout.commandMode == 1) charName = "SigmaPuppeteer";
			else if (Options.main.sigmaLoadout.commandMode == 2) charName = "SigmaStriker";
			else charName = "SigmaTagTeam";
		} else charName = null;

		return charName;
	}

	public void drawTeamTopHUD() {
		int teamSide = Global.level.mainPlayer.teamAlliance ?? -1;
		if (teamSide < 0 || Global.level.mainPlayer.isSpectator) {
			drawAllTeamsHUD();
			return;
		}
		int maxTeams = Global.level.teamNum;

		string teamText = $"{teamNames[teamSide]}: {teamPoints[teamSide].ToString().PadLeft(2 ,' ')}";
		Fonts.drawText(
			teamFonts[teamSide], teamText,
			Global.screenW - 56, 17, Alignment.Right
		);

		int leaderTeam = 0;
		int leaderScore = -1;
		bool moreThanOneLeader = false;
		for (int i = 0; i < Global.level.teamNum; i++) {
			if (teamPoints[0] >= leaderScore) {
				leaderTeam = i;
				if (leaderScore == teamPoints[i]) {
					moreThanOneLeader = true;
				}
				leaderScore = teamPoints[i];
			}
		}
		if (!moreThanOneLeader) {
			Fonts.drawText(
				teamFonts[leaderTeam], $"Leader: {leaderScore.ToString().PadLeft(2 ,' ')}",
				Global.screenW - 56, 7, Alignment.Right
			);
		} else {
			Fonts.drawText(
				FontType.WhiteSmall, $"Leader:{leaderScore.ToString().PadLeft(2 ,' ')}",
				Global.screenW - 56, 7, Alignment.Right
			);
		}
		drawTimeIfSet(37);
	}

	public void drawAllTeamsHUD() {
		for (int i = 0; i < Global.level.teamNum; i++) {
			Fonts.drawText(teamFonts[i], $"{teamNames[i]}: {teamPoints[i]}", 5, 5 + i * 10);
		}
		drawTimeIfSet(5 + 10 * (Global.level.teamNum + 1));
	}

	public void drawObjectiveNavpoint(string label, Point objPos) {
		if (level.mainPlayer.character == null) return;
		navPoints.Add((objPos, label));

		/*
		if (!string.IsNullOrEmpty(label)) label += ":";

		Point playerPos = level.mainPlayer.character.pos;

		var line = new Line(playerPos, objPos);
		var camRect = new Rect(
			level.camX, level.camY, level.camX + Global.viewScreenW, level.camY + Global.viewScreenH
		);

		var intersectionPoints = camRect.getShape().getLineIntersectCollisions(line);
		if (intersectionPoints.Count > 0 && intersectionPoints[0].hitData?.hitPoint != null) {
			Point intersectPoint = intersectionPoints[0].hitData.hitPoint.GetValueOrDefault();
			var dirTo = playerPos.directionTo(objPos).normalize();

			//a = arrow, l = length, m = minus
			float al = 10 / Global.viewSize;
			float alm1 = 9 / Global.viewSize;
			float alm2 = 8 / Global.viewSize;
			float alm3 = 7 / Global.viewSize;
			float alm4 = 5 / Global.viewSize;

			intersectPoint.inc(dirTo.times(-10));
			var posX = intersectPoint.x - Global.level.camX;
			var posY = intersectPoint.y - Global.level.camY;

			posX /= Global.viewSize;
			posY /= Global.viewSize;

			DrawWrappers.DrawLine(
				posX, posY,
				posX + dirTo.x * al, posY + dirTo.y * al,
				Helpers.getAllianceColor(), 1, ZIndex.HUD, false
			);
			DrawWrappers.DrawLine(
				posX + dirTo.x * alm4, posY + dirTo.y * alm4,
				posX + dirTo.x * alm3, posY + dirTo.y * alm3,
				Helpers.getAllianceColor(), 4, ZIndex.HUD, false
			);
			DrawWrappers.DrawLine(
				posX + dirTo.x * alm3, posY + dirTo.y * alm3,
				posX + dirTo.x * alm2, posY + dirTo.y * alm2,
				Helpers.getAllianceColor(), 3, ZIndex.HUD, false
			);
			DrawWrappers.DrawLine(
				posX + dirTo.x * alm2, posY + dirTo.y * alm2,
				posX + dirTo.x * alm1, posY + dirTo.y * alm1,
				Helpers.getAllianceColor(), 2, ZIndex.HUD, false
			);

			float distInMeters = objPos.distanceTo(playerPos) * 0.044f;
			bool isLeft = posX < Global.viewScreenW / 2;
			Fonts.drawText(
				FontType.WhiteSmall, label + MathF.Round(distInMeters).ToString() + "m",
				posX, posY, isLeft ? Alignment.Left : Alignment.Right
			);
		}
		*/
	}

	public void syncTeamScores() {
		if (Global.isHost) {
			Global.serverClient?.rpc(RPC.syncTeamScores, Global.level.gameMode.teamPoints);
		}
	}
}
