using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;

public class TeamElimination : GameMode {
	public TeamElimination(Level level, int playingTo, int? timeLimit) : base(level, timeLimit) {
		this.playingTo = playingTo;
		isTeamMode = true;
		if (remainingTime == null) {
			remainingTime = 300;
			startTimeLimit = remainingTime;
		}
	}
	public override void drawTopHUD() {
		if (level.is1v1()) {
			draw1v1TopHUD();
			return;
		}

		var fontColor = level.mainPlayer.alliance == GameMode.blueAlliance ? FontType.BlueMenu : FontType.RedMenu;
		var redPlayersStillAlive = level.players.Where(p => !p.isSpectator && p.deaths < playingTo && p.alliance == redAlliance).ToList();
		var bluePlayersStillAlive = level.players.Where(p => !p.isSpectator && p.deaths < playingTo && p.alliance == blueAlliance).ToList();
		int lives = playingTo - level.mainPlayer.deaths;
		Fonts.drawText(fontColor, "Lives: " + lives.ToString(), 5, 5);
		//Fonts.drawText(fontColor, "Alive: ", 5, 17);
		//Fonts.drawText(FontType.RedMenu,  (redPlayersStillAlive.Count).ToString(), 62, 17);
		//Fonts.drawText(FontType.BlueMenu, "/" + (bluePlayersStillAlive.Count).ToString(), 70, 17);

		if (virusStarted != 1) {
			drawTimeIfSet(25);
		} else {
			drawVirusTime(25);
		}
	}

	public override void checkIfWinLogic() {
		if (level.time < 15) return;

		var redPlayersStillAlive = level.players.Where(p => !p.isSpectator && p.deaths < playingTo && p.alliance == redAlliance).ToList();
		var bluePlayersStillAlive = level.players.Where(p => !p.isSpectator && p.deaths < playingTo && p.alliance == blueAlliance).ToList();

		if (redPlayersStillAlive.Count > 0 && bluePlayersStillAlive.Count == 0) {
			matchOverResponse = new RPCMatchOverResponse() {
				winningAlliances = new HashSet<int>() { redAlliance },
				winMessage = "Victory!",
				winMessage2 = "Red team wins",
				loseMessage = "You lost!",
				loseMessage2 = "Red team wins"
			};
		} else if (bluePlayersStillAlive.Count > 0 && redPlayersStillAlive.Count == 0) {
			matchOverResponse = new RPCMatchOverResponse() {
				winningAlliances = new HashSet<int>() { blueAlliance },
				winMessage = "Victory!",
				winMessage2 = "Blue team wins",
				loseMessage = "You lost!",
				loseMessage2 = "Blue team wins"
			};
		} else if (remainingTime <= 0 && virusStarted >= 3) {
			matchOverResponse = new RPCMatchOverResponse() {
				winningAlliances = new HashSet<int>() { },
				winMessage = "Stalemate!",
				loseMessage = "Stalemate!"
			};
		}
	}

	public override void drawScoreboard() {
		base.drawTeamScoreboard();
	}
}
