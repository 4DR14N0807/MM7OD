using System;
using System.Collections.Generic;
using SFML.Graphics;

namespace MMXOnline;

public class RushWeapon : Weapon {

	public static RushWeapon netWeapon = new();
	public RushWeapon() : base() {
		maxAmmo = 28;
		ammo = maxAmmo;
		displayName = "";
	}
	public override float getAmmoUsage(int chargeLevel) {
		return 0;
	}

	public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId) {
		base.getProjectile(pos, xDir, player, chargeLevel, netProjId);
		/*if (player.character is Rock rock) {
			int type = rock.rushWeaponIndex;
			rock.rush = new Rush(pos, player, xDir, netProjId, true, type, true);
		}*/
		shoot(player.character, (int)chargeLevel);
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Point shootPos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;

		if (player.character is Rock rock) {
			if (!rock.canCallRush()) return;
			if (rock.rush != null) {
				rock.rush.changeState(new RushWarpOut());
			} else {
				int type = rock.rushWeaponIndex;
				rock.rush = new Rush(shootPos, player, xDir, player.getNextActorNetId(), true, type, true);
			}
		}
	}
}

public class RushCoilWeapon : RushWeapon {
	public RushCoilWeapon() : base() {
		index = (int)RockWeaponIds.RushCoil;
		maxAmmo = 7;
		ammo = maxAmmo;
		weaponSlotIndex = (int)RockWeaponSlotIds.RushCoil;
		displayName = "RUSH COIL";
		description = new string[] { "Propels you to higher areas", "by jumping on him." };
	}
}

public class RushJetWeapon : RushWeapon {
	public RushJetWeapon() : base() {
		index = (int)RockWeaponIds.RushJet;
		weaponSlotIndex = (int)RockWeaponSlotIds.RushJet;
		displayName = "RUSH JET";
		description = new string[] { "Rush transforms into a sled that", "you can control mid-air" };

	}
}


public class RushSearchWeapon : RushWeapon {
	public RushSearchWeapon() : base() {
		index = (int)RockWeaponIds.RushSearch;
		weaponSlotIndex = (int)RockWeaponSlotIds.RushSearch;
		drawAmmo = false;
		displayName = "RUSH SEARCH";
		description = new string[] { "Spends 5 bolts to find a random item", "'Wanna test your luck?'"};
	}
}


public class RSBombProj : Projectile {
	public RSBombProj( 
		Point pos, int xDir, Player player, 
		ushort? netId, bool rpc = false) 
	: base
	(
		RushWeapon.netWeapon, pos, xDir, 0, 0,
		player, "rush_search_bomb", 0, 1, 
		netId, player.ownedByLocalPlayer
	) {
		projId = (int)RockProjIds.RSBomb;
		maxTime = 2f;
		destroyOnHitWall = true;
		fadeSprite = "generic_explosion";
		fadeSound = "danger_wrap_explosion";
		base.vel.y = -360;
		useGravity = true;
	}

	public override void onCollision(CollideData other) {
		var damagable = other.gameObject as IDamagable;
		var wall = other.gameObject as Wall;

		if ((damagable != null || wall != null) && damagable is not Rush && base.vel.y > 0) destroySelf();
	}

	public override void onDestroy() {
		base.onDestroy();

		new RSBombExplosionProj(pos, xDir, damager.owner, damager.owner.getNextActorNetId(), true);
	}
}


public class RSBombExplosionProj : Projectile {

	int radius;
	float maxRadius = 64;
	public RSBombExplosionProj(
		Point pos, int xDir, Player player,
		ushort? netId, bool rpc = false)
	: base 
	(
		RushWeapon.netWeapon, pos, xDir, 0, 1,
		player, "empty", Global.defFlinch, 1,
		netId, player.ownedByLocalPlayer
	) {
		//maxTime = 1f;
		projId = (int)RockProjIds.RSBombExplosion;
	}

	public override void update() {
		base.update();

		if (radius < maxRadius) radius += 4;
		else destroySelf();

		if (isRunByLocalPlayer()) {
			foreach (var go in Global.level.getGameObjectArray()) {
				var chr = go as Character;
				if (chr != null && chr.canBeDamaged(damager.owner.alliance, damager.owner.id, projId)
					&& chr.pos.distanceTo(pos) <= radius) {

					damager.applyDamage(chr, false, weapon, this, projId);
				}	
			}
		}
	}

	public override void render(float x, float y) {
		base.render(x, y);
		double transparency = (time) / (0.4);
		if (transparency < 0) { transparency = 0; }
		Color col1 = new(222, 41, 24, 128);
		Color col2 = new(255, 255, 255, 255);
		DrawWrappers.DrawCircle(pos.x + x, pos.y + y, radius, filled: true, col1, 2f, zIndex - 10, isWorldPos: true, col2);
	}
}
