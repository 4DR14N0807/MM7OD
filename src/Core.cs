using System;
using System.Collections.Generic;

namespace MMXOnline;

// TODO: Finish this.
// Core manager, this allows to control multiple levels.
// Also splits things like gamemode and server from level.
public class Core {
	public List<Level> levels = [];
	public Level activeLevel;
	public HUD hud;

	public Core(List<Level> levels) {
		// Populate level list.
		this.levels = levels;
		activeLevel = levels[0];
		hud = new HUD(activeLevel);


		// Sync hud and gamemodes.
		foreach (Level level in levels) {
			level.hud = hud;
			level.gameMode = levels[0].gameMode;
		}
	}

	public void update() {
		
	}

	public void render() {
		
	}
}