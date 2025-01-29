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
		/*Player[] enemyPlayersAlive = level.players.Where(
			p => !p.isSpectator && p.deaths < playingTo && p.alliance != Global.level.mainPlayer.alliance
		).ToArray();*/
		Player[] allyPlayersAlive = level.players.Where(
			p => !p.isSpectator && p.deaths < playingTo && p.alliance == Global.level.mainPlayer.alliance
		).ToArray();
		FontType fontColor = teamFonts[mainPlayer.alliance];
		int lives = playingTo - level.mainPlayer.deaths;
		string topText = "Allies:" + (allyPlayersAlive.Length).ToString().PadLeft(2 ,' ');
		string botText = "Lives:" + lives.ToString().PadLeft(2 ,' ');
		Fonts.drawText(fontColor, topText,  Global.screenW - 56, 7, Alignment.Right);
		Fonts.drawText(FontType.WhiteSmall, botText,  Global.screenW - 56, 17, Alignment.Right);

		if (virusStarted != 1) {
			drawTimeIfSet(37);
		} else {
			drawVirusTime(37);
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
