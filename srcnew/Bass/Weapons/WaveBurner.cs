using System;

namespace MMXOnline;

public class WaveBurner : Weapon {
	public WaveBurner() : base() {
		index = (int)BassWeaponIds.WaveBurner;
		weaponSlotIndex = 4;
		fireRateFrames = 4;
		isStream = true;
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Point shootPos = character.getShootPos();
		Player player = character.player;
	}
}
