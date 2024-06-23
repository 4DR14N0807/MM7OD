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
        character.playSound("spark_shock", true);
	}
}
public class SparkShockProj : Projectile {


    public SparkShockProj(Weapon weapon, Point pos, int xDir, Player player, ushort? netId, bool rpc = false) : 
    base(weapon, pos, xDir, 180, 2, player, "spark_shock_proj", 0, 0, netId, player.ownedByLocalPlayer) {
        maxTime = 0.75f;
        projId = (int)BluesProjIds.SparkShock;
    }


    public override void update() {
        base.update();}
        
    }
