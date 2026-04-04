using System.Collections.Generic;

namespace MMXOnline;

public class FFADeathMatch : GameMode {
	public FFADeathMatch(Level level, int killsToWin, int? timeLimit) : base(level, timeLimit) {
		playingTo = killsToWin;
	}

	public override void render() {
		base.render();
	}

	public override void checkIfWinLogic() {
		List<Player> winningPlayers = [];

		if (remainingTime <= 0) {
			int maxKills = 0;
			foreach (Player player in level.players) {
				if (player.kills >= maxKills) {
					maxKills = player.kills;
				}
			}
			// Get everyone with max kills.
			// This will be a draw if more than one.
			if (maxKills != 0) {
				foreach (var player in level.players) {
					if (player.kills >= playingTo) {
						winningPlayers.Add(player);
						break;
					}
				}
			} else {
				// Make the stage win to show lose message to everyone.
				winningPlayers.Add(Player.stagePlayer);
			}
		} else {
			foreach (Player player in level.players) {
				if (player.kills >= playingTo) {
					winningPlayers.Add(player);
					break;
				}
			}
		}

		if (winningPlayers.Count == 0) {
			return;
		}
		if (winningPlayers.Count == 1) {
			string winMessage = "You won!";
			string loseMessage = "You lost!";
			string loseMessage2 = winningPlayers[0].name + " wins";

			matchOverResponse = new RPCMatchOverResponse() {
				winningAlliances = [winningPlayers[0].alliance],
				winMessage = winMessage,
				loseMessage = loseMessage,
				loseMessage2 = loseMessage2
			};
		}
		string drawMessage = remainingTime <= 0 ? "Stalemate!" : "Draw!";
		matchOverResponse = new RPCMatchOverResponse() {
			winningAlliances = [nullAlliance],
			winMessage = drawMessage,
			loseMessage = drawMessage,
		};
	}

	public override void drawTopHUD() {
		string placeStr = "";
		List<Player> playerList = GameMode.getOrderedPlayerList();
		int place = playerList.IndexOf(level.mainPlayer) + 1;
		placeStr = Helpers.getNthString(place);
		string topText = "Leader: 0";
		if (playerList.Count > 0) {
			topText = "Leader:" + playerList[0].kills.ToString().PadLeft(2 ,' ');
		}
		string botText = "Kills:" + level.mainPlayer.kills.ToString().PadLeft(2 ,' ');
		float mapOffset = shouldDrawRadar() ? 0 : 48;
		Fonts.drawText(FontType.WhiteSmall, botText, Global.screenW - 56 + mapOffset, 7, Alignment.Right);
		Fonts.drawText(FontType.WhiteSmall, topText, Global.screenW - 56 + mapOffset, 17, Alignment.Right);

		drawTimeIfSet(37);
	}

	public override void drawScoreboard() {
		base.drawScoreboard();
	}
}
