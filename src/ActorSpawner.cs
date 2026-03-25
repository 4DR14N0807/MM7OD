using System;

namespace MMXOnline;

public class ActorSpawner {
	public Point pos;
	public float respawnTime = 60 * 10;
	public float time;
	public Actor currentActor;
	public int teamSide;
	public int xDir;

	public ActorSpawner(Point pos, int xDir, int teamSide) {
		this.pos = pos;
		this.xDir = xDir;
		this.teamSide = teamSide;

		if (teamSide < 0) {
			teamSide = GameMode.neutralAlliance;
		}
		time = 2;
	}
	public void update() {
		if (!Global.isHost || currentActor?.destroyed == false) {
			time = respawnTime;
			return;
		}

		if (time > 0) {
			time -= Global.gameSpeed;
			return;
		}
		time = respawnTime;

		currentActor = new Met(
			pos, xDir, Global.level.mainPlayer,
			Global.level.mainPlayer.getNextActorNetId(),
			sendRpc: true, alliance: teamSide
		);
	}
}
