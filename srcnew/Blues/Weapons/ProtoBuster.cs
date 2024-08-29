using System;
using System.Collections.Generic;

namespace MMXOnline;

public class ProtoBuster : Weapon {

	public static ProtoBuster netWeapon = new();
	public ProtoBuster() : base() {

	}
}

public class ProtoBusterProj : Projectile {
	public ProtoBusterProj(
		Point pos, int xDir, Player player,
		ushort? netId, Point? vel = null, bool rpc = false
	) : base(
		ProtoBuster.netWeapon, pos, xDir, 250, 1, player,
		"proto_buster_proj", 0, 0, netId, player.ownedByLocalPlayer
	) {
		maxTime = 0.425f;
		projId = (int)BluesProjIds.Lemon;
		fadeSprite = "proto_buster_proj_fade";

		if (rpc) {
			rpcCreate(pos, player, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new ProtoBusterProj(
			args.pos, args.xDir, args.player, args.netId
		);
	}
	public override void update() {
		base.update();
		if (reflectCount == 0 && System.MathF.Abs(vel.x) < 300) {
			vel.x += Global.spf * xDir * 900f;
			if (System.MathF.Abs(vel.x) >= 300) {
				vel.x = (float)xDir * 300;
			}
		}
	}

	public override void onReflect() {
		vel.x = 300;
		base.onReflect();
	}
}

public class ProtoBusterAngledProj : Projectile {
	public ProtoBusterAngledProj(
		Point pos, float byteAngle, int type, Player player, ushort? netId, bool rpc = false
	) : base(
		ProtoBuster.netWeapon, pos, 1, 0, 2, player,
		"proto_midcharge_proj", 0, 1, netId, player.ownedByLocalPlayer
	) {
		byteAngle = byteAngle % 256;
		fadeSprite = "proto_midcharge_proj_fade";
		fadeOnAutoDestroy = true;
		maxTime = 0.425f;
		projId = (int)BluesProjIds.LemonAngled;
		vel = 300 * Point.createFromByteAngle(byteAngle);

		if (byteAngle > 64 && byteAngle < 192) {
			xDir = -1;
			this.byteAngle = byteAngle - 128;
		} else {
			this.byteAngle = byteAngle;
		}

		if (type == 0) {
			changeSprite("proto_buster_proj", true);
			fadeSprite = "proto_buster_proj_fade";
			damager.damage = 1;
		} else if (type == 2) {
			damager.flinch = Global.miniFlinch;
			changeSprite("rock_buster2_proj", true);
			fadeSprite = "rock_buster2_fade";
		}

		if (rpc) {
			rpcCreateAngle(pos, player, netId, byteAngle, (byte)type);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new ProtoBusterAngledProj(
			args.pos, args.byteAngle, args.extraData[0], args.player, args.netId
		);
	}

	public override void onReflect() {
		xDir *= -1;
		vel.x *= -1;
		vel.y *= -1;
		time = 0;
	}
}

public class ProtoBusterOverdriveProj : Projectile {
	public ProtoBusterOverdriveProj(
		Point pos, int xDir, Player player,
		ushort? netId, Point? vel = null, bool rpc = false
	) : base(
		ProtoBuster.netWeapon, pos, xDir, 250, 1, player,
		"proto_midcharge_proj", Global.miniFlinch, 0, netId, player.ownedByLocalPlayer
	) {
		maxTime = 0.425f;
		projId = (int)BluesProjIds.LemonOverdrive;
		fadeSprite = "proto_midcharge_proj_fade";
		fadeOnAutoDestroy = true;

		if (rpc) {
			rpcCreate(pos, player, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new ProtoBusterProj(
			args.pos, args.xDir, args.player, args.netId
		);
	}
	public override void update() {
		base.update();
		if (reflectCount == 0 && System.MathF.Abs(vel.x) < 300) {
			vel.x += Global.spf * xDir * 900f;
			if (System.MathF.Abs(vel.x) >= 300) {
				vel.x = (float)xDir * 300;
			}
		}
	}

	public override void onReflect() {
		vel.x = 300;
		base.onReflect();
	}
}

public class ProtoBusterLv2Proj : Projectile {
	public ProtoBusterLv2Proj(
		int type, Point pos, int xDir, Player player,
		ushort? netId, bool rpc = false
	) : base(
		ProtoBuster.netWeapon, pos, xDir, 325, 2, player,
		"rock_buster1_proj", Global.miniFlinch, 0.5f, netId, player.ownedByLocalPlayer
	) {
		fadeSprite = "rock_buster1_fade";
		fadeOnAutoDestroy = true;
		maxTime = 0.4125f;
		projId = (int)BluesProjIds.BusterLV2;
		if (type == 1) {
			damager.flinch = Global.halfFlinch;
			changeSprite("rock_buster2_proj", true);
			fadeSprite = "rock_buster2_fade";
		}

		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type };

			rpcCreate(pos, player, netId, xDir, extraArgs);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new ProtoBusterLv2Proj(
			args.extraData[0], args.pos, args.xDir, args.player, args.netId
		);
	}
}


public class ProtoBusterLv3Proj : Projectile {
	public ProtoBusterLv3Proj(
		int type, Point pos, int xDir, Player player,
		ushort? netId, bool rpc = false
	) : base(
		ProtoBuster.netWeapon, pos, xDir, 325, 3, player,
		"proto_chargeshot_red_proj", Global.halfFlinch, 0.5f, netId, player.ownedByLocalPlayer
	) {
		fadeSprite = "proto_chargeshot_red_proj_fade";
		fadeOnAutoDestroy = true;
		maxTime = 0.45f;
		projId = (int)BluesProjIds.BusterLV4;
		if (type == 1) {
			damager.flinch = Global.defFlinch;
		}

		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type };

			rpcCreate(pos, player, netId, xDir, extraArgs);
		}
	}


	public static Projectile rpcInvoke(ProjParameters args) {
		return new ProtoBusterLv3Proj(
			args.extraData[0], args.pos, args.xDir, args.player, args.netId
		);
	}
}

public class ProtoBusterLv4Proj : Projectile {
	public ProtoBusterLv4Proj(
		int type, Point pos, int xDir, Player player,
		ushort? netId, bool rpc = false
	) : base(
		ProtoBuster.netWeapon, pos, xDir, 325, 4, player,
		"proto_chargeshot_proj", Global.defFlinch, 0.5f, netId, player.ownedByLocalPlayer
	) {
		fadeSprite = "proto_chargeshot_proj_fade";
		fadeOnAutoDestroy = true;
		maxTime = 0.5f;
		projId = (int)BluesProjIds.BusterLV4;
		if (type == 1) {
			damager.flinch = Global.superFlinch;
		}

		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type };

			rpcCreate(pos, player, netId, xDir, extraArgs);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new ProtoBusterLv4Proj(
			args.extraData[0], args.pos, args.xDir, args.player, args.netId
		);
	}
}
