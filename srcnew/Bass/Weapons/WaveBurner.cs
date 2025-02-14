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
		fireRate = 4;
		isStream = true;
		maxAmmo = 168;
		ammo = maxAmmo;
		allowSmallBar = false;
		ammoDisplayScale = 6;
		ammoGainMultiplier = ammoDisplayScale;
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Point shootPos = character.getShootPos();
		Player player = character.player;
		Bass bass = character as Bass ?? throw new NullReferenceException();

		if (character.isUnderwater()) {
			new WaveBurnerUnderwaterProj(
				bass, shootPos, character.getShootXDir(),
				player.getNextActorNetId(), true
			);
		} else {
			float shootAngle = bass.getShootAngle(true, true) + bass.wBurnerAngle;
			new WaveBurnerProj(bass, shootPos, shootAngle, player.getNextActorNetId(), true, player);
			bass.wBurnerAngle += 8 * bass.wBurnerAngleMod;
			if (Math.Abs(bass.wBurnerAngle) > 32) {
				bass.wBurnerAngleMod *= -1;
				bass.wBurnerAngle = 32 * MathF.Sign(bass.wBurnerAngle);
			}
			if (bass.wBurnerSound == null) {
				bass.wBurnerSound = new("waveburnerLoop", "waveburnerEnd", "waveburnerLoop", bass);
			};
			bass.wBurnerSound.play();
		}
	}
}


public class WaveBurnerProj : Projectile {
	Character? character;

	public WaveBurnerProj(
		Actor owner, Point pos, float byteAngle, ushort? netProjId, 
		bool rpc = false, Player? altPlayer = null
	) : base(
		pos, 1, owner, "wave_burner_proj", netProjId, altPlayer 
	) {
		projId = (int)BassProjIds.WaveBurner;
		maxTime = 0.2f;
		destroyOnHit = false;
		vel = Point.createFromByteAngle(byteAngle) * 240;

		damager.damage = 1;
		damager.hitCooldown = 12;

		if (ownedByLocalPlayer) {
			character = ownerPlayer.character;
			if (character != null) {
				xDir = character.getShootXDir();
			}
		}

		if (rpc) {
			rpcCreateByteAngle(pos, ownerPlayer, netId, byteAngle);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new WaveBurnerProj(
			arg.owner, arg.pos, arg.byteAngle, arg.netId, altPlayer: arg.player
		);
	}

	public override void update() {
		base.update();
		checkUnderwater();
	}

	public void checkUnderwater() {
		if (!ownedByLocalPlayer) return;
		if (isUnderwater()) {
			new BubbleAnim(pos, "bubbles") { vel = new Point(0, -60) };
			Global.level.delayedActions.Add(
				new DelayedAction(() => { new BubbleAnim(pos, "bubbles_small") { vel = new Point(0, -60) }; }, 0.1f)
			);
			destroySelf();
		}
	}
}

public class WaveBurnerUnderwaterProj : Projectile {
	int rand;
	Anim? bubble1;
	Anim? bubble2;
	public WaveBurnerUnderwaterProj(
		Actor owner, Point pos, int xDir, ushort? netProjId, 
		bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "wave_burner_underwater_proj", netProjId, altPlayer
	) {
		projId = (int)BassProjIds.WaveBurnerUnderwater;
		maxTime = 0.33f;
		destroyOnHit = false;
		rand = Helpers.randomRange(1, 4);

		vel.x = 240 * xDir;
		damager.hitCooldown = 24;

		if (ownedByLocalPlayer) {
			if (rand % 2 == 0) {
				bubble1 = new Anim(pos.addxy(Helpers.randomRange(-2, 2), Helpers.randomRange(-6, 6)), 
					"wave_burner_underwater_bubble", xDir, ownerPlayer.getNextActorNetId(), false, true)
					{ vel = new Point(speed * xDir, 0)};
			
					bubble1.frameSpeed = 0;
				}
			if (rand > 2) {
				bubble2 = new Anim(pos.addxy(Helpers.randomRange(-2, 2), Helpers.randomRange(-6, 6)), 
					"wave_burner_underwater_bubble", xDir, ownerPlayer.getNextActorNetId(), false, true)
					{ vel = new Point(speed * xDir, 0)};
			
				bubble2.frameSpeed = 0;
				bubble2.frameIndex = 1;
			}
		}


		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netProjId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new WaveBurnerUnderwaterProj(
			arg.owner, arg.pos, arg.xDir, arg.netId, altPlayer: arg.player
		);
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;

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
}
