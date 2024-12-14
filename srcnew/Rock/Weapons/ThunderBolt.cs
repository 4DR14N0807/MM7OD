using System;
using System.Collections.Generic;

namespace MMXOnline;

public class ThunderBolt : Weapon {

	public static ThunderBolt netWeapon = new ThunderBolt();

	public ThunderBolt() : base() {
		index = (int)RockWeaponIds.ThunderBolt;
		fireRate = 40;
		weaponSlotIndex = (int)RockWeaponSlotIds.ThunderBolt;
		weaponBarBaseIndex = (int)RockWeaponBarIds.ThunderBolt;
		weaponBarIndex = weaponBarBaseIndex;
		killFeedIndex = 0;
		maxAmmo = 20;
		ammo = maxAmmo;
		description = new string[] { "Powerful DPS weapon.", "Divides in 2 when hitting an enemy." };
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Point shootPos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;

		new ThunderBoltProj(shootPos, xDir, player, 0, player.getNextActorNetId());
		character.playSound("thunder_bolt", sendRpc: true);
	}
}
public class ThunderBoltProj : Projectile {
	public int type = 0;

	public Character? hitChar;
	float projSpeed = 300;

	public ThunderBoltProj(
		Point pos, int xDir, Player player, int type, ushort netProjId,
		Character? hitChar = null, bool rpc = false
	) : base(
		ThunderBolt.netWeapon, pos, xDir, 0, 2,
		player, "thunder_bolt_start", 0, 0.5f,
		netProjId, player.ownedByLocalPlayer
	) {

		projId = (int)RockProjIds.ThunderBolt;
		maxTime = 0.45f;
		fadeSound = "thunder_bolt_hit";
		fadeOnAutoDestroy = true;
		this.hitChar = hitChar;
		fadeSprite = "thunder_bolt_fade2";
		this.type = type;

		if (type == 1) {
			vel.x = projSpeed * xDir;
			var sprite = "thunder_bolt_proj";
			changeSprite(sprite, false);
		}

		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type };

			rpcCreate(pos, player, netProjId, xDir, extraArgs);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new ThunderBoltProj(
			arg.pos, arg.xDir, arg.player, arg.extraData[0], arg.netId
		);
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;

		if (type == 0) {
			Character? chr = null;
			if (isAnimOver()) {
				time = 0;
				new ThunderBoltProj(pos.clone(), xDir, damager.owner, 1, damager.owner.getNextActorNetId(true), chr, rpc: true);
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
			new ThunderBoltSplitProj(pos.clone(), xDir, damager.owner, 0, damager.owner.getNextActorNetId(true), true);
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
	Player player;
	public ThunderBoltSplitProj(
		Point pos, int xDir, Player player, int type,
		ushort netProjId, bool rpc = false
	) : base(
	ThunderBolt.netWeapon, pos, xDir, 0, 2,
	player, "thunder_bolt_fade", 4, 0.5f,
	netProjId, player.ownedByLocalPlayer
	) {

		projId = (int)RockProjIds.ThunderBoltSplit;
		maxTime = 0.75f;
		this.type = type;
		destroyOnHit = false;
		this.player = player;

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

			rpcCreate(pos, player, netProjId, xDir, extraArgs);
		}
		projId = (int)RockProjIds.ThunderBolt;
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new ThunderBoltSplitProj(
			arg.pos, arg.xDir, arg.player, arg.extraData[0], arg.netId
		);
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;
		if (type != 0) {
			sparkleTime += Global.spf;
			if (sparkleTime > 0.05) {
				sparkleTime = 0;
				new Anim(pos, "thunder_bolt_divide_trail", 1, player.getNextActorNetId(), true, true);
			}
		}
		if (type == 0) {
			if (isAnimOver()) {
				new ThunderBoltSplitProj(pos.addxy(0, -24), xDir, damager.owner, 1, damager.owner.getNextActorNetId(true), rpc: true);
				new ThunderBoltSplitProj(pos.addxy(0, 24), xDir, damager.owner, 2, damager.owner.getNextActorNetId(true), rpc: true);
				destroySelfNoEffect();
			}
		}
	}
}
