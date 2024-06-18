using System;
using System.Collections.Generic;

namespace MMXOnline;

public class ArrowSlash : Weapon {
    
    public static ArrowSlash netWeapon = new ArrowSlash();
    public ArrowSlash() : base() {
        index = (int)RockWeaponIds.SAArrowSlash;
        killFeedIndex = 0;
    }
}


public class ArrowSlashProj : Projectile {

    float timeMoving;

    public ArrowSlashProj(
        Weapon weapon, Point pos, int xDir,
        Player player, ushort netProjId,
        bool rpc = false
    ) : base (
        weapon, pos, xDir, 0, 1,
        player, "slash_claw_proj", 0, 0.5f,
        netProjId, player.ownedByLocalPlayer
    ) {
        maxTime = 1f;
        projId = (int)RockProjIds.SAArrowSlash;
        //fadeSprite = "slash_claw_proj_fade";

        if (rpc) {
            rpcCreate(pos, player, netProjId, xDir);
        }
    }

    public static Projectile rpcInvoke(ProjParameters arg) {
        return new ArrowSlashProj(
            FreezeCracker.netWeapon, arg.pos, arg.xDir, arg.player,
            arg.netId
        );
    }

    
    public override void update(){
        base.update();

        if (isAnimOver()) {
            timeMoving += Global.spf;
            base.vel.x = 240 * xDir;
        }

        if (timeMoving >= Global.spf * 8 && base.vel.y > -120) base.vel.y -= 5;

        damager.damage = getDamageIncrease();

    }

    int getDamageIncrease() {
        int finalDamage;
        finalDamage = (int)(time / (20f / 60f)) + 1;
        if (finalDamage >= 3) damager.flinch = Global.halfFlinch;
        return finalDamage;
    }
}

public class SAArrowSlashState : CharState {

    bool fired;

    public SAArrowSlashState() : base("sa_arrowslash", "", "","") {
    }

    public override bool canEnter(Character character) {
        return base.canEnter(character);
    }

    public override void update() {
        base.update();

        if (!fired) {
            new ArrowSlashProj(new ArrowSlash(), character.getCenterPos(), character.xDir, player, player.getNextActorNetId(), true);
            fired = true;
        }

        if (character.isAnimOver()) character.changeToIdleOrFall();
    }
}