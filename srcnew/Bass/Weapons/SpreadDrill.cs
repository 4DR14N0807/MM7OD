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
		Bass bass = character as Bass ?? throw new NullReferenceException();

		new SpreadDrillProj(bass, shootPos, bass.getShootXDir(), player.getNextActorNetId(), true);
		character.playSound("spreaddrill", true);
	}
}


public class SpreadDrillProj : Projectile {
	float timeTouseGravity;
	Bass? bass;
	Point addPos;
	Player player;
	Actor ownChr = null!;
	string exhaustSprite = "spread_drill_effect";
	Anim? anim;
	public SpreadDrillProj(
		Actor owner, Point pos, int xDir, ushort? netProjId, 
		bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "spread_drill_proj", netProjId, altPlayer
	) {
		maxTime = 2f;
		projId = (int)BassProjIds.SpreadDrill;
		destroyOnHit = false;
		this.player = ownerPlayer;
		if (ownedByLocalPlayer) {
			bass = player.character as Bass;
			if (bass != null) {
				bass.sDrill = this;
			}

			anim = new Anim(pos, exhaustSprite, xDir, null, false, false) 
			{ visible = false };
		}
		addPos = new Point(-22 * xDir, 7);

		vel.x = 100 * xDir;
		damager.damage = 2;
		damager.hitCooldown = 60;
		ownChr = owner;

		canBeLocal = false;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netProjId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new SpreadDrillProj(
			arg.owner, arg.pos, arg.xDir, arg.netId, altPlayer: arg.player
		);
	}
	public override void update() {
		base.update();
		timeTouseGravity += Global.speedMul;
		if (timeTouseGravity >= 60) { useGravity = true; }
		if (useGravity && gravityModifier > 0.75f) {
			gravityModifier -= 0.01f;
		}
		if (bass == null) return;

		if (ownedByLocalPlayer) {
			if (player.input.isPressed(Control.Shoot, player) && bass.currentWeapon is SpreadDrill) {
				new SpreadDrillMediumProj(ownChr, pos.addxy(0, 25), xDir, player.getNextActorNetId(), rpc: true);
				new SpreadDrillMediumProj(ownChr, pos.addxy(0, -25), xDir, player.getNextActorNetId(), rpc: true);
				destroySelf();
				return;
			}
		}

	}

	public override void render(float x, float y) {
		base.render(x,y);
		int? fi = anim?.frameIndex;
		int fiv = fi ?? Global.frameCount % 2;

		Global.sprites[exhaustSprite].draw(fiv, pos.x + addPos.x, pos.y + addPos.y, xDir, yDir, null, 1, 1, 1, zIndex);
		Global.sprites[exhaustSprite].draw(fiv, pos.x + addPos.x, pos.y - addPos.y, xDir, yDir, null, 1, 1, 1, zIndex);
	}

	public override void onDestroy() {
		base.onDestroy();
		new Anim(pos, "spread_drill_pieces", xDir, null, false) {
			ttl = 2, useGravity = true, vel = Point.random(0, -50, 0, -50), frameIndex = 0, frameSpeed = 0
		};
		new Anim(
			pos, "spread_drill_pieces", xDir, null, false
		) {
			ttl = 2, useGravity = true, vel = Point.random(0, 150, 0, -50), frameIndex = 1, frameSpeed = 0
		};
		if (bass != null) {
			bass.sDrill = null;
		}
	}
}
public class SpreadDrillMediumProj : Projectile {
	float sparksCooldown;
	int hits;
	Point addPos;
	Actor ownChr = null!;
	float projSpeed = 200;

	public SpreadDrillMediumProj(
		Actor owner, Point pos, int xDir, ushort? netProjId,
		bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "spread_drill_medium_proj", netProjId, altPlayer
	) {
		maxTime = 1f;
		projId = (int)BassProjIds.SpreadDrillMid;
		destroyOnHit = false;

		vel.x = projSpeed * xDir;
		damager.damage = 1;
		damager.hitCooldown = 30;
		ownChr = owner;

		addPos = new Point(-14 * xDir, 1);
		canBeLocal = false;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netProjId, xDir);
		}

		//projId = (int)BassProjIds.SpreadDrill;
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new SpreadDrillMediumProj(
			arg.owner, arg.pos, arg.xDir, arg.netId, altPlayer: arg.player
		);
	}

	public override void update() {
		base.update();
		if (ownedByLocalPlayer) {
			if (owner.input.isPressed(Control.Shoot, owner) && (ownChr as Character)?.currentWeapon is SpreadDrill) {
				new SpreadDrillSmallProj(ownChr, pos.addxy(0, 15), xDir, owner.getNextActorNetId(), rpc: true);
				new SpreadDrillSmallProj(ownChr, pos.addxy(0, -15), xDir, owner.getNextActorNetId(), rpc: true);
				destroySelf(doRpcEvenIfNotOwned: true);
				return;
			}
		}

		Helpers.decrementTime(ref sparksCooldown);

		if (hits >= 3) destroySelfNoEffect(true, true);
		
		if (Math.Abs(vel.x) < projSpeed) vel.x += Global.speedMul * xDir * 8;
		else if (Math.Abs(vel.x) > projSpeed) vel.x = speed * xDir;
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		if (ownedByLocalPlayer) {
			forceNetUpdateNextFrame = true;
		} else {
			return;
		}
		vel.x = 0;
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
		new Anim(pos, "spread_drill_medium_pieces", xDir, null, false) {
			ttl = 2, useGravity = true, vel = Point.random(0, -50, 0, -50), frameIndex = 0, frameSpeed = 0
		};
		new Anim(pos, "spread_drill_medium_pieces", xDir, null, false) {
			ttl = 2, useGravity = true, vel = Point.random(0, 150, 0, -50), frameIndex = 1, frameSpeed = 0
		};
	}
}
public class SpreadDrillSmallProj : Projectile {
	float sparksCooldown;
	int hits;
	Point addPos;
	float projSpeed = 400;
	public SpreadDrillSmallProj(
		Actor owner, Point pos, int xDir, ushort? netProjId, 
		bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "spread_drill_small_proj", netProjId, altPlayer
	) {
		maxTime = 1.5f;
		projId = (int)BassProjIds.SpreadDrillSmall;
		destroyOnHit = false;

		vel.x = projSpeed * xDir;
		damager.damage = 1;
		damager.hitCooldown = 15f;

		addPos = new Point(-8 * xDir, 0);
		canBeLocal = false;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netProjId, xDir);
		}
		//projId = (int)BassProjIds.SpreadDrill;
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new SpreadDrillSmallProj(
			arg.owner, arg.pos, arg.xDir, arg.netId, altPlayer: arg.player
		);
	}

	public override void update() {
		base.update();
		Helpers.decrementTime(ref sparksCooldown);

		if (hits >= 3) destroySelf();

		if (Math.Abs(vel.x) < projSpeed) vel.x += Global.speedMul * xDir * 16;
		else if (Math.Abs(vel.x) > projSpeed) vel.x = speed * xDir;
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		if (ownedByLocalPlayer) {
			forceNetUpdateNextFrame = true;
		} else {
			return;
		};
		vel.x = 0;
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
		new Anim(pos, "spread_drill_small_pieces", xDir, null, false) {
			ttl = 2, useGravity = true, vel = Point.random(0, -50, 0, -50), frameIndex = 0, frameSpeed = 0
		};
		new Anim(pos, "spread_drill_small_pieces", xDir, null, false) {
			ttl = 2, useGravity = true, vel = Point.random(0, 150, 0, -50), frameIndex = 1, frameSpeed = 0
		};
	}
}
