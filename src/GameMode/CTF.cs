namespace MMXOnline;

public class CTF : GameMode {
	int neutralKillLimit;

	public CTF(
		Level level, int playingTo,
		int? timeLimit, int neutralKillLimit = 100
	) : base(level, timeLimit) {
		this.playingTo = playingTo;
		this.neutralKillLimit = neutralKillLimit;
		isTeamMode = true;
	}

	public override void render() {
		base.render();
		if (level.mainPlayer.character == null) return;
		bool isBlue = level.mainPlayer.alliance == GameMode.blueAlliance;
		bool isRed = level.mainPlayer.alliance == GameMode.redAlliance;

		if (isBlue && level.redFlag.linkedChar == level.mainPlayer.character) {
			addMapNavpoint("Ped", level.blueFlag.pedestal.pos);
		}
		else if (level.redFlag.linkedChar == null || Global.level.frameCount % 10 < 6) {
			addMapNavpoint("RFlag", level.redFlag.pos);
		}
		if (isRed && level.blueFlag.linkedChar == level.mainPlayer.character) {
			addMapNavpoint("Ped", level.redFlag.pedestal.pos);
		}
		else if (level.blueFlag.linkedChar == null || Global.level.frameCount % 10 < 6) {
			addMapNavpoint("BFlag", level.blueFlag.pos);
		}

		if (!Options.main.oldNavPoints) { return; }
		if (level.mainPlayer.alliance > redAlliance) { return; }
		drawObjectiveNavpoint(
			"Capture",
			level.mainPlayer.alliance == redAlliance ? level.blueFlag.pos : level.redFlag.pos
		);
		if (level.mainPlayer.character?.flag != null) {
			drawObjectiveNavpoint(
				"Return",
				level.mainPlayer.alliance == redAlliance ?
				level.redFlag.pedestal.pos : level.blueFlag.pedestal.pos
			);
		} else {
			drawObjectiveNavpoint(
				"Defend", level.mainPlayer.alliance == redAlliance ?
				level.redFlag.pos : level.blueFlag.pos
			);
		}
	}

	public override void drawTopHUD() {
		drawTeamTopHUD();
	}

	public override void checkIfWinLogic() {
		checkIfWinLogicTeams();
	}

	public override void drawScoreboard() {
		drawTeamScoreboard();
	}
}
