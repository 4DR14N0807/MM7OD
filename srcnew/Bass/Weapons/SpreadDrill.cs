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
		maxAmmo = 7;
		ammo = maxAmmo;
		fireRateFrames = 90;
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
	}
}
public class SpreadDrillProj : Projectile {
	//int state = 0;
	float timeTouseGravity;
	Anim? anim;
	Anim? anim2;
	Bass? bass;
	public SpreadDrillProj(
		Point pos, int xDir, Player player, ushort netProjId, bool rpc = false
	) : base(
		SpreadDrill.netWeapon, pos, xDir, 100, 2, player, "spread_drill_proj", 0, 1f, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 2f;
		projId = (int)BassProjIds.SpreadDrill;
		destroyOnHit = false;
		bass = player.character as Bass;
		if (bass != null) bass.sDrill = this;

		anim = new Anim(getFirstPOI(0) ?? new Point(0,0), "spread_drill_effect", xDir, player.getNextActorNetId(), false, true);
		anim2 = new Anim(getFirstPOI(1) ?? new Point(0,0), "spread_drill_effect", xDir, player.getNextActorNetId(), false, true);
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

		timeTouseGravity += Global.spf;
		if (timeTouseGravity >= 1f) { useGravity = true; }

		if (ownedByLocalPlayer) {
			if (owner.input.isPressed(Control.Shoot, owner)) {
				new SpreadDrillMediumProj(pos.addxy(0, 25), xDir, owner, owner.getNextActorNetId(), rpc: true);
				new SpreadDrillMediumProj(pos.addxy(0, -25), xDir, owner, owner.getNextActorNetId(), rpc: true);
				destroySelf();
			}
		}

		if (useGravity && gravityModifier > 0.75f) {
			gravityModifier -= 0.01f;
		}

		if (anim != null) anim.changePos(getFirstPOI(0) ?? new Point(0, 0));
		if (anim2 != null) anim2.changePos(getFirstPOI(1) ?? new Point(0, 0));
	}

	public override void onDestroy() {
		base.onDestroy();
		if (bass != null) bass.sDrill = null;
		if (anim != null) anim.destroySelf();
		if (anim2 != null) anim2.destroySelf();
		new Anim(pos, "spread_drill_pieces", xDir, null, false) { ttl = 2, useGravity = true, vel = Point.random(0, -50, 0, -50), frameIndex = 0, frameSpeed = 0 };
		new Anim(pos, "spread_drill_pieces", xDir, null, false) { ttl = 2, useGravity = true, vel = Point.random(0, 150, 0, -50), frameIndex = 1, frameSpeed = 0 };
	}
}
public class SpreadDrillMediumProj : Projectile {
	float sparksCooldown;
	Anim? anim;
	int hits;

	public SpreadDrillMediumProj(
		Point pos, int xDir, Player player, ushort netProjId, bool rpc = false
	) : base(
		SpreadDrill.netWeapon, pos, xDir, 200, 1, player, "spread_drill_medium_proj", 0, 0.50f, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 1f;
		projId = (int)BassProjIds.SpreadDrillMid;
		destroyOnHit = false;

		anim = new Anim(getFirstPOI() ?? new Point(0,0), "spread_drill_medium_effect", xDir, player.getNextActorNetId(), false, true);
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
			}
		}
		Helpers.decrementTime(ref sparksCooldown);

		if (anim != null) anim.changePos(getFirstPOI(0) ?? new Point(0, 0));
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
			}
		} 
	}

	public override void onDestroy() {
		base.onDestroy();
		if (anim != null) anim.destroySelf();

		new Anim(pos, "spread_drill_medium_pieces", xDir, null, false) { ttl = 2, useGravity = true, vel = Point.random(0, -50, 0, -50), frameIndex = 0, frameSpeed = 0 };
		new Anim(pos, "spread_drill_medium_pieces", xDir, null, false) { ttl = 2, useGravity = true, vel = Point.random(0, 150, 0, -50), frameIndex = 1, frameSpeed = 0 };
	}
}
public class SpreadDrillSmallProj : Projectile {
	float sparksCooldown;
	Anim? anim;
	int hits;
	public SpreadDrillSmallProj(
		Point pos, int xDir, Player player, ushort netProjId, bool rpc = false
	) : base(
		SpreadDrill.netWeapon, pos, xDir, 400, 1, player, "spread_drill_small_proj", 0, 0.25f, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 1.5f;
		projId = (int)BassProjIds.SpreadDrillSmall;
		destroyOnHit = false;

		anim = new Anim(pos.addxy(-5, 5), "spread_drill_small_effect", xDir, player.getNextActorNetId(), false, true);
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

		if (anim != null) anim.changePos(getFirstPOI(0) ?? new Point(0, 0));
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
			}
		} 
	}

	public override void onDestroy() {
		base.onDestroy();
		if (anim != null) anim.destroySelf();

		new Anim(pos, "spread_drill_small_pieces", xDir, null, false) { ttl = 2, useGravity = true, vel = Point.random(0, -50, 0, -50), frameIndex = 0, frameSpeed = 0 };
		new Anim(pos, "spread_drill_small_pieces", xDir, null, false) { ttl = 2, useGravity = true, vel = Point.random(0, 150, 0, -50), frameIndex = 1, frameSpeed = 0 };
	}
}
