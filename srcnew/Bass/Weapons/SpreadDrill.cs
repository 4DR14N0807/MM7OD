using System;
using System.Collections.Generic;

namespace MMXOnline;

public class SpreadDrill : Weapon {
	public static SpreadDrill netWeapon = new();

	public SpreadDrill() : base() {
		iconSprite = "hud_weapon_icon_bass";
		index = (int)BassWeaponIds.SpreadDrill;
		displayName = "SPREAD DRILL";
		weaponSlotIndex = index;
		weaponBarBaseIndex = index;
		weaponBarIndex = index;
		maxAmmo = 10;
		ammo = maxAmmo;
		fireRate = 90;
		switchCooldown = 30;
		//descriptionV2 = (
		//	"Shoots a drill that spread by pressing SPECIAL." + "\n" +
		//	"Slowdown on hit, the smaller the drill the faster it is."
		//);
		descriptionV2 = [
			[ "Shoots a drill that splits by pressing SHOOT button\n" +  
			"The smaller the drill the faster it is." ],
		];
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
		addPos = new Point(-20 * xDir, 7);

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
		if (timeTouseGravity >= 60 || player.input.isHeld(Control.Down, player)) { useGravity = true; }
		if (useGravity && gravityModifier > 0.75f) {
			gravityModifier -= 0.01f;
		}
		if (bass == null) return;

		if (ownedByLocalPlayer) {
			if (player.input.isPressed(Control.Shoot, player) && bass.currentWeapon is SpreadDrill) {
				new SpreadDrillMediumProj(ownChr, pos, xDir, player.getNextActorNetId(), true, rpc: true);
				new SpreadDrillMediumProj(ownChr, pos, xDir, player.getNextActorNetId(), false, rpc: true);
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
			ttl = 2, useGravity = true, vel = Point.random(0, -50, 0, -50), frameIndex = 0, frameSpeed = 0,
			gravityModifier = 0.33f, blink = true
		};
		new Anim(
			pos, "spread_drill_pieces", xDir, null, false
		) {
			ttl = 2, useGravity = true, vel = Point.random(0, 150, 0, -50), frameIndex = 1, frameSpeed = 0,
			gravityModifier = 0.33f, blink = true
		};
		if (bass != null) {
			bass.sDrill = null;
		}
	}

	public override void onDamageEX(IDamagable damagable) {
		base.onDamageEX(damagable);
		if (!ownedByLocalPlayer) return;

		Point? hitPos = sprite.getCurrentFrame().POIs[0];
		new Anim(
			pos.add(hitPos.Value.times(xDir)), 
			"rock_buster_fade", xDir, damager.owner.getNextActorNetId(), true, true
		);
	}
}
public class SpreadDrillMediumProj : Projectile {
	int hits;
	Point addPos;
	Actor ownChr = null!;
	float projSpeed = 150;

	public SpreadDrillMediumProj(
		Actor owner, Point pos, int xDir, ushort? netProjId,
		bool upOrDown, bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "spread_drill_medium_proj", netProjId, altPlayer
	) {
		maxTime = 40f / 60f;
		projId = (int)BassProjIds.SpreadDrillMid;
		destroyOnHit = false;

		vel.x = projSpeed * xDir;
		yDir = upOrDown ? -1 : 1;
		damager.damage = 1;
		damager.hitCooldown = 30;
		ownChr = owner;

		addPos = new Point(-12 * xDir, 0 * yDir);
		canBeLocal = false;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netProjId, xDir, new byte[] {(byte)(yDir == -1 ? 1 : 0)});
		}

		//projId = (int)BassProjIds.SpreadDrill;
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new SpreadDrillMediumProj(
			arg.owner, arg.pos, arg.xDir ,arg.netId,
			arg.extraData[0] == 1, altPlayer: arg.player
		);
	}

	public override void update() {
		base.update();
		if (ownedByLocalPlayer) {
			if (owner.input.isPressed(Control.Shoot, owner) && (ownChr as Character)?.currentWeapon is SpreadDrill) {
				new SpreadDrillSmallProj(ownChr, pos, xDir, owner.getNextActorNetId(), true, rpc: true);
				new SpreadDrillSmallProj(ownChr, pos, xDir, owner.getNextActorNetId(), false, rpc: true);
				destroySelf(doRpcEvenIfNotOwned: true);
				return;
			}
		}
		if (time < 0.2f && hits == 0) move(new Point(0, yDir * 120));

		if (hits >= 3) destroySelfNoEffect(true, true);
		
		if (Math.Abs(vel.x) < projSpeed) vel.x += Global.speedMul * xDir * 8;
		else if (Math.Abs(vel.x) > projSpeed) vel.x = speed * xDir;
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		if (!ownedByLocalPlayer) return;
		
		forceNetUpdateNextFrame = true;
		time = 0;
	}

	public override void onDamageEX(IDamagable damagable) {
		base.onDamageEX(damagable);
		if (!ownedByLocalPlayer) return;

		hits++;
		playSound("spreaddrillHit", true);
		vel.x = xDir * -90;
		Point? hitPos = sprite.getCurrentFrame().POIs[0];
		new Anim(
			pos.add(hitPos.Value.times(xDir)), 
			"rock_buster_fade", xDir, damager.owner.getNextActorNetId(), true, true
		);
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
			ttl = 2, useGravity = true, vel = Point.random(0, -50, 0, -50), frameIndex = 0, frameSpeed = 0,
			gravityModifier = 0.66f, blink = true
		};
		new Anim(pos, "spread_drill_medium_pieces", xDir, null, false) {
			ttl = 2, useGravity = true, vel = Point.random(0, 150, 0, -50), frameIndex = 1, frameSpeed = 0,
			gravityModifier = 0.66f, blink = true
		};
	}
}
public class SpreadDrillSmallProj : Projectile {
	int hits;
	Point addPos;
	float projSpeed = 200;
	public SpreadDrillSmallProj(
		Actor owner, Point pos, int xDir, ushort? netProjId, 
		bool upOrDown, bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "spread_drill_small_proj", netProjId, altPlayer
	) {
		maxTime = 0.5f;
		projId = (int)BassProjIds.SpreadDrillSmall;
		destroyOnHit = false;

		vel.x = projSpeed * xDir;
		yDir = upOrDown ? -1 : 1;
		damager.damage = 1;
		damager.hitCooldown = 15f;

		addPos = new Point(-6 * xDir, 1 * yDir);
		canBeLocal = false;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netProjId, xDir, new byte[] {(byte)(yDir == -1 ? 1 : 0)});
		}
		//projId = (int)BassProjIds.SpreadDrill;
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new SpreadDrillSmallProj(
			arg.owner, arg.pos, arg.xDir, arg.netId, 
			arg.extraData[0] == 1, altPlayer: arg.player
		);
	}

	public override void update() {
		base.update();

		if (time < 0.1f && hits == 0) move(new Point(0, yDir * 120));

		if (hits >= 3) destroySelf();

		if (Math.Abs(vel.x) < projSpeed) vel.x += Global.speedMul * xDir * 16;
		else if (Math.Abs(vel.x) > projSpeed) vel.x = speed * xDir;
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		if (!ownedByLocalPlayer) return;
		
		forceNetUpdateNextFrame = true;
		time = 0;
	}

	public override void onDamageEX(IDamagable damagable) {
		base.onDamageEX(damagable);
		if (!ownedByLocalPlayer) return;

		hits++;
		playSound("spreaddrillHit", true);
		vel.x = xDir * -120;
		yPushVel = yDir * 60;
		Point? hitPos = sprite.getCurrentFrame().POIs[0];
		new Anim(
			pos.add(hitPos.Value.times(xDir)), 
			"rock_buster_fade", xDir, damager.owner.getNextActorNetId(), true, true
		);
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
			ttl = 2, useGravity = true, vel = Point.random(0, -50, 0, -50), frameIndex = 0, frameSpeed = 0,
			blink = true
		};
		new Anim(pos, "spread_drill_small_pieces", xDir, null, false) {
			ttl = 2, useGravity = true, vel = Point.random(0, 150, 0, -50), frameIndex = 1, frameSpeed = 0,
			blink = true
		};
	}
}
