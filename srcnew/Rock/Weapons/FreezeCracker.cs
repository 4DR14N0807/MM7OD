using System;
using System.Collections.Generic;

namespace MMXOnline;

public class FreezeCracker : Weapon {
	public static FreezeCracker netWeapon = new();

	public FreezeCracker() : base() {
		displayName = "FREEZE CRACKER";
		index = (int)RockWeaponIds.FreezeCracker;
		killFeedIndex = 0;
		weaponBarBaseIndex = (int)RockWeaponBarIds.FreezeCracker;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = (int)RockWeaponSlotIds.FreezeCracker;
		fireRate = 45;
		maxAmmo = 20;
		ammo = maxAmmo;
		descriptionV2 = [
			[ "Splits in 6 pieces when hitting a wall.\n" + "Can be aimed up or down." ]
		];
	}

	public override void shootRock(Rock rock, params int[] args) {
		base.shootRock(rock, args);
		Point shootPos = rock.getShootPos();
		int xDir = rock.getShootXDir();
		Player player = rock.player;
		int input = player.input.getYDir(player);

		new FreezeCrackerRmProj(rock, shootPos, xDir, player.getNextActorNetId(), 0, input);
		rock.playSound("buster2", sendRpc: true);
	}
}


public class FreezeCrackerRmProj : Projectile {
	public int type;
	private bool framgented;
	private bool didSplit;
	int input;
	public float sparkleTime = 0;
	Anim? sparkle;
	float projSpeed = 300;

	public FreezeCrackerRmProj(
		Actor owner, Point pos, int xDir, ushort? netProjId, 
		int type, int input = 0, bool sendRpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "freeze_cracker_start", netProjId, altPlayer
	) {
		projId = (int)RockProjIds.FreezeCracker;
		maxTime = 0.6f;
		fadeSprite = "freeze_cracker_start";
		this.type = type;
		this.input = input;
		damager.damage = 2;
		damager.hitCooldown = 6;

		if (type == 1) {
			canBeLocal = false;
			changeSprite("freeze_cracker_proj", false);
			reflectable = true;
			destroyOnHit = true;
			int dir = input * 32;
			float ang = xDir > 0 ? dir : -dir + 128;
			vel = Point.createFromByteAngle(ang) * projSpeed;
		}

		if (sendRpc) {
			rpcCreate(pos, owner, ownerPlayer, netProjId, xDir, [(byte)type ]);
		}
	}

	public override void update() {
		base.update();

		sparkleTime += Global.speedMul;
		if (sparkleTime >= 3) {
			sparkleTime = 0;

			if (ownedByLocalPlayer) sparkle = new Anim(
				pos, "freeze_cracker_sparkles", 1, ownerPlayer.getNextActorNetId(), true
			) { useGravity = true, gravityModifier = 0.5f };

		}

		if (type == 0 && isAnimOver() && ownedByLocalPlayer && ownerActor != null) {
			time = 0;
			new FreezeCrackerRmProj(
				ownerActor, pos, xDir, ownerPlayer.getNextActorNetId(true), 1, input, sendRpc: true
			);
			destroySelfNoEffect();
		}
	}

	public void onHit() {
		if (!ownedByLocalPlayer || didSplit) {
			return;
		}
		didSplit = true;
		

	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		if (!ownedByLocalPlayer ||
			other.gameObject.collider == null ||
			!other.gameObject.collider.isClimbable ||
			destroyed
		) {
			return;
		}
		onHit();
		playSound("ding", true, true);

		if (ownerActor == null) {
			return;
		}
		for (int i = 0; i < 6; i++) {
			new FreezeCrackerPieceRmProj(
				ownerActor, pos, xDir, ownerPlayer.getNextActorNetId(true), i, rpc: true);
		}

		destroySelf();
	}

	// Do effect only.
	public override void onDestroy() {
		base.onDestroy();
		fragment();
	}

	// Split after dealing damage.
	public override void afterDamage(IDamagable damagable, bool didDamage) {
		base.afterDamage(damagable, didDamage);
		onHit();
	}

	public void fragment() {
		if (!ownedByLocalPlayer || framgented) {
			return;
		}
		framgented = true;
		float fTime = 1.25f;

		new Anim(pos, "freeze_cracker_fragments", xDir, ownerPlayer.getNextActorNetId(), false, true) 
		{ frameSpeed = 0, frameIndex = 0, useGravity = true, vel = new Point(-xDir * 60, -120), ttl = fTime };
		new Anim(pos, "freeze_cracker_fragments", xDir, ownerPlayer.getNextActorNetId(), false, true) 
		{ frameSpeed = 0, frameIndex = 0, useGravity = true, vel = new Point(xDir * 30, -60), ttl = fTime };
		new Anim(pos, "freeze_cracker_fragments", xDir, ownerPlayer.getNextActorNetId(), false, true) 
		{ frameSpeed = 0, frameIndex = 1, useGravity = true, vel = new Point(xDir * 30, -180), ttl = fTime };
		new Anim(pos, "freeze_cracker_fragments", xDir, ownerPlayer.getNextActorNetId(), false, true) 
		{ frameSpeed = 0, frameIndex = 1, useGravity = true, vel = new Point(-xDir * 30, -120), ttl = fTime };
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new FreezeCrackerRmProj(
			arg.owner, arg.pos, arg.xDir, arg.netId, 
			arg.extraData[0], altPlayer: arg.player
		);
	}
}


public class FreezeCrackerPieceRmProj : Projectile {
	public FreezeCrackerPieceRmProj(
		Actor owner, Point pos, int xDir, ushort? netProjId, 
		int type, bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "freeze_cracker_piece", netProjId, altPlayer
	) {

		projId = (int)RockProjIds.FreezeCrackerPiece;
		maxTime = 0.4f;
		reflectable = true;
		damager.damage = 2;
		damager.hitCooldown = 15;

		base.vel = Point.createFromByteAngle(type * 42.5f) * 240;

		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type };
			rpcCreate(pos, owner, ownerPlayer, netProjId, xDir, extraArgs);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new FreezeCrackerPieceRmProj(
			arg.owner, arg.pos, arg.xDir, arg.netId, 
			arg.extraData[0], altPlayer: arg.player
		);
	}
}
