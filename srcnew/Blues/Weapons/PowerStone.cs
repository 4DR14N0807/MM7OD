using System;
using System.Collections.Generic;

namespace MMXOnline;

public class PowerStone : Weapon {
    public static PowerStone netWeapon = new();

    public PowerStone() : base() {
        index = (int)RockWeaponIds.PowerStone;
        fireRateFrames = 90;
		hasCustomAnim = true;
    }

    public override float getAmmoUsage(int chargeLevel) {
        return 5;
    }

    public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
        Point shootPos = character.getShootPos();
        int xDir = character.getShootXDir();

        new PowerStoneProj(shootPos, xDir, character.player, 0, character.player.getNextActorNetId(), true);
        new PowerStoneProj(shootPos, xDir, character.player, 1, character.player.getNextActorNetId(), true);
        new PowerStoneProj(shootPos, xDir, character.player, 2, character.player.getNextActorNetId(), true);
	}
}

public class PowerStoneProj : Projectile {
    Character character;
    int stoneAngle = 120;
    float radius = 10;
    int type;
    public PowerStoneProj(Point pos, int xDir, Player player, int type, ushort? netId, bool rpc = false) : 
    base(PowerStone.netWeapon, pos, xDir, 0, 2 ,player, "power_stone_proj", 0, 0.25f, netId, player.ownedByLocalPlayer) {
        //projId = (int)RockProjIds.PowerStone;
        maxTime = 1;
        projId = (int)BluesProjIds.PowerStone;

        character = player.character;
        this.type = type;
        stoneAngle = type * 120;

        if (rpc) {
            byte[] extraArgs = new byte[] { (byte)type };

            rpcCreate(pos, player, netId, xDir, extraArgs);
        }
    }

    public static Projectile rpcInvoke(ProjParameters args) {
		return new PowerStoneProj(
			args.pos, args.xDir, args.player, args.extraData[0],
            args.netId
		);
	}


    public override void update() {
        base.update();

        base.pos.x = character.getCenterPos().x + (Helpers.cosd(stoneAngle) * radius);
        base.pos.y = character.getCenterPos().y + (Helpers.sind(stoneAngle) * radius);

        stoneAngle += 8;
        if (stoneAngle >= 360) {
			stoneAngle = 0;
		}
        radius += 1.25f;
    }
}
