using System;
using System.Collections.Generic;
using SFML.Graphics;
using SFML.Graphics.Glsl;

namespace MMXOnline;

public class CopyVision : Weapon {
	public static CopyVision netWeapon = new();
	public CopyVisionClone? clone;

	public CopyVision() : base() {
		index = (int)BassWeaponIds.CopyVision;
		displayName = "COPY VISION";
		weaponSlotIndex = index;
		weaponBarBaseIndex = index;
		weaponBarIndex = index;
		killFeedIndex = 0;
		maxAmmo = 10;
		ammo = maxAmmo;
		//switchCooldown = 0.75f; //gambiarrita
		fireRate = 9;

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
		Bass bass = character as Bass ?? throw new NullReferenceException();
		float shootAngle = 0;

		if (!player.ownedByLocalPlayer) return;

		if (character.xDir < 0) {
			shootAngle = 128;
		}
		if (ammo > 0 && !isStream && bass.cVclone?.destroyed != false) {
			clone = new CopyVisionClone(
				bass, shootPos, character.xDir, character.player.getNextActorNetId(), true, true, player
			);
			bass.cVclone = clone;
			addAmmo(-1, player);
			bass?.playSound("copyvision", true);
		} else {
			if (ammo > 0) {
				new CopyVisionLemon(bass, shootPos, bass.xDir, player.getNextActorNetId(), true);
			} else {
				new CopyVisionLemonAlt(shootPos, bass.xDir, player, player.getNextActorNetId(), true);
			}
			bass.playSound("bassbuster", true);
		}
	}

	public override void update() {
		base.update();
		if (ammo <= 0 || clone?.destroyed == false) {
			isStream = true;
		} else {
			isStream = false;
		}
	}

	public override bool canShoot(int chargeLevel, Character character) {
		return true;
	}
}

public class CopyVisionLemon : Projectile {
	public CopyVisionLemon(
		Actor owner, Point pos, int xDir, ushort? netProjId, 
		bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "copy_vision_lemon", netProjId, altPlayer
	) {
		projId = (int)BassProjIds.CopyVisionLemon;
		maxTime = 0.525f;
		fadeSprite = "copy_vision_lemon_fade";

		vel.x = 240 * xDir;
		damager.damage = 1;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netProjId, xDir);
		}
		projId = (int)BassProjIds.BassLemon;
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new CopyVisionLemon(
			arg.owner, arg.pos, arg.xDir, arg.netId, altPlayer: arg.player
		);
	}
}

public class CopyVisionLemonAlt : Projectile {
	public CopyVisionLemonAlt(
		Point pos, int xDir, Player player, ushort netProjId, bool rpc = false
	) : base(
		CopyVision.netWeapon, pos, xDir, 240, 0.5f, player, "copy_vision_lemon",
		0, 9f / 60f, netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)BassProjIds.CopyVisionLemonAlt;
		maxTime = 0.525f;
		fadeSprite = "copy_vision_lemon_fade";

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
		projId = (int)BassProjIds.BassLemon;
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new CopyVisionLemon(
			arg.owner, arg.pos, arg.xDir, arg.netId
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
	Bass? bass;
	Player player;

	public CopyVisionClone(
		Actor owner, Point pos, int xDir, ushort? netId, bool ownedByLocalPlayer, 
		bool rpc = false, Player? altPlayer = null
	) : base("copy_vision_start", pos, netId, ownedByLocalPlayer, false
	) {
		player = altPlayer ?? throw new NullReferenceException();
		if (!ownedByLocalPlayer) {
			bass = player.character as Bass;
		}
		useGravity = false;
		this.xDir = xDir;

		netActorCreateId = NetActorCreateId.CopyVisionClone;
		if (rpc) {
			createActorRpc(bass.player.id);
		}
	}

	public override void update() {
		base.update();
		cloneTime += Global.speedMul;

		if (!ownedByLocalPlayer) {
			if (cloneTime >= 60 * 4) {
				destroySelf();
			}
			return;
		}
		if (isAnimOver()) {
			state = 1;
		}
		if (state == 1) {
			changeSprite("copy_vision_clone", false);
			cloneShootTime += Global.speedMul;
			if (cloneShootTime > rateOfFire) {
				Point? shootPos = getFirstPOI();
				if (shootPos != null && player != null) {
					new CopyVisionLemon(bass, shootPos.Value, xDir, player.getNextActorNetId(), rpc: true);
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
		if (bass != null && bass.cVclone == this) {
			bass.cVclone = null;
		}
	}

}
