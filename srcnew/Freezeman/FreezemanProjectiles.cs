using System;
using System.Collections.Generic;

namespace MMXOnline;

public class FreezeMProj : Projectile {

    float projSpeed = 300;
    Actor? ownChr = null;
    int ang;
    public FreezeMProj(
        Actor owner, Point pos, int xDir, int ang,
        ushort? netId, bool rpc = false, Player? player = null
    ) : base(
        pos, xDir, owner, "freeze_cracker_proj", netId, player
    ) {
        projId = (int)RobotMastersProjIds.FreezeMProj;
        maxTime = 0.6f;
        fadeSprite = "freeze_cracker_start";
        vel = Point.createFromByteAngle(ang).times(projSpeed);
        damager.damage = 2;
        damager.hitCooldown = 10;

        if (ownedByLocalPlayer) ownChr = owner;
        this.ang = ang;

        if (rpc) {
			byte[] extraArgs = new byte[] { (byte)ang };

			rpcCreate(pos, owner, ownerPlayer, netId, xDir, extraArgs);
		}
    }

    public static Projectile rpcInvoke(ProjParameters arg) {
		return new FreezeMProj(
			arg.owner, arg.pos, arg.xDir, arg.extraData[0],
            arg.netId, player: arg.player
		);
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
        if (!ownedByLocalPlayer || ownChr == null) return;
        destroySelf();

        bool vertical = ang != 64 && ang != 192;
        if (vertical) playSound("freezemHitWall", sendRpc: true);
        else playSound("freezemHitGround", sendRpc: true);

        for (int i = 0; i < 6; i++) {
            new FreezeMSplitProj(
                ownChr, pos, xDir, i, false, owner.getNextActorNetId(), rpc: true
            );
        }
	}
}

public class FreezeMFreezeProj : Projectile {

    Actor? ownChr = null;
    int ang;
    public FreezeMFreezeProj(
        Actor owner, Point pos, int xDir, int ang,
        ushort? netId, bool rpc = false, Player? player = null
    ) : base(
        pos, xDir, owner, "freezem_fcracker2", netId, player
    ) {
        projId = (int)RobotMastersProjIds.FreezeMFreezeProj;
        maxTime = 0.6f;
        fadeSprite = "freeze_cracker_start";
        vel = Point.createFromByteAngle(ang).times(240);
        damager.damage = 2;
        damager.hitCooldown = 10;

        if (ownedByLocalPlayer) ownChr = owner;
        this.ang = ang;

        if (rpc) {
			byte[] extraArgs = new byte[] { (byte)ang };

			rpcCreate(pos, owner, ownerPlayer, netId, xDir, extraArgs);
		}
    }

    public static Projectile rpcInvoke(ProjParameters arg) {
		return new FreezeMFreezeProj(
			arg.owner, arg.pos, arg.xDir, arg.extraData[0],
            arg.netId, player: arg.player
		);
	}

    public override void onHitWall(CollideData other) {
		base.onHitWall(other);
        if (!ownedByLocalPlayer || ownChr == null) return;
        destroySelf();
        /* for (int i = 0; i < 6; i++) {
            new FreezeMSplitProj(
                ownChr, pos, xDir, i, true, owner.getNextActorNetId(), rpc: true
            );
        } */

        bool vertical = ang != 64 && ang != 192;
        if (vertical) playSound("freezemHitWall", sendRpc: true);
        else playSound("freezemHitGround", sendRpc: true);

       /*  new FreezeMCrawlProj(
            ownChr, pos, 1, vertical, owner.getNextActorNetId(), rpc: true
        );
        new FreezeMCrawlProj(
            ownChr, pos, -1, vertical, owner.getNextActorNetId(), rpc: true
        ); */
	}
}

public class FreezeMSplitProj : Projectile {
    public FreezeMSplitProj(
        Actor owner, Point pos, int xDir, int type, bool freeze,
        ushort? netId, bool rpc = false, Player? player = null
    ) : base(
        pos, xDir, owner, "freezem_fcracker_small", netId, player
    ) {
        projId = (int)RobotMastersProjIds.FreezeMSplitProj;
        maxTime = 8 / 60f;
        
        vel = Point.createFromByteAngle((type * 42.5f) + 21.25f).times(240);
        damager.damage = 2;
        damager.hitCooldown = 10;

        if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type, (byte)(freeze ? 1 : 0) };

			rpcCreate(pos, owner, ownerPlayer, netId, xDir, extraArgs);
		}

        projId = freeze ? (int)RobotMastersProjIds.FreezeMFreezeProj : (int)RobotMastersProjIds.FreezeMSplitProj;
    }
    
    public static Projectile rpcInvoke(ProjParameters arg) {
		return new FreezeMSplitProj(
			arg.owner, arg.pos, arg.xDir, arg.extraData[0],
            Helpers.byteToBool(arg.extraData[1]), arg.netId, player: arg.player
		);
	}
}

public class FreezeMSplitProj2 : Projectile {
    public FreezeMSplitProj2(
        Actor owner, Point pos, int xDir, float ang,
        ushort? netId, bool rpc = false, Player? player = null
    ) : base(
        pos, xDir, owner, "freezem_fcracker_small", netId, player
    ) {
        projId = (int)RobotMastersProjIds.FreezeMSplitProj2;
        maxTime = 0.1f;
        
        vel = Point.createFromByteAngle(ang).times(480);
        damager.damage = 2;
        damager.flinch = Global.halfFlinch;
        damager.hitCooldown = 10;

        if (rpc) {
			byte[] extraArgs = new byte[] { (byte)ang };

			rpcCreate(pos, owner, ownerPlayer, netId, xDir, extraArgs);
		}
    }

    public static Projectile rpcInvoke(ProjParameters arg) {
		return new FreezeMSplitProj2(
			arg.owner, arg.pos, arg.xDir, arg.extraData[0],
            arg.netId, player: arg.player
		);
	}
}


public class FreezeMCrawlProj : Projectile {

    Point dir;
    float currentAngle;
    bool vertical;

    public FreezeMCrawlProj(
        Actor owner, Point pos, int xDir, bool vertical,
        ushort? netId, bool rpc = false, Player? player = null
    ) : base(
        pos, xDir, owner, "freezem_floor_proj", netId, player
    ) {
        projId = (int)RobotMastersProjIds.FreezeMCrawlProj;
        maxTime = 1.25f;
        wallCrawlSpeed = 240;
        destroyOnHit = false;

        this.vertical = vertical;
        dir = vertical ? new Point(0, 1) : new Point(1,0);
        if (xDir < 0 && vertical) wallCrawlSpeed *= -1;

        setupWallCrawl(new Point(xDir, 0));
    }

	public override void update() {
		base.update();

        updateWallCrawl();
        if (time >= 10/60f) {
            if (vertical && deltaPos.x != 0) {
                destroySelf();
            } else if (deltaPos.y != 0) {
                //destroySelf();
            }
        }
        
	}
}