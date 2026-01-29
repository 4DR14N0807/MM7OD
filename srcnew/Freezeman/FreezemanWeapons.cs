using System;

namespace MMXOnline;

public class FreezemanWeapon : Weapon {
	public static FreezemanWeapon netWeapon = new();

	public FreezemanWeapon() : base() {
		displayName = "Freeze Cracker";
		weaponBarBaseIndex = 0;
		weaponBarIndex = (int)RockWeaponBarIds.FreezeCracker;
		maxAmmo = 28;
		ammo = maxAmmo;
	}
}

