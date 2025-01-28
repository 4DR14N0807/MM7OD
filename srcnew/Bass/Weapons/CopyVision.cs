using System;
using System.Collections.Generic;
using SFML.Graphics;
using SFML.Graphics.Glsl;

namespace MMXOnline;

public class CopyVision : Weapon {
	public static CopyVision netWeapon = new();

	public CopyVision() : base() {
		index = (int)BassWeaponIds.CopyVision;
		displayName = "COPY VISION";
		weaponSlotIndex = index;
		weaponBarBaseIndex = index;
		weaponBarIndex = index;
		killFeedIndex = 0;
		maxAmmo = 10;
		ammo = maxAmmo;
		switchCooldown = 0.75f; //gambiarrita
		//fireRateFrames = 60;

		descriptionV2 = (
			"Creates a clone that attack automatically." + "\n" +
			"Can only have one clone at once."
		);
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 0;
	}

	public override void shoot(Character character, params int[] args) {
		Point shootPos = character.getShootPos();
		Player player = character.player;
		Bass? bass = character as Bass;
		float shootAngle = 0;

		if (!player.ownedByLocalPlayer) return;

		if (character.xDir < 0) shootAngle = 128;

		if (bass?.cVclone == null) {
			new CopyVisionClone(shootPos, player, character.xDir, character.player.getNextActorNetId(), true, true);
			if (bass != null) bass.weaponCooldown = 120;
			addAmmo(-1, player);
			bass?.playSound("copyvision", true);
		} else {
			new BassBusterProj(shootPos, shootAngle, player, player.getNextActorNetId(), true);
			bass.weaponCooldown = 9;
			bass.playSound("bassbuster", true);
		}

		

	}
	/*public override bool canShoot(int chargeLevel, Player player) {
		if (!base.canShoot(chargeLevel, player)) {
			return false;
		}
		if (player.character is Bass { cVclone: not null }) {
			return false;
		}
		return true;
	}*/
}
public class CopyVisionLemon : Projectile {
	public CopyVisionLemon(
		Point pos, int xDir, Player player, ushort netProjId, bool rpc = false
	) : base(
		CopyVision.netWeapon, pos, xDir, 240, 1, player, "copy_vision_lemon",
		0, 0.075f, netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)BassProjIds.CopyVisionLemon;
		maxTime = 0.525f;
		fadeSprite = "copy_vision_lemon_fade";

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
		projId = (int)BassProjIds.BassLemon;
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new CopyVisionLemon(
			arg.pos, arg.xDir, arg.player, arg.netId
		);
	}
}

public class CopyVisionClone : Actor {
	int state = 0;
	float cloneShootTime;
	float cloneTime;
	int lemons;

	// Define the rateOfFire of the clone.
	float rateOfFire = 9;
	Bass bass = null!;
	Player player;

	public CopyVisionClone(
		Point pos, Player player, int xDir, ushort netId, bool ownedByLocalPlayer, bool rpc = false
	) : base("copy_vision_start", pos, netId, ownedByLocalPlayer, false
	) {
		bass = player.character as Bass ?? throw new NullReferenceException();
		bass.cVclone = this;
		
		useGravity = false;
		this.xDir = xDir;
		this.player = player;

		netActorCreateId = NetActorCreateId.CopyVisionClone;
		if (rpc) {
			createActorRpc(player.id);
		}
	}



	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;
		cloneTime += Global.speedMul;
		
		if (isAnimOver()) {
			state = 1;
		}
		
		if (state == 1) {
			changeSprite("copy_vision_clone", false);
			cloneShootTime += Global.speedMul;
			if (cloneShootTime > rateOfFire) {
				Point? shootPos = getFirstPOI();
				if (shootPos != null && player != null) {
					new CopyVisionLemon(shootPos.Value, xDir, player, player.getNextActorNetId(), rpc: true);
					cloneShootTime = 0;
					lemons++;
				}
			}
		}
		if (cloneTime >= 120 || lemons >= 6) {
			destroySelf();
		}
	}
	public override void onDestroy() {
		base.onDestroy();

		if (!ownedByLocalPlayer) return;
		
		new Anim(
				pos, "copy_vision_exit", xDir,
				player.getNextActorNetId(), true, sendRpc: true
			);
		
		bass.cVclone = null!;

	}

}
