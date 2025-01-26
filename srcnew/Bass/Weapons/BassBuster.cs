using System;
using System.Collections.Generic;

namespace MMXOnline;

public class BassBuster : Weapon {
	public static BassBuster netWeapon = new();
	public List<BassBusterProj> lemonsOnField = new List<BassBusterProj>();

	public BassBuster() : base() {
		index = (int)BassWeaponIds.BassBuster;
		displayName = "BASS BUSTER";
		weaponSlotIndex = index;
		fireRate = 6;
		isStream = true;
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 0;
	}

	public override bool canShoot(int chargeLevel, Player player) {
		if (!base.canShoot(chargeLevel, player)) return false;
		if (chargeLevel > 1) {
			return true;
		}
		for (int i = lemonsOnField.Count - 1; i >= 0; i--) {
			if (lemonsOnField[i].destroyed) {
				lemonsOnField.RemoveAt(i);
				continue;
			}
		}
		return lemonsOnField.Count < 4;
	}


	public override void shoot(Character character, params int[] args) {
		if (character is not Bass bass) {
			return;
		}
		Point shootPos = character.getShootPos();
		Player player = character.player;

		var proj = new BassBusterProj(shootPos, bass.getShootAngle(), player, player.getNextActorNetId(), true);
		new Anim(shootPos, "bass_buster_anim", character.xDir, player.getNextActorNetId(), true, true);
		lemonsOnField.Add(proj);
		character.playSound("bassbuster", true);
	}
}

public class BassBusterProj : Projectile {
	public BassBusterProj(
		Point pos, float byteAngle, Player player, ushort? netId, bool rpc = false
	) : base(
		BassBuster.netWeapon, pos, 1, 0, 0.25f, player, "bass_buster_proj",
		0, 0, netId, player.ownedByLocalPlayer
	) {
		projId = (int)BassProjIds.BassLemon;
		byteAngle = MathF.Round(byteAngle);
		maxTime = 0.7f;
		this.byteAngle = byteAngle;
		vel = Point.createFromByteAngle(byteAngle) * 240;
		//destroyOnHitWall = true;
		fadeSprite = "bass_buster_proj_fade";
		canBeLocal = false;

		if (rpc) {
			rpcCreateByteAngle(pos, player, netId, byteAngle);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new BassBusterProj(
			arg.pos, arg.byteAngle, arg.player, arg.netId
		);
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);

		destroySelf();
	}
}
