using System;
using System.Collections.Generic;

namespace MMXOnline;

public class SpreadDrill : Weapon {

	public static SpreadDrill netWeapon = new();
	public SpreadDrill() : base() {
		index = (int)BassWeaponIds.SpreadDrill;
		weaponSlotIndex = 3;
		weaponBarBaseIndex = 0;
		killFeedIndex = 0;
		maxAmmo = 7;
		ammo = maxAmmo;
		rateOfFire = 1.5f;
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
		projId = (int)ProjIds.TunnelFang;
		destroyOnHit = false;
		bass = player.character as Bass;
		if (bass != null) bass.sDrill = this;

		anim = new Anim(getFirstPOI(0).Value, "spread_drill_effect", xDir, player.getNextActorNetId(), false, true);
		anim2 = new Anim(getFirstPOI(1).Value, "spread_drill_effect", xDir, player.getNextActorNetId(), false, true);

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
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

		if (anim != null) anim.pos = getFirstPOI(0).Value;
		if (anim2 != null) anim2.pos = getFirstPOI(1).Value;
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

	public SpreadDrillMediumProj(
		Point pos, int xDir, Player player, ushort netProjId, bool rpc = false
	) : base(
		SpreadDrill.netWeapon, pos, xDir, 200, 1, player, "spread_drill_medium_proj", 0, 0.50f, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 1f;
		projId = (int)ProjIds.TunnelFang;
		destroyOnHit = false;

		anim = new Anim(getFirstPOI().Value, "spread_drill_medium_effect", xDir, player.getNextActorNetId(), false, true);

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
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

		if (anim != null) anim.pos = getFirstPOI(0).Value;
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		vel.x = 4 * xDir;
		// To update the reduced speed.
		if (ownedByLocalPlayer) {
			forceNetUpdateNextFrame = true;
		}

		if (damagable is not CrackedWall) {
			time -= Global.spf;
			if (time < 0) time = 0;
		}

		if (sparksCooldown == 0) {
			//playSound("tunnelFangDrill");
			//var sparks = new Anim(pos, "tunnelfang_sparks", xDir, null, true);
			//sparks.setzIndex(zIndex + 100);
			sparksCooldown = 0.25f;
		}
		var chr = damagable as Character;
		if (chr != null && chr.ownedByLocalPlayer && !chr.isImmuneToKnockback()) {
			chr.vel = Point.lerp(chr.vel, Point.zero, Global.spf * 10);
			chr.slowdownTime = 0.25f;
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
	public SpreadDrillSmallProj(
		Point pos, int xDir, Player player, ushort netProjId, bool rpc = false
	) : base(
		SpreadDrill.netWeapon, pos, xDir, 400, 1, player, "spread_drill_small_proj", 0, 0.25f, netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 1.5f;
		projId = (int)ProjIds.TunnelFang;
		destroyOnHit = false;

		anim = new Anim(pos.addxy(-5, 5), "spread_drill_small_effect", xDir, player.getNextActorNetId(), false, true);

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void update() {
		base.update();
		Helpers.decrementTime(ref sparksCooldown);

		if (anim != null) anim.pos = getFirstPOI(0).Value;
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		vel.x = 4 * xDir;
		// To update the reduced speed.
		if (ownedByLocalPlayer) {
			forceNetUpdateNextFrame = true;
		}

		if (damagable is not CrackedWall) {
			time -= Global.spf;
			if (time < 0) time = 0;
		}

		if (sparksCooldown == 0) {
			//playSound("tunnelFangDrill");
			//var sparks = new Anim(pos, "tunnelfang_sparks", xDir, null, true);
			//sparks.setzIndex(zIndex + 100);
			sparksCooldown = 0.25f;
		}
		var chr = damagable as Character;
		if (chr != null && chr.ownedByLocalPlayer && !chr.isImmuneToKnockback()) {
			chr.vel = Point.lerp(chr.vel, Point.zero, Global.spf * 10);
			chr.slowdownTime = 0.25f;
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		if (anim != null) anim.destroySelf();

		new Anim(pos, "spread_drill_small_pieces", xDir, null, false) { ttl = 2, useGravity = true, vel = Point.random(0, -50, 0, -50), frameIndex = 0, frameSpeed = 0 };
		new Anim(pos, "spread_drill_small_pieces", xDir, null, false) { ttl = 2, useGravity = true, vel = Point.random(0, 150, 0, -50), frameIndex = 1, frameSpeed = 0 };
	}
}
