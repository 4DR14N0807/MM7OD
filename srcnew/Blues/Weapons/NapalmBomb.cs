using System;
using System.Collections.Generic;
using SFML.Graphics;

namespace MMXOnline;

public class NapalmBomb : Weapon {
	public static NapalmBomb netWeapon = new();

	public NapalmBomb() : base() {
		displayName = "NAPALM BOMB";
		descriptionV2 = "";
		defaultAmmoUse = 3;

		//index = (int)RockWeaponIds.NapalmBomb;
		fireRate = 40;
	}

	public override float getAmmoUsage(int chargeLevel) {
		return defaultAmmoUse;
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Point shootPos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;
		float shootAngle = 0;
		if (xDir == -1) {
			shootAngle = -shootAngle + 128;
		}

		new NapalmBombProj(shootPos, shootAngle, player, player.getNextActorNetId(), true) {
			owningActor = character
		};
		character.playSound("buster");
	}
}


public class NapalmBombProj : Projectile {

	bool bouncedOnce;
	int bounces;
	int airTime;
	public NapalmBombProj(
		Point pos, float byteAngle, Player player, ushort? netId, bool rpc = false
	) : base(
		NapalmBomb.netWeapon, pos, 1, 0, 0.5f, player, "napalm_bomb_proj",
		0, 0, netId, player.ownedByLocalPlayer
	) {
		byteAngle = MathF.Round(byteAngle);
		this.byteAngle = byteAngle;
		vel = Point.createFromByteAngle(byteAngle) * 120;
		useGravity = true;
		maxTime = 5;

		if (rpc) {
			rpcCreateByteAngle(pos, player, netId, byteAngle);
		}
	}

	public override void update() {
		base.update();

		byteAngle += vel.x / 64;
		if (airTime < 60) airTime++;

		if (bounces >= 5) destroySelf();
	}


	public override void onCollision(CollideData other) {
		var wall = other.gameObject as Wall;

		if (wall != null) {

			if (other.isGroundHit() || other.isCeilingHit()) {
				if (!bouncedOnce) vel.y = -240 - (airTime * 3);
				else {
					vel.y *= -0.5f;
					incPos(new Point(0, 5 * MathF.Sign(vel.y)));
				}
			} else if (other.isSideWallHit()) {
				vel.x *= -0.8f;
				vel.y *= -1;
				incPos(new Point(5 * MathF.Sign(vel.x), 5 * MathF.Sign(vel.y)));
			}
			Global.playSound("ding");
			bouncedOnce = true;
			bounces++;
			airTime = 0;
		}
	}

	public override void onDestroy() {
		base.onDestroy();

		for (int i = 0; i < 6; i++) {
			float x = Helpers.cosd(i * 60) * 120;
			float y = Helpers.sind(i * 60) * 120;
			new Anim(pos, "generic_explosion", 1, null, true) { vel = new Point(x, y) };
		}
		playSound("danger_wrap_explosion");

		new NapalmBombExplosionProj(pos, xDir, damager.owner, damager.owner.getNextActorNetId(), true);
	}
}


public class NapalmBombExplosionProj : Projectile {

	int radius;
	const int maxRadius = 48;
	public NapalmBombExplosionProj(
		Point pos, int xDir, Player player,
		ushort? netProjId, bool rpc = false
	) : base 
	(
		NapalmBomb.netWeapon, pos, xDir, 0, 3,
		player, "empty", Global.halfFlinch, 1, 
		netProjId, player.ownedByLocalPlayer
	) {
		destroyOnHit = false;
		shouldShieldBlock = false;
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
		DrawWrappers.DrawCircle(pos.x + x, pos.y + y, radius, filled: true, col1, 3f, zIndex - 10, isWorldPos: true, col2);
	}
}
