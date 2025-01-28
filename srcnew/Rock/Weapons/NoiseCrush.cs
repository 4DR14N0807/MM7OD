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

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Point shootPos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;
		int chargeLevel = args[0];
		bool charged = args[1] == 1;

		if (player.character is Rock rock) {

				if (charged) {
					character.playSound("noise_crush_charged");
					new NoiseCrushChargedProj(shootPos, xDir, player, 0, player.getNextActorNetId(), true);
					new NoiseCrushChargedProj(shootPos.addxy(6 * -xDir, 0), xDir, player, 0, player.getNextActorNetId(), true);
					new NoiseCrushChargedProj(shootPos.addxy(12 * -xDir, 0), xDir, player, 1, player.getNextActorNetId(), true);
					new NoiseCrushChargedProj(shootPos.addxy(18 * -xDir, 0), xDir, player, 2, player.getNextActorNetId(), true);
					new NoiseCrushChargedProj(shootPos.addxy(24 * -xDir, 0), xDir, player, 3, player.getNextActorNetId(), true);
					rock.hasChargedNoiseCrush = false;
					rock.noiseCrushAnimTime = 0;
				} else {
					new NoiseCrushProj(shootPos, xDir, player, 0, player.getNextActorNetId(), true, true);
					new NoiseCrushProj(shootPos.addxy(4 * -xDir, 0), xDir, player, 0, player.getNextActorNetId(true), rpc: true);
					new NoiseCrushProj(shootPos.addxy(8 * -xDir, 0), xDir, player, 1, player.getNextActorNetId(true), rpc: true);
					new NoiseCrushProj(shootPos.addxy(12 * -xDir, 0), xDir, player, 1, player.getNextActorNetId(true), rpc: true);
					new NoiseCrushProj(shootPos.addxy(16 * -xDir, 0), xDir, player, 2, player.getNextActorNetId(true), rpc: true);
					character.playSound("noise_crush", sendRpc: true);
					addAmmo(-1, player);
				}
			}
	}
}


public class NoiseCrushProj : Projectile {

	public int type;
	public int bounces = 0;
	public bool isMain;

	public NoiseCrushProj(
		Point pos, int xDir, Player player, 
		int type, ushort netProjId,
		bool isMain = false, bool rpc = false
	) : base(
		NoiseCrush.netWeapon, pos, xDir, 240, 1,
		player, "noise_crush_top", 0, 0.2f,
		netProjId, player.ownedByLocalPlayer
	) {

		projId = (int)RockProjIds.NoiseCrush;
		maxTime = 0.75f;
		this.type = type;
		this.isMain = isMain;
		//improve fade sprite
		fadeSprite = "noise_crush_fade";
		fadeOnAutoDestroy = true;
		canBeLocal = false;

		if (type == 1) changeSprite("noise_crush_middle", true);
		else if (type == 2) {
			changeSprite("noise_crush_bottom", true);
		}
		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type };

			rpcCreate(pos, player, netProjId, xDir, extraArgs);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new NoiseCrushProj(
			arg.pos, arg.xDir, arg.player, arg.extraData[0], arg.netId
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
		Point pos, int xDir, Player player,
		int type, ushort netProjId, bool rpc = false
	) : base(
		NoiseCrush.netWeapon, pos, xDir, 240, 3,
		player, "noise_crush_charged_top", 0, 0.33f,
		netProjId, player.ownedByLocalPlayer
	) {

		projId = (int)RockProjIds.NoiseCrushCharged;
		maxTime = 1f;
		this.type = type;
		fadeSprite = "noise_crush_fade";

		if (type == 1) changeSprite("noise_crush_charged_middle", true);
		else if (type == 2) changeSprite("noise_crush_charged_middle2", true);
		else if (type == 3) {
			changeSprite("noise_crush_charged_bottom", true);
		}
		
		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type };

			rpcCreate(pos, player, netProjId, xDir, extraArgs);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new NoiseCrushChargedProj(
			arg.pos, arg.xDir, arg.player, arg.extraData[0], arg.netId
		);
	}
}
