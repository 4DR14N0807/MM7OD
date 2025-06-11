using System;
using System.Collections.Generic;
using SFML.Graphics;

namespace MMXOnline;

public class RushWeapon : Weapon {
	public static RushWeapon netWeapon = new();

	public RushWeapon() : base() {
		maxAmmo = 28;
		ammo = maxAmmo;
		weaponSlotIndex = (int)RockWeaponSlotIds.RushSearch;
		displayName = "Rush";
		hasCustomAnim = true;
	}
	public override float getAmmoUsage(int chargeLevel) {
		return 0;
	}

	public override void shootRock(Rock rock, params int[] args) {
		base.shootRock(rock, args);
		Point shootPos = rock.getShootPos();
		int xDir = rock.getShootXDir();
		Player player = rock.player;
		int type = 0;
		if (player.input.isHeld(Control.Up, player)) type = 1;
		else if (player.input.isHeld(Control.Down, player)) type = 2;

		if (!player.ownedByLocalPlayer) return;

		if (player.character is Rock) {
			if (!rock.canCallRush(type)) return;
			if (type < 2 && ammo <= 0) return;
			if (rock.rush != null) {
				rock.rush.changeState(new RushWarpOut());
			} else {
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

	Actor ownChr = null!;

	public RSBombProj( 
		Actor owner, Point pos, int xDir, ushort? netProjId,
		bool rpc = false, Player? altPlayer = null
	) : base
	(
		pos, xDir, owner, "rush_search_bomb", netProjId, altPlayer
	) {
		projId = (int)RockProjIds.RSBomb;
		maxTime = 2f;
		destroyOnHitWall = true;
		fadeSprite = "generic_explosion";
		fadeSound = "danger_wrap_explosion";
		damager.hitCooldown = 60;
		base.vel.y = -360;
		useGravity = true;

		if (ownedByLocalPlayer) ownChr = owner;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new RSBombProj(
			arg.owner, arg.pos, arg.xDir, arg.netId, altPlayer: arg.player
		);
	}
	
	public override void onCollision(CollideData other) {
		var damagable = other.gameObject as IDamagable;
		var wall = other.gameObject as Wall;

		if ((damagable != null || wall != null) && damagable is not Rush && base.vel.y > 0) destroySelf();
	}

	public override void onDestroy() {
		base.onDestroy();
		if (!ownedByLocalPlayer) return;

		new RSBombExplosionProj(ownChr, pos, xDir, damager.owner.getNextActorNetId(), true, damager.owner);
	}
}


public class RSBombExplosionProj : Projectile {

	int radius;
	float maxRadius = 64;
	public RSBombExplosionProj(
		Actor owner, Point pos, int xDir, ushort? netProjId,
		bool rpc = false, Player? altPlayer = null
	) : base 
	(
		pos, xDir, owner, "empty", netProjId, altPlayer
	) {
		maxTime = 1f;
		projId = (int)RockProjIds.RSBombExplosion;

		damager.damage = 1;
		damager.flinch = Global.defFlinch;
		damager.hitCooldown = 60;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new RSBombExplosionProj(
			arg.owner, arg.pos, arg.xDir, arg.netId, altPlayer: arg.player
		);
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
