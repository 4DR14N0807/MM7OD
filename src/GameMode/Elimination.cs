using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;

public class Elimination : GameMode {
	public Elimination(Level level, int lives, int? timeLimit) : base(level, timeLimit) {
		playingTo = lives;
		if (remainingTime == null && !level.is1v1()) {
			remainingTime = 300;
			startTimeLimit = remainingTime;
		}
	}

	public override void render() {
		base.render();
	}

	public override void checkIfWinLogic() {
		if (level.time < 10) return;

		Player? winningPlayer = null;

		var playersStillAlive = level.players.Where(p => !p.isSpectator && p.deaths < playingTo).ToList();

		if (playersStillAlive.Count == 1) {
			winningPlayer = playersStillAlive[0];
		}

		if (remainingTime <= 0 && (virusStarted >= 3 || level.is1v1()) && winningPlayer == null) {
			matchOverResponse = new RPCMatchOverResponse() {
				winningAlliances = new HashSet<int>() { },
				winMessage = "Stalemate!",
				loseMessage = "Stalemate!"
			};
		} else if (winningPlayer != null) {
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
		Player[] playersStillAlive = level.players.Where(p => !p.isSpectator && p.deaths < playingTo).ToArray();
		int lives = playingTo - level.mainPlayer.deaths;
		string topText = "Lives:" + lives.ToString().PadLeft(2 ,' ');
		string botText = "Alive:" + (playersStillAlive.Length).ToString().PadLeft(2 ,' ');
		float mapOffset = shouldDrawRadar() ? 0 : 48;
		Fonts.drawText(FontType.WhiteSmall, botText,  Global.screenW - 56 + mapOffset, 7, Alignment.Right);
		Fonts.drawText(FontType.WhiteSmall, topText,  Global.screenW - 56 + mapOffset, 17, Alignment.Right);

		if (virusStarted != 1) {
			drawTimeIfSet(37);
		} else {
			drawVirusTime(37);
		}
	}

	public override void drawScoreboard() {
		base.drawScoreboard();
	}
}
