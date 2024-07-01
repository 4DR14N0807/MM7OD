using System;

namespace MMXOnline;

public class MagicCard : Weapon {
    public MagicCard() : base() {
        index = (int)RockWeaponIds.MagicCard;
        weaponSlotIndex = 8;
        fireRateFrames = 20;
    }

    public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Point shootPos = character.getShootPos();
		Player player = character.player;
       
	}
}