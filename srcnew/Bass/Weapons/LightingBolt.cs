using System;

namespace MMXOnline;

public class LightingBolt : Weapon {
	public LightingBolt() : base() {
		index = (int)BassWeaponIds.LightingBolt;
		weaponSlotIndex = index;
		weaponBarBaseIndex = index;
		weaponBarIndex = index;
		fireRateFrames = 180;
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Point shootPos = character.getShootPos();
		Player player = character.player;

	}
}
