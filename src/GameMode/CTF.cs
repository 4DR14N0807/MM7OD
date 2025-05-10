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

		if (isBlue && level.redFlag.chr == level.mainPlayer.character) {
			addMapNavpoint("Ped", level.blueFlag.pedestal.pos);
		}
		else if (level.redFlag.chr == null || Global.level.frameCount % 10 < 6) {
			addMapNavpoint("RFlag", level.redFlag.pos);
		}
		if (isRed && level.blueFlag.chr == level.mainPlayer.character) {
			addMapNavpoint("Ped", level.redFlag.pedestal.pos);
		}
		else if (level.blueFlag.chr == null || Global.level.frameCount % 10 < 6) {
			addMapNavpoint("BFlag", level.blueFlag.pos);
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
