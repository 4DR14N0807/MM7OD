using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MMXOnline;

namespace MMXOnline;

public class RockLoadoutSetup {
	public static List<Weapon> getLoadout(RockLoadout loadout) {
		List<Weapon> weapons = new();
		// 1v1/Training loadout.
		if (Global.level.isTraining() && !Global.level.server.useLoadout || Global.level.is1v1()) {
			weapons = Weapon.getAllRockWeapons();
		}
		// Regular Loadout.
		else {
			weapons.Add(getWeaponById(loadout.weapon1));
			weapons.Add(getWeaponById(loadout.weapon2));
			weapons.Add(getWeaponById(loadout.weapon3));
		}
		return weapons;
	}

	public static Weapon getWeaponById(int id) {
		return id switch {
			0 => new RockBuster(),
			1 => new FreezeCracker(),
			2 => new ThunderBolt(),
			3 => new JunkShield(),
			4 => new ScorchWheel(),
			5 => new SlashClawWeapon(),
			6 => new NoiseCrush(),
			7 => new DangerWrap(),
			8 => new WildCoil(),
			_ => new RockBuster()
		};
	}
}
