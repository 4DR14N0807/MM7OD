using System;

namespace MMXOnline;

public class LightningBolt : Weapon {
	public LightningBolt() : base() {
		index = (int)BassWeaponIds.LightningBolt;
		displayName = "LIGHTNING BOLT";
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
