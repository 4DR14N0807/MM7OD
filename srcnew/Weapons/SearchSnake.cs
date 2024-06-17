using System;
using System.Collections.Generic;

namespace MMXOnline;


public class SearchSnake : Weapon {
    public SearchSnake() : base() {
        index = (int)RockWeaponIds.SearchSnake;
        rateOfFire = 0.75f;
    }


     public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
        Point shootPos = character.getShootPos();
        int xDir = character.getShootXDir();
	}
}