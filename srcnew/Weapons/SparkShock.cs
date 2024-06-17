using System;
using System.Collections.Generic;

namespace MMXOnline;


public class SparkShock : Weapon {
    public SparkShock() : base() {
        index = (int)RockWeaponIds.SparkShock;
        rateOfFire = 1f;
    }


     public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
        Point shootPos = character.getShootPos();
        int xDir = character.getShootXDir();
	}
}