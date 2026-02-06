using System;

namespace MMXOnline;

public class CopyVision : Weapon {
	public static CopyVision netWeapon = new();
	public CopyVisionClone? cClone;

	public CopyVision() : base() {
		iconSprite = "hud_weapon_icon_bass";
		index = (int)BassWeaponIds.CopyVision;
		displayName = "COPY VISION";
		weaponSlotIndex = index;
		weaponBarBaseIndex = index;
		weaponBarIndex = index;
		killFeedIndex = 0;
		maxAmmo = 12;
		ammo = maxAmmo;
		//switchCooldown = 0.75f; //gambiarrita
		fireRate = 9;
		drawCooldown = false;

		//descriptionV2 = (
		//	"Creates a clone that attack automatically." + "\n" +
		//	"Can only have one clone at once."
		//);

		descriptionV2 = [
			[ "Creates a clone that attacks automatically." + "\n" +
			"Can only have one clone at once." ],
		];
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 0;
	}

	public override void shoot(Character character, params int[] args) {
		Point shootPos = character.getShootPos();
		Player player = character.player;
		if (!player.ownedByLocalPlayer) return;
		Bass bass = character as Bass ?? throw new NullReferenceException();
	
		if (ammo > 0 && !isStream && cClone?.destroyed != false) {
			cClone = new CopyVisionClone(
				bass, player, shootPos, character.xDir, character.player.getNextActorNetId(), true, true
			);
			addAmmo(-1, player);
			bass?.playSound("copyvision", true);
		} else {
			new CopyVisionLemonAlt(bass, shootPos, bass.xDir, ammo <= 0, player.getNextActorNetId(), true);
			bass.playSound("bassbuster", true);
		}
	}

	public override void update() {
		base.update();
		if (ammo <= 0 || cClone?.destroyed == false) {
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
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new CopyVisionLemon(
			arg.owner, arg.pos, arg.xDir, arg.netId, altPlayer: arg.player
		);
	}
}

public class CopyVisionLemonAlt : Projectile {
	public CopyVisionLemonAlt(
		Actor owner, Point pos, int xDir, bool isWeak, ushort? netProjId, 
		bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "copy_vision_lemon", netProjId, altPlayer
	) {
		projId = (int)BassProjIds.CopyVisionLemonAlt;
		maxTime = 28 / 60f;
		fadeSprite = "copy_vision_lemon_fade";

		vel.x = 240 * xDir;
		damager.damage = 1;
		if (isWeak) {
			damager.damage = 0.5f;
		}

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netProjId, xDir, (byte)(isWeak ? 1 : 0));
		}
		addRenderEffect(RenderEffectType.ChargePurple);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new CopyVisionLemonAlt(
			arg.owner, arg.pos, arg.xDir, arg.extraData[0] == 1, arg.netId
		);
	}
}

public class CopyVisionClone : Actor {
	int state = 0;
	float cloneShootTime;
	float cloneTime;
	int lemons;

	// Define the rateOfFire of the clone.
	float rateOfFire = 12;
	Bass? bass;
	Player player;

	public CopyVisionClone(
		Actor? owner, Player player, Point pos, int xDir, ushort? netId, bool ownedByLocalPlayer, 
		bool rpc = false
	) : base(
		"copy_vision_start", pos, netId, ownedByLocalPlayer, false
	) {
		bass = owner as Bass;
		this.player = player;

		useGravity = false;
		this.xDir = xDir;

		netActorCreateId = NetActorCreateId.CopyVisionClone;
		if (rpc) {
			createActorRpc(player.id);
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
				if (shootPos != null && bass != null) {
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

		if (!ownedByLocalPlayer || player == null) return;
		new Anim(
			pos, "copy_vision_exit", xDir,
			player.getNextActorNetId(), true, sendRpc: true
		);
	}

}
