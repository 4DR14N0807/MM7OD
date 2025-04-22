using System;
using System.Collections.Generic;

namespace MMXOnline;

public class StarCrash : Weapon {
	public static StarCrash netWeapon = new();
	public StarCrashProj? activeProj;

	public StarCrash() : base() {
		displayName = "STAR CRASH";
		descriptionV2 = "Creates a star-shaped energy barrier\nthat reduces gravity.";
		ammoUseText = "4";

		index = (int)BluesWeaponIds.StarCrash;
		fireRate = 60;
		hasCustomAnim = true;
	}

	public override float getAmmoUsage(int chargeLevel) {
		if (activeProj?.destroyed == false) {
			return 4;
		}
		return 0;
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Blues blues = character as Blues ?? throw new NullReferenceException();

		if (activeProj?.destroyed == false || blues.starCrash != null || blues.starCrashActive) {
			blues.delinkStarCrash();
			activeProj?.shoot();
			activeProj = null;
		} else {
			activeProj = new StarCrashProj(
				blues, blues.getCenterPos(), blues.xDir,
				character.player.getNextActorNetId(), true
			);
			blues.starCrash = activeProj;
			blues.starCrashActive = true;
			blues.gravityModifier = 0.625f;
			shootCooldown = 0;
		}
		blues.inCustomShootAnim = false;
	}
}

public class StarCrashProj : Projectile {
	Blues? blues;
	Player? player;
	float starAngle;
	int radius = 28;
	int frameCount;
	int starFrame;
	List<Sprite> stars = new();
	bool threw;

	public StarCrashProj(
		Actor owner, Point pos, int xDir, ushort? netId, 
		bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "empty", netId, altPlayer
	) {
		projId = (int)BluesProjIds.StarCrash;
		if (ownedByLocalPlayer) {
			blues = owner as Blues ?? throw new NullReferenceException();
			this.player = ownerPlayer;
		}
		
		setIndestructableProperties();
		fadeOnAutoDestroy = true;
		canBeLocal = false;

		for (int i = 0; i < 3; i++) {
			Sprite star = new Sprite("star_crash");
			stars.Add(star);
		}

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new StarCrashProj(
			args.owner, args.pos, args.xDir, args.netId, altPlayer: args.player
		);
	}

	public override void update() {
		base.update();

		frameCount++;
		if (frameCount > 4) {
			frameCount = 0;
			starFrame++;
		}

		starAngle += 4 * xDir;
		if (starAngle >= 256) { starAngle -= 256; }
		if (starAngle < 0) { starAngle += 256; }

		// We check if we shoot it already.
		if (!ownedByLocalPlayer) return;
		if (threw) return;

		// Sync poses with protoman.
		if (blues != null) {
			changePos(blues.pos.addxy(2 * xDir, -20));
			xDir = blues.xDir;
		}
		// Destroy if not linked with Protoman anymore.
		if (blues == null || blues.destroyed || blues.starCrash != this ||
			!blues.starCrashActive || blues.overheating || time >= 8
		) {
			destroySelf();
		}
	}

	public void shoot() {
		if (!ownedByLocalPlayer) return;
		if (blues != null) {
			blues.delinkStarCrash();
		}
		new StarCrashProj2(
			pos, xDir, damager.owner, 
			damager.owner.getNextActorNetId(), 
			starAngle, starFrame, true
		);
	}

	public override void render(float x, float y) {
		base.render(x,y);
		Point center = pos;

		for (int i = 0; i < stars.Count; i++) {
			float extraAngle = (starAngle + i * 85) % 256;
			long altZIndex = extraAngle >= 128 ? ZIndex.Character - 100 : zIndex;
			float xPlus = (Helpers.cosb(extraAngle) * radius);
			float yPlus = (Helpers.sinb(extraAngle) * radius);

			for (int j = 2; j >= 1; j--) {
				float backAngle = extraAngle - (16 * j * xDir) % 256;
				long altZIndex1 = backAngle >= 128 ? ZIndex.Character - 100 : zIndex;
				float xBack = (Helpers.cosb(backAngle) * radius);
				float yBack = (Helpers.sinb(backAngle) * radius);
				stars[i].draw(
					starFrame % 4, center.x + xBack, center.y + yBack,
					xDir, yDir, getRenderEffectSet(), 0.6666f - (0.3333f * (j - 1)), 1, 1, altZIndex1
				);
			}
			stars[i].draw(
				starFrame % 4, center.x + xPlus, center.y + yPlus,
				xDir, yDir, getRenderEffectSet(), 1, 1, 1, altZIndex
			);
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		if (!ownedByLocalPlayer) return;
		blues?.delinkStarCrash();
	}
}


public class StarCrashProj2 : Projectile {
	int radius = 28;
	int frameCount;
	int starFrame;
	List<Sprite> stars = new();
	float starAngle;

	public StarCrashProj2(
		Point pos, int xDir, Player player, ushort? netId, 
		float ang, int frame, bool rpc = false
	) : base(
		StarCrash.netWeapon, pos, xDir, 180, 2,
		player, "star_crash_proj", 0, 0.5f, 
		netId, player.ownedByLocalPlayer
	) {
		projId = (int)BluesProjIds.StarCrash2;
		maxTime = 1;
		starAngle = ang;
		canBeLocal = false;
		
		for (int i = 0; i < 3; i++) {
			Sprite star = new Sprite("star_crash");
			stars.Add(star);
		}

		if (rpc) rpcCreate(pos, player, netId, xDir, new byte[] { (byte)ang, (byte)frame} );
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new StarCrashProj2(
			args.pos, args.xDir, args.player, args.netId, 
			args.extraData[0], args.extraData[1]
		);
	}

	public override void update() {
		base.update();

		if (frameCount > 4) {
			frameCount = 0;
			starFrame++;
		}

		starAngle += 8 * xDir;
		if (starAngle >= 256) { starAngle -= 256; }
		if (starAngle < 0) { starAngle += 256; }
	}

	public override void render(float x, float y) {
		base.render(x,y);
		Point center = pos;

		for (int i = 0; i < stars.Count; i++) {
			float extraAngle = (starAngle + i * 85) % 256;
			float xPlus = (Helpers.cosb(extraAngle) * radius);
			float yPlus = (Helpers.sinb(extraAngle) * radius);

			for (int j = 3; j >= 1; j--) {
				float backAngle = extraAngle - (16 * j * xDir) % 256;
				float xBack = (Helpers.cosb(backAngle) * radius);
				float yBack = (Helpers.sinb(backAngle) * radius);
				stars[i].draw(
					starFrame % 4, center.x + xBack, center.y + yBack,
					xDir, yDir, getRenderEffectSet(), 0.75f - (0.25f * (j - 1)), 1, 1, zIndex
				);
			}
			stars[i].draw(
				starFrame % 4, center.x + xPlus, center.y + yPlus,
				xDir, yDir, getRenderEffectSet(), 1, 1, 1, zIndex
			);
		}
	}

	public override void onDestroy() {
		base.onDestroy();

		if (!ownedByLocalPlayer) return;

		for (int i = 0; i < 3; i++) {
			float extraAngle = (starAngle + i * 120) % 360;
			float xPlus = pos.x + (Helpers.cosd(extraAngle) * radius);
			float yPlus = pos.y + (Helpers.sind(extraAngle) * radius);

			new Anim(new Point(xPlus, yPlus), "star_crash_fade", xDir, damager.owner.getNextActorNetId(), true, true);
		}
	}
} 
