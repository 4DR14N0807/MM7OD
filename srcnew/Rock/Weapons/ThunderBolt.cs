using System;
using System.Collections.Generic;

namespace MMXOnline;

public class ThunderBolt : Weapon {
	public static ThunderBolt netWeapon = new();

	public ThunderBolt() : base() {
		index = (int)RockWeaponIds.ThunderBolt;
		fireRate = 40;
		weaponSlotIndex = (int)RockWeaponSlotIds.ThunderBolt;
		weaponBarBaseIndex = (int)RockWeaponBarIds.ThunderBolt;
		weaponBarIndex = weaponBarBaseIndex;
		killFeedIndex = 0;
		maxAmmo = 24;
		ammo = maxAmmo;
		description = new string[] { "Powerful DPS weapon.", "Divides in 2 when hitting an enemy." };
	}

	public override void shootRock(Rock rock, params int[] args) {
		base.shootRock(rock, args);
		Point shootPos = rock.getShootPos();
		int xDir = rock.getShootXDir();
		Player player = rock.player;
		ushort netId = player.getNextActorNetId();

		new ThunderBoltProj(rock, shootPos, xDir, netId, 0);
		rock.playSound("thunder_bolt", sendRpc: true);
	}
}
public class ThunderBoltProj : Projectile {
	public int type = 0;
	float projSpeed = 300;
	Actor ownChr = null!;

	public ThunderBoltProj(
		Actor owner, Point pos, int xDir, ushort? netProjId, 
		int type, bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "thunder_bolt_start", netProjId, altPlayer
	) {

		projId = (int)RockProjIds.ThunderBolt;
		maxTime = 0.45f;
		fadeSound = "thunder_bolt_hit";
		fadeSprite = "thunder_bolt_fade2";
		fadeOnAutoDestroy = true;
		this.type = type;
		damager.damage = 2;
		damager.hitCooldown = 30;
		ownChr = owner;

		if (type == 1) {
			base.vel.x = projSpeed * xDir;
			var sprite = "thunder_bolt_proj";
			changeSprite(sprite, true);
		}

		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type };

			rpcCreate(pos, owner, ownerPlayer, netProjId, xDir, extraArgs);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new ThunderBoltProj(
			arg.owner, arg.pos, arg.xDir, arg.netId, 
			arg.extraData[0], altPlayer: arg.player
		);
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;

		if (type == 0) {
			if (isAnimOver()) {
				new ThunderBoltProj(ownChr, pos, xDir, damager.owner.getNextActorNetId(true), 1, rpc: true);
				destroySelfNoEffect(disableRpc: true, true);
			}
		}
	}

	public void onHit() {
		if (!ownedByLocalPlayer) {
			destroySelfNoEffect();
			return;
		}
		if (type == 1) {
			new ThunderBoltSplitProj(ownChr, pos, xDir, damager.owner.getNextActorNetId(true), 0, true);
		}
	}


	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		onHit();
		destroySelf();
	}
}

public class ThunderBoltSplitProj : Projectile {
	public int type = 0;
	public float sparkleTime = 0;
	float projSpeed = 300;
	Actor ownChr = null!;
	public ThunderBoltSplitProj(
		Actor owner, Point pos, int xDir, ushort? netProjId, 
		int type, bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "thunder_bolt_fade", netProjId, altPlayer
	) {

		projId = (int)RockProjIds.ThunderBoltSplit;
		maxTime = 0.75f;
		this.type = type;
		destroyOnHit = false;
		damager.damage = 2;
		damager.flinch = Global.miniFlinch;
		damager.hitCooldown = 30;
		ownChr = owner;

		if (type >= 1) {
			var sprite = "thunder_bolt_divide_proj";
			changeSprite(sprite, false);
			vel.y = -projSpeed;

			if (type == 2) {
				yDir *= -1;
				vel.y *= -1;
			}
		}

		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type };

			rpcCreate(pos, owner, ownerPlayer, netProjId, xDir, extraArgs);
		}
		projId = (int)RockProjIds.ThunderBolt;
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new ThunderBoltSplitProj(
			arg.owner, arg.pos, arg.xDir, arg.netId, 
			arg.extraData[0], altPlayer: arg.player
		);
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;
		if (type != 0) {
			sparkleTime += Global.spf;
			if (sparkleTime > 0.05) {
				sparkleTime = 0;
				new Anim(pos, "thunder_bolt_divide_trail", 1, damager.owner.getNextActorNetId(), true, true);
			}
		}
		if (type == 0) {
			if (isAnimOver()) {
				new ThunderBoltSplitProj(ownChr, pos.addxy(0, -24), xDir, damager.owner.getNextActorNetId(true), 1, rpc: true);
				new ThunderBoltSplitProj(ownChr, pos.addxy(0, 24), xDir, damager.owner.getNextActorNetId(true), 2, rpc: true);
				destroySelfNoEffect();
			}
		}
	}
}
