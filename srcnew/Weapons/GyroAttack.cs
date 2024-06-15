using System;
using System.Collections.Generic;

namespace MMXOnline;

public class GyroAttack : Weapon {
    public GyroAttack() : base() {
        index = (int)RockWeaponIds.GyroAttack;
        rateOfFire = 1f;

    }


	public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId) {
		base.getProjectile(pos, xDir, player, chargeLevel, netProjId);
	}
}



public class GyroAttackProj : Projectile {

    bool changedDir;
    Player player;
    const float projSpeed = 180;

    public GyroAttackProj(Weapon weapon, Point pos, int xDir, Player player, ushort? netId, bool rpc = false) : 
    base(weapon, pos, xDir, projSpeed, 2, player, "gyro_attack_proj", 0, 0, netId, player.ownedByLocalPlayer) {
        maxTime = 1f;
        this.player =  player;
        canBeLocal = false;
        
    }


    public override void update() {
        base.update();

        if (!changedDir) {
            if (player.input.isPressed(Control.Up, player)) {
                base.vel = new Point(0, -projSpeed);
                changedDir = true;
            }
            else if (player.input.isPressed(Control.Down, player)) {
                base.vel = new Point(0, projSpeed);
                changedDir = true;
            } 
        }
    }
}