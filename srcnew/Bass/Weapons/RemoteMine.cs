using System;

namespace MMXOnline;

public class RemoteMine : Weapon {
	public RemoteMine() : base() {
		index = (int)BassWeaponIds.RemoteMine;
		weaponSlotIndex = 5;
		fireRateFrames = 45;
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Point shootPos = character.getShootPos();
		Player player = character.player;

	}
}
