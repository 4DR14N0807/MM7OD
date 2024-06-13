using System;
using System.Collections.Generic;

namespace MMXOnline;

public class ProtoBuster : Weapon {

    public static ProtoBuster netWeapon = new();
    public ProtoBuster() : base() {

    }
}

public class ProtoBusterProj : Projectile {

    public const int projSpeed = 300;

    public ProtoBusterProj(
        Point pos, int xDir, Player player, 
        ushort? netId, Point? vel = null, bool rpc = false
    ) : base (
        ProtoBuster.netWeapon, pos, xDir, projSpeed, 1,
        player, "proto_buster_proj", 0, 0, netId,
        player.ownedByLocalPlayer
    ) {
        maxTime = 0.6f;
        if (vel != null) base.vel = vel.Value;
    }
}


public class ProtoBusterChargedProj : Projectile {

    public ProtoBusterChargedProj(
        Point pos, int xDir, Player player,
        ushort? netId, bool rpc = false
    ) : base (
        ProtoBuster.netWeapon, pos, xDir, 420, 3,
        player, "proto_chargeshot_proj", Global.halfFlinch, 0.5f,
        netId, player.ownedByLocalPlayer
    ) {
        maxTime = 0.5175f;
    }
}