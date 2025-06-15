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
		Player winningPlayer = null;

		if (remainingTime <= 0) {
			winningPlayer = level.players[0];
		} else {
			foreach (var player in level.players) {
				if (player.kills >= playingTo) {
					winningPlayer = player;
					break;
				}
			}
		}

		if (winningPlayer != null) {
			string winMessage = "You won!";
			string loseMessage = "You lost!";
			string loseMessage2 = winningPlayer.name + " wins";

			matchOverResponse = new RPCMatchOverResponse() {
				winningAlliances = new HashSet<int>() { winningPlayer.alliance },
				winMessage = winMessage,
				loseMessage = loseMessage,
				loseMessage2 = loseMessage2
			};
		}
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
