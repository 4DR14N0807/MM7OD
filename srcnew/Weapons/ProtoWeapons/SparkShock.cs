using System;
using System.Collections.Generic;

namespace MMXOnline;

public class SparkShock : Weapon {
    public SparkShock() : base() {
        index = (int)RockWeaponIds.SparkShock;
        fireRateFrames = 60;
    }

     public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
        Point shootPos = character.getShootPos();
        int xDir = character.getShootXDir();
        new SparkShockProj(this, shootPos, xDir, character.player, character.player.getNextActorNetId(), true);

	}
}
public class SparkShockProj : Projectile {


    public SparkShockProj(Weapon weapon, Point pos, int xDir, Player player, ushort? netId, bool rpc = false) : 
    base(weapon, pos, xDir, 100, 2, player, "hard_knuckle_proj", 0, 0, netId, player.ownedByLocalPlayer) {
        maxTime = 1f;
        projId = (int)BluesProjIds.SparkShock;
        canBeLocal = false;
        
    }


    public override void update() {
        base.update();}
        
    }
