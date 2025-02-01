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
		drawAmmo = false;
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

		if (!player.ownedByLocalPlayer) return;

		var proj = new BassBusterProj(bass, shootPos, bass.getShootAngle(), player.getNextActorNetId(), true);
		new Anim(shootPos, "bass_buster_anim", character.xDir, player.getNextActorNetId(), true, true);
		lemonsOnField.Add(proj);
		character.playSound("bassbuster", true);
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
		maxTime = 0.7f;
		this.byteAngle = byteAngle;
		vel = Point.createFromByteAngle(byteAngle) * 240;
		//destroyOnHitWall = true;
		fadeSprite = "bass_buster_proj_fade";
		fadeOnAutoDestroy = true;

		damager.damage = 0.25f;

		if (rpc) {
			rpcCreateByteAngle(pos, ownerPlayer, netId, byteAngle);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new BassBusterProj(
			arg.owner, arg.pos, arg.byteAngle, arg.netId, altPlayer: arg.player
		);
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);

		destroySelf();
	}
}
