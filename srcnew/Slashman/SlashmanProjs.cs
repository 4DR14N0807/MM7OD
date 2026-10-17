using System;

namespace MMXOnline;

public class SlashMClawMelee : GenericMeleeProj {
    public SlashMClawMelee(Point pos, Player player, bool onRage, bool addToLevel) : base(
		TenguBlade.netWeapon, pos, ProjIds.SlashMIdle,
		player, onRage ? 3 : 2, Global.halfFlinch, 20, addToLevel: addToLevel
	) {
		projId = (int)RobotMastersProjIds.SlashMClaw;
	}
}

public class SlashManClawProj : Projectile {
    public SlashManClawProj(
        Actor owner, Point pos, int xDir, bool onRage, 
        ushort? netId, bool rpc = false, Player? player = null
    ) : base(
        pos, xDir, owner, "slashman_projs_claw", netId, player
    ) {
        projId = (int)RobotMastersProjIds.SlashMClaw;
        destroyOnHit = false;
        maxTime = 0.75f;

        damager.damage = onRage ? 3 : 2;
        damager.flinch = Global.halfFlinch;
        damager.hitCooldown = 20;

        if (onRage) {
            xPushVel = xDir * 8;
        }

        if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, new byte[] {Helpers.boolToByte(onRage)});
		}
    }

    public static Projectile rpcInvoke(ProjParameters arg) {
		return new SlashManClawProj(
			arg.owner, arg.pos, arg.xDir, 
            Helpers.byteToBool(arg.extraData[0]), 
			arg.netId, player: arg.player
		);
	}

	public override void update() {
		base.update();

        if (isAnimOver()) destroySelf();
	}
}


public class SlashManGooProj : Projectile {

    float height = 64;
    public SlashManGooProj(
        Actor owner, Point pos, int xDir, int type, bool onRage,
        ushort? netId, bool rpc = false, Player? player = null
    ) : base(
        pos, xDir, owner, "slashman_projs_goo", netId, player
    ) {
        projId = (int)RobotMastersProjIds.SlashMGoo;
        useGravity = true;

        float mod = 0.5f + (type * 0.5f);
        vel.y = -300 * mod;

        maxTime = (-vel.y + MathF.Sqrt((MathF.Pow(vel.y, 2) + 2 * getGravity() * height))) / (getGravity() * 60);
        vel.x = (80 * (onRage ? 1.5f : 1)) / maxTime * xDir;
        maxTime += 0.1f;

        if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, new byte[] {(byte)type, Helpers.boolToByte(onRage)});
		}
    }

    public static Projectile rpcInvoke(ProjParameters arg) {
		return new SlashManGooProj(
			arg.owner, arg.pos, arg.xDir, arg.extraData[0], 
            Helpers.byteToBool(arg.extraData[1]), 
			arg.netId, player: arg.player
		);
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
        destroySelf();
        if (ownerActor == null || !ownedByLocalPlayer) return;

        int type = 0;
        if (other.isCeilingHit()) type = 2;
        else if (other.isSideWallHit()) type = 1;

        new SlashManGooPuddleProj(
            ownerActor, pos, xDir, type, damager.owner.getNextActorNetId(), true
        );

        playSound("SlashMTomatoSplash", sendRpc: true);
	}
}


public class SlashManGooPuddleProj : Projectile {
    public SlashManGooPuddleProj(
        Actor owner, Point pos, int xDir, int type,
        ushort? netId, bool rpc = false, Player? player = null
    ) : base(
        pos, xDir, owner, "slashman_projs_goo_puddle", netId, player
    ) {
        projId = (int)RobotMastersProjIds.SlashMGooPuddle;
        maxTime = 5;

        if (type == 1) {
            changeSprite("slashman_projs_goo_puddle_side", true);
            projId = (int)RobotMastersProjIds.SlashMGooPuddleSide;
        } else if (type == 2) {
            changeSprite("slashman_projs_goo_puddle_ceiling", true);
            projId = (int)RobotMastersProjIds.SlashMGooPuddleCeiling;
        }

        if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, new byte[] {(byte)type});
		}
    }

    public static Projectile rpcInvoke(ProjParameters arg) {
		return new SlashManGooPuddleProj(
			arg.owner, arg.pos, arg.xDir, arg.extraData[0], 
			arg.netId, player: arg.player
		);
	}

	public override void onDestroy() {
		base.onDestroy();

        if (time >= maxTime) {
            Anim.createGibEffect("slashman_goo_fade", pos, null, pieceOverdive: 5, blink: true);
        }
	}
}


public class SlashMDiveMelee : GenericMeleeProj {
    public SlashMDiveMelee(Point pos, Player player, bool onRage, bool addToLevel) : base(
		TenguBlade.netWeapon, pos, ProjIds.SlashMDive,
		player, 2, Global.halfFlinch, 20, addToLevel: addToLevel
	) {
		
	}

	public override void afterDamage(IDamagable damagable, bool wasHit) {
		base.afterDamage(damagable, wasHit);

        if (!ownedByLocalPlayer) return;
        if (damager.owner.character is not Slashman sm) return;

        if (wasHit && sm.bounces == 0) {
            sm.changeState(new SlashMFlip());
            if (!sm.onRage) sm.bounces++;
        }
	}
}