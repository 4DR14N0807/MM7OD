using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MMXOnline;
using ProtoBuf;

namespace MMXOnline;

[ProtoContract]
public class RockLoadout {
	[ProtoMember(1)] public int weapon1;    //0 indexed
	[ProtoMember(2)] public int weapon2;
	[ProtoMember(3)] public int weapon3;
	[ProtoMember(4)] public int rushLoadout;

	public List<int> getRockWeaponIndices() {
		return new List<int>() { weapon1, weapon2, weapon3, rushLoadout };
	}

	public void validate() {
		if (weapon1 < 0 || weapon1 > 9) weapon1 = 0;
		if (weapon2 < 0 || weapon2 > 9) weapon2 = 0;
		if (weapon3 < 0 || weapon3 > 9) weapon3 = 0;

		if ((weapon1 == weapon2 && weapon1 >= 0) ||
			(weapon1 == weapon3 && weapon2 >= 0) ||
			(weapon2 == weapon3 && weapon3 >= 0)) {
			weapon1 = 0;
			weapon2 = 1;
			weapon3 = 2;

			if (rushLoadout < 0 || rushLoadout > 2) rushLoadout = 0;
		}
	}

	public Weapon getRushFromLoadout(Player player) {
		var indices = (byte)rushLoadout;
		var rushW = Rock.getAllRushWeapons();
		
		return rushW[indices];
	}

	public static RockLoadout createRandom() {
		List<int> weapons = [ 0, 1, 2, 3, 4, 5, 6, 7, 8 ];

		RockLoadout loadout = new();
		int targetWeapon = Helpers.randomRange(0, weapons.Count - 1);
		loadout.weapon1 = weapons[targetWeapon];
		weapons.RemoveAt(targetWeapon);

		targetWeapon = Helpers.randomRange(0, weapons.Count - 1);
		loadout.weapon2 = weapons[targetWeapon];
		weapons.RemoveAt(targetWeapon);

		targetWeapon = Helpers.randomRange(0, weapons.Count - 1);
		loadout.weapon3 = weapons[targetWeapon];
		weapons.RemoveAt(targetWeapon);

		loadout.validate();
		return loadout;
	}
}
