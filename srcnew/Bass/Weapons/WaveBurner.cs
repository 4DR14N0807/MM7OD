using System;
using System.Xml;

namespace MMXOnline;

public class WaveBurner : Weapon {
	public static WaveBurner netWeapon = new();

	public WaveBurner() : base() {
		iconSprite = "hud_weapon_icon_bass";
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
		ammoDisplayScale = 9;
		ammoGainMultiplier = ammoDisplayScale;
		drawCooldown = false;
		soundTime = 6;
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Point shootPos = character.getShootPos();
		Player player = character.player;
		Bass bass = character as Bass ?? throw new NullReferenceException();
		float shootAngle = bass.getShootAngle(true, true);

		if (character.isUnderwater()) {
			new WaveBurnerUnderwaterProj(
				bass, shootPos, shootAngle,
				player.getNextActorNetId(), true
			);

			Point pushVel = Point.createFromByteAngle(shootAngle + 128).times(30);
			bass.xPushVel = pushVel.x;
			bass.yPushVel = pushVel.y;

		} else {
			
			new WaveBurnerProj(bass, shootPos, shootAngle + bass.wBurnerAngle, player.getNextActorNetId(), true, player);
			bass.wBurnerAngle += 8 * bass.wBurnerAngleMod;
			if (Math.Abs(bass.wBurnerAngle) > 32) {
				bass.wBurnerAngleMod *= -1;
				bass.wBurnerAngle = 32 * MathF.Sign(bass.wBurnerAngle);
			}

			if (soundTime <= 0) {
				soundTime = 4;
				bass.playSound("waveburnerLoop", sendRpc: true);
			}
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
			rpcCreateByteAngle(pos, owner, ownerPlayer, netId, byteAngle);
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
	Point bubble1Spd;
	Anim? bubble2;
	Point bubble2Spd;
	float spd = 240;
	public WaveBurnerUnderwaterProj(
		Actor owner, Point pos, float byteAngle, ushort? netProjId, 
		bool rpc = false, Player? altPlayer = null
	) : base(
		pos, 1, owner, "wave_burner_underwater_proj", netProjId, altPlayer
	) {
		projId = (int)BassProjIds.WaveBurnerUnderwater;
		maxTime = 0.33f;
		destroyOnHit = false;
		rand = Helpers.randomRange(1, 4);

		vel = Point.createFromByteAngle(byteAngle) * spd;
		this.byteAngle = byteAngle;
		damager.damage = 1;
		damager.hitCooldown = 24;

		bubble1Spd = Point.createFromByteAngle(byteAngle + (Helpers.randomRange(-12, 12))).times(spd);
		bubble2Spd = Point.createFromByteAngle(byteAngle + (Helpers.randomRange(-12, 12))).times(spd);

		if (rpc) {
			rpcCreateByteAngle(pos, owner, ownerPlayer, netProjId, byteAngle);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new WaveBurnerUnderwaterProj(
			arg.owner, arg.pos, arg.byteAngle, arg.netId, altPlayer: arg.player
		);
	}

	public override void onStart() {
		base.onStart();
		if (ownedByLocalPlayer) {
			if (rand % 2 == 0) {
				bubble1 = new BubbleAnim(
					pos.addxy(Helpers.randomRange(-2, 2), Helpers.randomRange(-6, 6)),
					"wave_burner_underwater_bubble", damager.owner.getNextActorNetId()
				);
					bubble1.xDir = xDir;
					bubble1.vel = bubble1Spd;
					bubble1.frameSpeed = 0;
					bubble1.ttl = 0.5f;
			}
			if (rand > 2) {
				bubble2 = new BubbleAnim(
					pos.addxy(Helpers.randomRange(-2, 2), Helpers.randomRange(-6, 6)),
					"wave_burner_underwater_bubble", damager.owner.getNextActorNetId()
				);
			
				bubble2.xDir = xDir;
				bubble2.vel = bubble2Spd;
				bubble2.frameSpeed = 0;
				bubble2.frameIndex = 1;
				bubble2.ttl = 0.5f;
			}
		}
	}

	public override void update() {
		base.update();
		if (!isUnderwater()) destroySelf();
	}
}
