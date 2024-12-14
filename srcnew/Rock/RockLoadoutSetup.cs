using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MMXOnline;

namespace MMXOnline;

public class RockLoadoutSetup {
	public static List<Weapon> getLoadout(Player player) {
		List<Weapon> weapons = new();
		// 1v1/Training loadout.
		if (Global.level.isTraining() && !Global.level.server.useLoadout || Global.level.is1v1()) {
			weapons = Weapon.getAllRockWeapons();
		}
		// Regular Loadout.
		else {
			weapons = player.loadout.rockLoadout.getWeaponsFromLoadout(player);
		}

		return weapons;
	}
}
