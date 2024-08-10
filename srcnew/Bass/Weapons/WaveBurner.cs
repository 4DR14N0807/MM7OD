using System;
using System.Xml;

namespace MMXOnline;

public class WaveBurner : Weapon {

	public static WaveBurner netWeapon = new();

	public WaveBurner() : base() {
		index = (int)BassWeaponIds.WaveBurner;
		displayName = "WAVE BURNER";
		weaponSlotIndex = index;
		weaponBarBaseIndex = index;
		weaponBarIndex = index;
		fireRateFrames = 4;
		isStream = true;
		maxAmmo = 168;
		ammo = maxAmmo;
		allowSmallBar = false;
		ammoDisplayScale = 6;
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Point shootPos = character.getShootPos();
		Player player = character.player;
		Bass? bass = character as Bass ?? throw new NullReferenceException();

		if (character.isUnderwater()) {
			new WaveBurnerUnderwaterProj(
				shootPos, character.getShootXDir(),
				player, player.getNextActorNetId(), true
			);
		} else {
			float shootAngle = bass.getShootAngle(true, true) + bass.wBurnerAngle;
			new WaveBurnerProj(shootPos, shootAngle, player, player.getNextActorNetId(), true);
			bass.wBurnerAngle += 8 * bass.wBurnerAngleMod;
			if (Math.Abs(bass.wBurnerAngle) > 48) bass.wBurnerAngleMod *= -1;
		}
	}
}


public class WaveBurnerProj : Projectile {
	public WaveBurnerProj(
		Point pos, float byteAngle, Player player,
		ushort? netProjId, bool rpc = false
	) : base(
		WaveBurner.netWeapon, pos, 1, 0, 1,
		player, "wave_burner_proj", 0, 0.25f, 
		netProjId, player.ownedByLocalPlayer 
	) {
		projId = (int)BassProjIds.WaveBurner;
		maxTime = 0.2f;
		//this.byteAngle = byteAngle;
		vel = Point.createFromByteAngle(byteAngle) * 240;
		xDir = player.character.getShootXDir();
		canBeLocal = false;

		if (rpc) {
			rpcCreateByteAngle(pos, player, netId, byteAngle);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new WaveBurnerProj(
			arg.pos, arg.byteAngle, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();
		checkUnderwater();
	}

	public void checkUnderwater() {
		if (isUnderwater()) {
			new BubbleAnim(pos, "bubbles") { vel = new Point(0, -60) };
			Global.level.delayedActions.Add(new DelayedAction(() => { new BubbleAnim(pos, "bubbles_small") { vel = new Point(0, -60) }; }, 0.1f));
			destroySelf();
		}
	}
}


public class WaveBurnerUnderwaterProj : Projectile {

	int rand;
	Anim? bubble1;
	Anim? bubble2;
	public WaveBurnerUnderwaterProj(
		Point pos, int xDir, Player player,
		ushort? netProjId, bool rpc = false
	) : base(
		WaveBurner.netWeapon, pos, xDir, 240, 0,
		player, "wave_burner_underwater_proj", 0, 0.4f, 
		netProjId, player.ownedByLocalPlayer 
	) {
		projId = (int)BassProjIds.WaveBurnerUnderwater;
		maxTime = 0.33f;
		destroyOnHit = false;
		rand = Helpers.randomRange(1, 4);
		
		if (rand % 2 == 0) {
			bubble1 = new Anim(pos.addxy(Helpers.randomRange(-2, 2), Helpers.randomRange(-6, 6)), 
				"wave_burner_underwater_bubble", xDir, player.getNextActorNetId(), false, true)
				{ vel = new Point(speed * xDir, 0)};
			
			bubble1.frameSpeed = 0;
		}
		if (rand > 2) {
			bubble2 = new Anim(pos.addxy(Helpers.randomRange(-2, 2), Helpers.randomRange(-6, 6)), 
				"wave_burner_underwater_bubble", xDir, player.getNextActorNetId(), false, true)
				{ vel = new Point(speed * xDir, 0)};
			
			bubble2.frameSpeed = 0;
			bubble2.frameIndex = 1;
		}

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new WaveBurnerUnderwaterProj(
			arg.pos, arg.xDir, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();

		if (bubble1 != null && bubble1.time >= 0.2f) {
			int dirMod = Helpers.randomRange(-1, 1);
			int velMod = Helpers.randomRange(1, 2);
			bubble1.vel.y += velMod * dirMod;
			bubble1.vel.x -= xDir * 4;
		}

		if (bubble2 != null && bubble2.time >= 0.2f) {
			int dirMod = Helpers.randomRange(-1, 1);
			int velMod = Helpers.randomRange(1, 2);
			bubble2.vel.y += velMod * dirMod;
			bubble2.vel.x -= xDir * 4;
		}

		if (bubble1?.time >= maxTime) bubble1.destroySelf();
		if (bubble2?.time >= maxTime) bubble2.destroySelf();
	}


	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		var chr = damagable as Character;

		if (chr != null) {
			chr.xPushVel = xDir * 180;
		} 
	}
}
