using System;
using System.Collections.Generic;

namespace MMXOnline;


public class StarCrash : Weapon {
    public StarCrash() : base() {
        index = (int)RockWeaponIds.StarCrash;
        rateOfFire = 1f;
    }


    public override void shoot(Character character, params int[] args) {
	    base.shoot(character, args);
        Point shootPos = character.getShootPos();
        int xDir = character.getShootXDir();
	}
}