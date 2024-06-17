using System;
using System.Collections.Generic;

namespace MMXOnline;


public class HardKnuckle : Weapon {
    public HardKnuckle() : base() {
        index = (int)RockWeaponIds.HardKnuckle;
        rateOfFire = 1.25f;
    }


    public override void shoot(Character character, params int[] args) {
	    base.shoot(character, args);
        Point shootPos = character.getShootPos();
        int xDir = character.getShootXDir();
	}
}