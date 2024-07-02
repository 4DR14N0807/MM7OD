using System;
using System.Collections.Generic;

namespace MMXOnline;

public class RushWeapon : Weapon {
	public RushWeapon() : base() {
		maxAmmo = 7;
		ammo = maxAmmo;
		weaponSlotIndex = (int)RockWeaponSlotIds.RushCoil;
	}

	public override bool canShoot(int chargeLevel, Player player) {
		Rock? rock = player.character as Rock;

		return rock?.rush == null && base.canShoot(chargeLevel, player);
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 0;
	}

	public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId) {
		base.getProjectile(pos, xDir, player, chargeLevel, netProjId);
		if (player.character is Rock rock) {
			rock.rush = new Rush(pos, player, xDir, netProjId, true, true);
		}
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Point shootPos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;

		if (player.character is Rock rock) {
			rock.rush = new Rush(shootPos, player, xDir, player.getNextActorNetId(), true, true);
		}
	}
}
