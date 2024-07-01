using System;

namespace MMXOnline;

public class WaterWave : Weapon {
	public static GyroAttack netWeapon = new();

	public WaterWave() : base() {
		displayName = "Water Wave";
		descriptionV2 = "A weapon that uses high pressure to\ninject compressed water in one direction.";
		defaultAmmoUse = 4;

		index = (int)RockWeaponIds.WaterWave;
		fireRateFrames = 50;
	}

	public override float getAmmoUsage(int chargeLevel) {
		return defaultAmmoUse;
	}
}
