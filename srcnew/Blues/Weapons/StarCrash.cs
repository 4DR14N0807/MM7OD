using System;
using System.Collections.Generic;

namespace MMXOnline;

public class StarCrash : Weapon {
	public StarCrashProj? activeProj;

	public StarCrash() : base() {
		index = (int)RockWeaponIds.StarCrash;
		fireRateFrames = 60;
		hasCustomAnim = true;
	}

	public override float getAmmoUsage(int chargeLevel) {
		if (activeProj?.destroyed == false) {
			return 1;
		}
		return 0;
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Blues blues = character as Blues ?? throw new NullReferenceException();

		if (activeProj?.destroyed == false || blues.starCrash != null || blues.starCrashActive) {
			blues.destroyStarCrash();
			activeProj?.destroySelf();
			activeProj = null;
		} else {
			activeProj = new StarCrashProj(
				this, character.getCenterPos(), character.xDir, character.player,
				character.player.getNextActorNetId(), true
			);
			blues.starCrash = activeProj;
			blues.starCrashActive = true;
			blues.gravityModifier = 0.625f;
			shootCooldown = 0;
		}
	}
}

public class StarCrashProj : Projectile {
	Blues? blues;
	float starAngle;
	int radius = 30;
	int coreCooldown = 50;

	public StarCrashProj(
		Weapon weapon, Point pos, int xDir,
		Player player, ushort? netId, bool sendRpc = false
	) : base(
		weapon, pos, xDir, 0, 0, player, "empty",
		0, 0, netId, player.ownedByLocalPlayer
	) {
		projId = (int)BluesProjIds.StarCrash;
		blues = player.character as Blues;
		canBeLocal = false;
	}

	public override void update() {
		base.update();
		starAngle += 4;
		if (starAngle >= 360) starAngle -= 360;
		if (blues == null) {
			return;
		}
		// Sync poses with protoman.
		pos = blues.getCenterPos().round();
		xDir = blues.xDir;

		// Local player ends here.
		if (!ownedByLocalPlayer) {
			return;
		}
		// Destroy if not linked with Protoman anymore.
		if (blues.starCrash != this || !blues.starCrashActive || blues.overheating) {
			destroySelf();
		}
		// Ammo reduction.
		if (coreCooldown <= 0) {
			coreCooldown = 50;
			blues.addCoreAmmo(1);
		} else {
			coreCooldown--;
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		blues?.destroyStarCrash();
	}

	public override void render(float x, float y) {
		base.render(x, y);
		// Main pieces render
		for (var i = 0; i < 3; i++) {
			float extraAngle = (starAngle + i * 120) % 360;
			float xPlus = Helpers.cosd(extraAngle) * radius;
			float yPlus = Helpers.sind(extraAngle) * radius;

			Global.sprites["star_crash"].draw(
				frameIndex, pos.x + xPlus,
				pos.y + yPlus,
				xDir, yDir, getRenderEffectSet(),
				1, 1, 1, zIndex
			);
		}
	}
}
