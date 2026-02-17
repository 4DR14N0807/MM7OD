using System;
using System.Collections.Generic;

namespace MMXOnline;

public class BassBuster : Weapon {
	public static BassBuster netWeapon = new();
	public int maxSteams = 4;
	public List<float> cooldowns = [];
	// Firerate * 6;
	public int streamCooldown = 28;

	public BassBuster() : base() {
		iconSprite = "hud_weapon_icon_bass";
		index = (int)BassWeaponIds.BassBuster;
		displayName = "BASS BUSTER";
		weaponSlotIndex = index;
		fireRate = 5;
		isStream = true;
		drawAmmo = false;
		drawCooldown = false;
		descriptionV2 = [
			[ "Bass' default weapon.\n" + 
			"Can be aimed in 7 directions\n" + 
			"but can't go through walls." ]
		];
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 0;
	}

	public override bool canShoot(int chargeLevel, Player player) {
		return cooldowns.Count < maxSteams;
	}

	public override void update() {
		base.update();
		for (int i = cooldowns.Count - 1; i >= 0; i--) {
			cooldowns[i] -= Global.speedMul;
			if (cooldowns[i] <= 0) {
				cooldowns.RemoveAt(i);
			}
		}
	}

	public override void shoot(Character character, params int[] args) {
		if (character is not Bass bass) {
			return;
		}
		Point shootPos = character.getShootPos();
		Player player = character.player;

		if (!player.ownedByLocalPlayer) return;
		new BassBusterProj(bass, shootPos, bass.getShootAngle(), player.getNextActorNetId(), true);
		new Anim(shootPos, "bass_buster_anim", character.xDir, player.getNextActorNetId(), true, true);
		character.playSound("bassbuster", true);

		cooldowns.Add(streamCooldown);
	}
}

public class BassBusterProj : Projectile {
	public BassBusterProj(
		Actor owner, Point pos, float byteAngle, ushort? netId, 
		bool rpc = false, Player? altPlayer = null
	) : base(
		pos, 1, owner, "bass_buster_proj", netId, altPlayer
	) {
		projId = (int)BassProjIds.BassLemon;
		byteAngle = MathF.Round(byteAngle);
		maxTime = 0.5f;
		this.byteAngle = byteAngle;
		vel = Point.createFromByteAngle(byteAngle) * 240;
		destroyOnHitWall = true;
		fadeSprite = "bass_buster_proj_fade";
		fadeOnAutoDestroy = true;

		damager.damage = 0.5f;

		if (rpc) {
			rpcCreateByteAngle(pos, ownerPlayer, netId, byteAngle);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new BassBusterProj(
			arg.owner, arg.pos, arg.byteAngle, arg.netId, altPlayer: arg.player
		);
	}
}
