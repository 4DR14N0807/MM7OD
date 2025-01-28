using System;
using System.Collections.Generic;

namespace MMXOnline;

public class SpreadDrill : Weapon {
	public static SpreadDrill netWeapon = new();

	public SpreadDrill() : base() {
		index = (int)BassWeaponIds.SpreadDrill;
		displayName = "SPREAD DRILL";
		weaponSlotIndex = index;
		weaponBarBaseIndex = index;
		weaponBarIndex = index;
		maxAmmo = 10;
		ammo = maxAmmo;
		fireRate = 90;
		descriptionV2 = (
			"Shoots a drill that spread by pressing SPECIAL." + "\n" +
			"Slowdown on hit, the smaller the drill the faster the drill."
		);
	}

	public override bool canShoot(int chargeLevel, Player player) {
		if (!base.canShoot(chargeLevel, player)) return false;
		Bass? bass = Global.level.mainPlayer.character as Bass;
		return bass?.sDrill == null;
	}


	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Point shootPos = character.getShootPos();
		Player player = character.player;
		Bass? bass = character as Bass;

		new SpreadDrillProj(shootPos, character.getShootXDir(), player, player.getNextActorNetId(), true);
		character.playSound("spreaddrill", true);
	}
}


public class SpreadDrillProj : Projectile {
	float timeTouseGravity;
	Bass bass = null!;
	Point addPos;
	Player player;
	public SpreadDrillProj(
		Point pos, int xDir, Player player, ushort netProjId, bool rpc = false
	) : base(
		SpreadDrill.netWeapon, pos, xDir, 100, 2, 
		player, "spread_drill_proj", 0, 1f, 
		netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 2f;
		projId = (int)BassProjIds.SpreadDrill;
		destroyOnHit = false;
		bass = player.character as Bass ?? throw new NullReferenceException();
		this.player = player;
		if (bass != null) bass.sDrill = this;
		addPos = new Point(-22 * xDir, 7);

		canBeLocal = false;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new SpreadDrillProj(
			arg.pos, arg.xDir, arg.player, arg.netId
		);
	}
	public override void update() {
		base.update();
		if (bass == null) return;

		timeTouseGravity += Global.speedMul;
		if (timeTouseGravity >= 60) { useGravity = true; }

		if (ownedByLocalPlayer) {
			if (player.input.isPressed(Control.Shoot, player)) {
				new SpreadDrillMediumProj(pos.addxy(0, 25), xDir, player, player.getNextActorNetId(), rpc: true);
				new SpreadDrillMediumProj(pos.addxy(0, -25), xDir, player, player.getNextActorNetId(), rpc: true);
				destroySelf();
				return;
			}
		}

		if (useGravity && gravityModifier > 0.75f) {
			gravityModifier -= 0.01f;
		}
	}

	public override void render(float x, float y) {
		base.render(x,y);
		string exhaust = "spread_drill_effect";
		int fi = Global.frameCount % 2;

		Global.sprites[exhaust].draw(fi, pos.x + addPos.x, pos.y + addPos.y, xDir, yDir, null, 1, 1, 1, zIndex);
		Global.sprites[exhaust].draw(fi, pos.x + addPos.x, pos.y - addPos.y, xDir, yDir, null, 1, 1, 1, zIndex);
	}

	public override void onDestroy() {
		base.onDestroy();
		
		if (!ownedByLocalPlayer) return;

		bass.sDrill = null;
		new Anim(pos, "spread_drill_pieces", xDir, null, false) 
		{ ttl = 2, useGravity = true, vel = Point.random(0, -50, 0, -50), frameIndex = 0, frameSpeed = 0 };

		new Anim(pos, "spread_drill_pieces", xDir, null, false) 
		{ ttl = 2, useGravity = true, vel = Point.random(0, 150, 0, -50), frameIndex = 1, frameSpeed = 0 };
	}
}
public class SpreadDrillMediumProj : Projectile {
	float sparksCooldown;
	int hits;
	Point addPos;

	public SpreadDrillMediumProj(
		Point pos, int xDir, Player player, ushort netProjId, bool rpc = false
	) : base(
		SpreadDrill.netWeapon, pos, xDir, 200, 
		1, player, "spread_drill_medium_proj", 0, 0.50f, 
		netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 1f;
		projId = (int)BassProjIds.SpreadDrillMid;
		destroyOnHit = false;

		addPos = new Point(-14 * xDir, 1);
		canBeLocal = false;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}

		projId = (int)BassProjIds.SpreadDrill;
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new SpreadDrillMediumProj(
			arg.pos, arg.xDir, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();
		if (ownedByLocalPlayer) {
			if (owner.input.isPressed(Control.Shoot, owner)) {
				new SpreadDrillSmallProj(pos.addxy(0, 15), xDir, owner, owner.getNextActorNetId(), rpc: true);
				new SpreadDrillSmallProj(pos.addxy(0, -15), xDir, owner, owner.getNextActorNetId(), rpc: true);
				destroySelf();
				return;
			}
		}

		Helpers.decrementTime(ref sparksCooldown);

		if (hits >= 3) destroySelf();
		
		if (Math.Abs(vel.x) < speed) vel.x += Global.speedMul * xDir * 8;
		else if (Math.Abs(vel.x) > speed) vel.x = speed * xDir;
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		vel.x = 0;
		if (ownedByLocalPlayer) {
			forceNetUpdateNextFrame = true;
		}

		if (damagable.canBeDamaged(damager.owner.alliance, damager.owner.id, projId)) {
			if (damagable.projectileCooldown.ContainsKey(projId + "_" + owner.id) &&
				damagable.projectileCooldown[projId + "_" + owner.id] >= damager.hitCooldown
			) {
				hits++;
				playSound("spreaddrillHit", true);
			}
		} 
	}

	public override void render(float x, float y) {
		base.render(x,y);
		string exhaust = "spread_drill_effect";
		int fi = Global.frameCount % 2;

		Global.sprites[exhaust].draw(fi, pos.x + addPos.x, pos.y + addPos.y, xDir, yDir, null, 1, 1, 1, zIndex);
	}

	public override void onDestroy() {
		base.onDestroy();

		if (!ownedByLocalPlayer) return;

		new Anim(pos, "spread_drill_medium_pieces", xDir, null, false) 
		{ ttl = 2, useGravity = true, vel = Point.random(0, -50, 0, -50), frameIndex = 0, frameSpeed = 0 };

		new Anim(pos, "spread_drill_medium_pieces", xDir, null, false) 
		{ ttl = 2, useGravity = true, vel = Point.random(0, 150, 0, -50), frameIndex = 1, frameSpeed = 0 };
	}
}
public class SpreadDrillSmallProj : Projectile {
	float sparksCooldown;
	int hits;
	Point addPos;
	public SpreadDrillSmallProj(
		Point pos, int xDir, Player player, 
		ushort netProjId, bool rpc = false
	) : base(
		SpreadDrill.netWeapon, pos, xDir, 400, 1,
		player, "spread_drill_small_proj", 0, 0.25f, 
		netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 1.5f;
		projId = (int)BassProjIds.SpreadDrillSmall;
		destroyOnHit = false;

		addPos = new Point(-8 * xDir, 0);
		canBeLocal = false;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
		projId = (int)BassProjIds.SpreadDrill;
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new SpreadDrillSmallProj(
			arg.pos, arg.xDir, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();
		Helpers.decrementTime(ref sparksCooldown);

		if (hits >= 3) destroySelf();

		if (Math.Abs(vel.x) < speed) vel.x += Global.speedMul * xDir * 16;
		else if (Math.Abs(vel.x) > speed) vel.x = speed * xDir;
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		vel.x = 0;
		if (ownedByLocalPlayer) {
			forceNetUpdateNextFrame = true;
		}

		if (damagable.canBeDamaged(damager.owner.alliance, damager.owner.id, projId)) {
			if (damagable.projectileCooldown.ContainsKey(projId + "_" + owner.id) &&
				damagable.projectileCooldown[projId + "_" + owner.id] >= damager.hitCooldown
			) {
				hits++;
				playSound("spreaddrillHit", true);
			}
		} 
	}

	public override void render(float x, float y) {
		base.render(x,y);
		string exhaust = "spread_drill_effect";
		int fi = Global.frameCount % 2;

		Global.sprites[exhaust].draw(fi, pos.x + addPos.x, pos.y + addPos.y, xDir, yDir, null, 1, 1, 1, zIndex);
	}

	public override void onDestroy() {
		base.onDestroy();

		if (!ownedByLocalPlayer) return;

		new Anim(pos, "spread_drill_small_pieces", xDir, null, false) 
		{ ttl = 2, useGravity = true, vel = Point.random(0, -50, 0, -50), frameIndex = 0, frameSpeed = 0 };

		new Anim(pos, "spread_drill_small_pieces", xDir, null, false) 
		{ ttl = 2, useGravity = true, vel = Point.random(0, 150, 0, -50), frameIndex = 1, frameSpeed = 0 };
	}
}
