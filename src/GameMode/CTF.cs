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
		drawObjectiveNavpoint("RFlag", level.redFlag.pos);
		drawObjectiveNavpoint("BFlag", level.blueFlag.pos);

		if (level.mainPlayer.character?.flag != null && level.blueFlag.chr != null) {
			drawObjectiveNavpoint("Ped", level.redFlag.pos);
		};
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
