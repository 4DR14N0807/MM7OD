using System;

namespace MMXOnline;

public class TenguBlade : Weapon {
	public TenguBlade() : base() {
		index = (int)BassWeaponIds.TenguBlade;
		weaponSlotIndex = 7;
		fireRateFrames = 60;
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Point shootPos = character.getShootPos();
		Player player = character.player;

	}
}
