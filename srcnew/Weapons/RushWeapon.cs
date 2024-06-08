using System;
using System.Collections.Generic;

namespace MMXOnline;

public class RushWeapon : Weapon {
    public RushWeapon() : base() {
        maxAmmo = 7;
        ammo = maxAmmo;
    }


	public override bool canShoot(int chargeLevel, Player player) {
		Rock? rock = player.character as Rock;
        if (rock.rush != null) return false;
        return true;
	}


	public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId) {
		base.getProjectile(pos, xDir, player, chargeLevel, netProjId);
        if (player.character is Rock rock) {
            rock.rush = new Rush(pos, player, xDir, netProjId, true, true);
        }
	}
}