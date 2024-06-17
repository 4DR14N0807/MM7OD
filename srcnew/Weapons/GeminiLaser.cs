using System;
using System.Collections.Generic;

namespace MMXOnline;

public class GeminiLaser : Weapon {
    public GeminiLaser() : base() {
        index = (int)RockWeaponIds.GeminiLaser;
        rateOfFire = 1;
    }

    public override float getAmmoUsage(int chargeLevel) {
        return 3;
    }


    public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
        Point shootPos = character.getShootPos();
        int xDir = character.getShootXDir();

        new GeminiLaserProj(this, shootPos, xDir, character.player, character.player.getNextActorNetId(), true);
	}
}


public class GeminiLaserProj : Projectile {

    public Sprite spriteStart;
	public List<Sprite> spriteMids = new List<Sprite>();
	public Sprite spriteEnd;
	public float length = 1;
    public float laserTime;
    bool didBounceOnce;

    public GeminiLaserProj(Weapon weapon, Point pos, int xDir, Player player, ushort netId, bool rpc = false) :
    base(weapon, pos, xDir, 0, 2, player, "gemini_laser_mid_proj", 0, 0.5f, netId, player.ownedByLocalPlayer) {
        maxTime = 2;
        canBeLocal = false;
        destroyOnHit = false;

        spriteStart = Global.sprites["gemini_laser_start_proj"].clone();
        for (var i = 0; i < 4; i++) {
			var midSprite = Global.sprites["gemini_laser_mid_proj"].clone();
			spriteMids.Add(midSprite);
		}
        spriteEnd = Global.sprites["gemini_laser_end_proj"].clone();

    }


    public override void render(float x, float y) {
        int spriteMidLen = 9;
		int i = 0;
		spriteStart.draw(frameIndex, pos.x + x + ((i - 1) * xDir * spriteMidLen), pos.y + y, xDir, yDir, getRenderEffectSet(), 1, 1, 1, zIndex);
		for (i = 0; i < length; i++) {
			spriteMids[i].draw(frameIndex, pos.x + x + (i * xDir * spriteMidLen), pos.y + y, xDir, yDir, getRenderEffectSet(), 1, 1, 1, zIndex);
		}
		
        spriteEnd.draw(frameIndex, pos.x + x + (i * xDir * spriteMidLen), pos.y + y, xDir, yDir, getRenderEffectSet(), 1, 1, 1, zIndex);
	}

    public override void update() {
		base.update();

		var topX = 0;
		var topY = 0;

		var spriteMidLen = 9;
		var spriteEndLen = 9;

		var botX = (length * spriteMidLen) + spriteEndLen;
		var botY = 7;

		var rect = new Rect(topX, topY, botX, botY);
		globalCollider = new Collider(rect.getPoints(), true, this, false, false, 0, new Point(0, 0));

		laserTime += Global.spf;
		if (laserTime > 0.2f) {
			if (length < 4) {
				length++;
			} else {
				vel.x = 240 * xDir;
			}
			laserTime = 0;
		}
	}


	public override void onHitWall(CollideData other) {
		base.onHitWall(other);

        if (!didBounceOnce) {
            xDir *= -1;
            yDir *= -1;
            base.vel.x = 240 * Helpers.cosd(45) * xDir;
            base.vel.y = 240 * Helpers.sind(45) * yDir; 
            didBounceOnce = true;

            spriteStart = Global.sprites["gemini_laser_start_bounce_proj"].clone();
            for (var i = 0; i < 4; i++) {
			var midSprite = Global.sprites["gemini_laser_mid_proj"].clone();
			spriteMids[i] = midSprite;
		    }
            spriteEnd = Global.sprites["gemini_laser_end_bounce_proj"].clone();
        } else {
            var normal = other.hitData.normal ?? new Point(0, -1);

            if (normal.isSideways()) {
                xDir *= -1;
            } else {
                yDir *= -1;
            }

            base.vel.x = 240 * Helpers.cosd(45) * xDir;
            base.vel.y = 240 * Helpers.sind(45) * yDir;
            if (damager.damage < 4) damager.damage++;
        }
	}
}