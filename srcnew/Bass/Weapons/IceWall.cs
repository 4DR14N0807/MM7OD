using System;

namespace MMXOnline;

public class IceWall : Weapon {
	public IceWall() : base() {
		index = (int)BassWeaponIds.IceWall;
		weaponSlotIndex = 1;
		fireRateFrames = 180;
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Point shootPos = character.getShootPos();
		Player player = character.player;

	}
}
