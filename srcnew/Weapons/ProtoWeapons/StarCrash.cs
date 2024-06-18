using System;
using System.Collections.Generic;

namespace MMXOnline;


public class StarCrash : Weapon {
    public StarCrash() : base() {
        index = (int)RockWeaponIds.StarCrash;
        fireRateFrames = 60;
    }

    
    public override float getAmmoUsage(int chargeLevel) {
        return 0;
    }

    public override void shoot(Character character, params int[] args) {
	    base.shoot(character, args);
        Point shootPos = character.getCenterPos();
        int xDir = character.getShootXDir();
        ProtoMan protoman = character as ProtoMan;

        if (protoman.starCrash != null) {
            protoman.destroyStarCrash();
        }
        else {
            new StarCrashProj(this, shootPos, xDir, character.player, character.player.getNextActorNetId(), true);
            protoman.addCoreAmmo(4);
        }
	}
}


public class StarCrashProj : Projectile {

    ProtoMan? protoman;
    Point centerPos;
    float starAngle;
    int radius = 30;
    int coreCooldown;
    Player player;

    public StarCrashProj(Weapon weapon, Point pos, int xDir, Player player, ushort? netId, bool sendRpc = false) :
    base(weapon, pos, xDir, 0, 0, player, "empty", 0, 0, netId, player.ownedByLocalPlayer) {
        projId = (int)RockProjIds.StarCrash;
        protoman = player.character as ProtoMan;
        protoman.starCrash = this; 
        protoman.gravityModifier = 0.625f;
        this.player = player;
    }


    public override void update() {
        base.update();

        centerPos = getCenterPos();
        changePos(centerPos);
        starAngle += 4;
        if (starAngle >= 360) starAngle -= 360;

        if (time >= 1) {
            if (coreCooldown <= 0) {
                coreCooldown = 20;
                protoman.addCoreAmmo(1);
            }
            coreCooldown--;
        }

        if (player.input.isPressed(Control.Special1, player)) destroySelf();
    }


    public override void onDestroy() {
        base.onDestroy();
        protoman.destroyStarCrash();
    }


    public override void render(float x, float y){
		base.render(x, y);
		if (protoman != null) centerPos = protoman.getCenterPos();
		
		//main pieces render
		for (var i = 0; i < 3; i++) {
			float extraAngle = starAngle + i*120;
			if (extraAngle >= 360) extraAngle -= 360;
			float xPlus = Helpers.cosd(extraAngle) * radius;
			float yPlus = Helpers.sind(extraAngle) * radius;
			if (protoman != null) xDir = protoman.getShootXDir();
			Global.sprites["star_crash"].draw(frameIndex, centerPos.x + xPlus, centerPos.y + yPlus, xDir, yDir, getRenderEffectSet(), 1, 1, 1, zIndex);
		}
	}
}