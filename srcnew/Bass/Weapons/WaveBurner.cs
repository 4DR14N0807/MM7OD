using System;
using System.Xml;

namespace MMXOnline;

public class WaveBurner : Weapon {
	public static WaveBurner netWeapon = new();
	public float shootAngle;
	public float angleMod = 1;
	public float bloomTimer = 0;

	public WaveBurner() : base() {
		iconSprite = "hud_weapon_icon_bass";
		index = (int)BassWeaponIds.WaveBurner;
		displayName = "WAVE BURNER";
		weaponSlotIndex = index;
		weaponBarBaseIndex = index;
		weaponBarIndex = index;
		fireRate = 4;
		isStream = true;
		maxAmmo = 24 * 9;
		ammo = maxAmmo;
		allowSmallBar = false;
		ammoDisplayScale = 9;
		ammoGainMultiplier = ammoDisplayScale;
		drawCooldown = false;
		soundTime = 6;
		descriptionV2 = [
			[ "Short range DPS weapon.\n" +
			"Can be aimed in 8 directions."]
		];
	}

	public override void charLinkedUpdate(Character character, bool isAlwaysOn) {
		base.charLinkedUpdate(character, isAlwaysOn);

		if (shootAngle != 0 && character.currentWeapon != this ||
			!character.player.input.isHeld(Control.Shoot, character.player)
		) {
			bloomTimer -= character.speedMul;
			if (bloomTimer <= 0) {
				shootAngle = 0;
				angleMod *= -1;
			}
		}
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Point shootPos = character.getShootPos();
		Player player = character.player;
		Bass bass = character as Bass ?? throw new NullReferenceException();
		float shootAngle = bass.getShootAngle(true, true);
		bloomTimer = 6;

		if (character.isUnderwater()) {
			new WaveBurnerUnderwaterProj(
				bass, shootPos, shootAngle,
				player.getNextActorNetId(), true
			);

			Point pushVel = Point.createFromByteAngle(shootAngle + 128).times(30);
			bass.xPushVel = pushVel.x;
			bass.yPushVel = pushVel.y;

		} else {
			new WaveBurnerProj(
				bass, shootPos, shootAngle + this.shootAngle,
				player.getNextActorNetId(), true, player
			);
			this.shootAngle += 3 * angleMod;
			if (Math.Abs(this.shootAngle) >= 16) {
				angleMod *= -1;
				this.shootAngle = 16 * MathF.Sign(this.shootAngle);
			}

			if (soundTime <= 0) {
				soundTime = 4;
				bass.playSound("waveburnerLoop", sendRpc: true);
			}
		}
	}
}


public class WaveBurnerProj : Projectile {
	public bool inWater;

	public WaveBurnerProj(
		Actor owner, Point pos, float byteAngle, ushort? netProjId, 
		bool rpc = false, Player? altPlayer = null
	) : base(
		pos, 1, owner, "wave_burner_proj", netProjId, altPlayer 
	) {
		damager.damage = 0.55f;
		damager.hitCooldown = 8;
		maxUHitCount = 2;

		projId = (int)BassProjIds.WaveBurner;
		maxTime = 0.4f;
		destroyOnHit = false;
		vel = Point.createFromByteAngle(byteAngle) * 5f * 60;

		if (byteAngle > 64 && byteAngle < 192) {
			xDir = -1;
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
		if (!inWater && isUnderwater()) {
			reduceSpeed();
			new BubbleAnim(pos, "bubbles") { vel = new Point(0, -60) };
			Global.level.delayedActions.Add(
				new DelayedAction(() => { new BubbleAnim(pos, "bubbles_small") { vel = new Point(0, -60) }; }, 0.1f)
			);
		}
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		reduceSpeed();
	}

	public void reduceSpeed() {
		if (inWater) {
			return;
		}
		vel *= 0.6f;
		inWater = true;
	}

	public override void render(float x, float y) {
		base.render(x, y);

		float savedAlpha = alpha;
		long savedZindex = zIndex;
		zIndex = ZIndex.Background;
		alpha = savedAlpha * 0.25f;
		base.render(x + (-moveDelta.x * 3), y + (-moveDelta.y * 3));
		alpha = savedAlpha * 0.5f;
		base.render(x + (-moveDelta.x * 1.5f), y + (-moveDelta.y * 1.5f));
		alpha = savedAlpha;
		zIndex = savedZindex;
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
}
