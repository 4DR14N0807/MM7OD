using System;
using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;

// Thunder revenge.
// This chip generate sharpnel when flinched on stunned.
// Has a 4s cooldown.
public class ThunderRevenge : Chip {
	public float maxCooldown = 60 * 4;
	public float cooldown;

	public ThunderRevenge() {
		id = ChipId.ThunderRevenge;
		onFlinch = onFlinchOrStun;
		onStun = onFlinchOrStun;
	}

	// Update cooldown.
	public override void update(Character chara) {
		base.update(chara);
		// Update cooldown.
		Helpers.decrementFrames(ref cooldown);
	}

	public float onFlinchOrStun(
		Character chara, float val, Actor? damager,
		Player? enemyPlayer, Character? enemyChar, float flinchTime
	) {
		// Skip if on-cooldown or if we are not flinched.
		if (cooldown > 0 || flinchTime <= Global.miniFlinch) {
			return flinchTime;
		}
		// Randonly offset pos.
		Point pos = damager?.pos ?? chara.pos;
		pos.x += Helpers.randomRange(-12, 12);
		pos.y += Helpers.randomRange(-38, -10);

		// Generate sharpnel.
		new ThunderSharpnel(chara, pos, chara.xDir, chara.player.getNextActorNetId(), true);

		// Exit.
		return flinchTime;
	}
}

// Projectile.
public class ThunderSharpnel : Projectile {
	public ThunderSharpnel(
		Actor owner, Point pos, int xDir, ushort? netId, 
		bool sendRpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "explosion", netId, altPlayer
	) {
		maxTime = 4;
		projId = (int)GenericProjIds.ThunderSharpnel;
		damager.damage = 1;

		if (sendRpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public override void update() {
		base.update();
		if (isAnimOver()) {
			destroySelf(disableRpc: true);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new ThunderSharpnel(
			args.owner, args.pos, args.xDir, args.netId, altPlayer: args.player
		);
	}
}
