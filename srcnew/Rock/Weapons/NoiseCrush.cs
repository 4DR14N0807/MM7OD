using System;
using System.Collections.Generic;

namespace MMXOnline;

public class NoiseCrush : Weapon {
	public static NoiseCrush netWeapon = new();

	public NoiseCrush() : base() {
		index = (int)RockWeaponIds.NoiseCrush;
		weaponSlotIndex = (int)RockWeaponSlotIds.NoiseCrush;
		weaponBarBaseIndex = (int)RockWeaponBarIds.NoiseCrush;
		weaponBarIndex = weaponBarBaseIndex;
		killFeedIndex = 0;
		maxAmmo = 14;
		ammo = maxAmmo;
		fireRate = 30;
		description = new string[] { "Weak projectile that bounces on walls.", "Catch it to get a stronger shot." };
	}

	public override bool canShoot(int chargeLevel, Player player) {
		Rock? rock = player.character as Rock;
		if (rock != null && rock.hasChargedNoiseCrush) return true;
		return base.canShoot(chargeLevel, player);
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 0;
	}

	public override void shootRock(Rock rock, params int[] args) {
		base.shootRock(rock, args);
		Point shootPos = rock.getShootPos();
		int xDir = rock.getShootXDir();
		Player player = rock.player;
		int chargeLevel = args[0];
		bool charged = args[1] == 1;

		if (charged) {
			rock.playSound("noise_crush_charged");
			new NoiseCrushChargedProj(rock, shootPos, xDir, 0, player.getNextActorNetId(), true);
			new NoiseCrushChargedProj(rock, shootPos.addxy(6 * -xDir, 0), xDir, 0, player.getNextActorNetId(), true);
			new NoiseCrushChargedProj(rock, shootPos.addxy(12 * -xDir, 0), xDir, 1, player.getNextActorNetId(), true);
			new NoiseCrushChargedProj(rock, shootPos.addxy(18 * -xDir, 0), xDir, 2, player.getNextActorNetId(), true);
			new NoiseCrushChargedProj(rock, shootPos.addxy(24 * -xDir, 0), xDir, 3, player.getNextActorNetId(), true);
			rock.hasChargedNoiseCrush = false;
			rock.noiseCrushAnimTime = 0;
		} else {
			new NoiseCrushProj(rock, shootPos, xDir, 0, player.getNextActorNetId(), true, true);
			new NoiseCrushProj(rock, shootPos.addxy(4 * -xDir, 0), xDir, 0, player.getNextActorNetId(true), rpc: true);
			new NoiseCrushProj(rock, shootPos.addxy(8 * -xDir, 0), xDir, 1, player.getNextActorNetId(true), rpc: true);
			new NoiseCrushProj(rock, shootPos.addxy(12 * -xDir, 0), xDir, 1, player.getNextActorNetId(true), rpc: true);
			new NoiseCrushProj(rock, shootPos.addxy(16 * -xDir, 0), xDir, 2, player.getNextActorNetId(true), rpc: true);
			rock.playSound("noise_crush", sendRpc: true);
			addAmmo(-1, player);
		}
	}
}


public class NoiseCrushProj : Projectile {

	public int type;
	public int bounces = 0;
	public bool isMain;

	public NoiseCrushProj(
		Actor owner, Point pos, int xDir, int type, ushort? netProjId,
		bool isMain = false, bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "noise_crush_top", netProjId, altPlayer
	) {

		projId = (int)RockProjIds.NoiseCrush;
		maxTime = 0.75f;
		this.type = type;
		this.isMain = isMain;
		//improve fade sprite
		fadeSprite = "noise_crush_fade";
		fadeOnAutoDestroy = true;
		canBeLocal = false;

		vel.x = 240 * xDir;
		damager.damage = 1;
		damager.hitCooldown = 12;

		if (type == 1) changeSprite("noise_crush_middle", true);
		else if (type == 2) {
			changeSprite("noise_crush_bottom", true);
		}
		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type };

			rpcCreate(pos, owner, ownerPlayer, netProjId, xDir, extraArgs);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new NoiseCrushProj(
			arg.owner, arg.pos, arg.xDir, arg.extraData[0], 
			arg.netId, altPlayer: arg.player
		);
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);

		if (bounces < 4) {

			if (other.isSideWallHit()) {
				vel.x *= -1;
				xDir *= -1;
				incPos(new Point(5 * MathF.Sign(vel.x), 0));
				bounces++;
				time = 0;
			}
		}
	}
}


public class NoiseCrushChargedProj : Projectile {

	public int type;

	public NoiseCrushChargedProj(
		Actor owner, Point pos, int xDir, int type, 
		ushort? netProjId, bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "noise_crush_charged_top", netProjId, altPlayer
	) {

		projId = (int)RockProjIds.NoiseCrushCharged;
		maxTime = 1f;
		this.type = type;
		fadeSprite = "noise_crush_fade";

		vel.x = 240 * xDir;
		damager.damage = 3;
		damager.hitCooldown = 20;

		if (type == 1) changeSprite("noise_crush_charged_middle", true);
		else if (type == 2) changeSprite("noise_crush_charged_middle2", true);
		else if (type == 3) {
			changeSprite("noise_crush_charged_bottom", true);
		}
		
		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type };

			rpcCreate(pos, owner, ownerPlayer, netProjId, xDir, extraArgs);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new NoiseCrushChargedProj(
			arg.owner, arg.pos, arg.xDir, 
			arg.extraData[0], arg.netId, altPlayer: arg.player
		);
	}
}
