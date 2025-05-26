using System;
using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;

public class Slashman : Character {
	public Slashman(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer,
		bool isWarpIn = true
	) : base(
		player, x, y, xDir, isVisible, netId, ownedByLocalPlayer, isWarpIn
	) {
		charId = CharIds.Slashman;
		charge1Time = 50;
		maxHealth -= (decimal)player.evilEnergyStacks * (decimal)player.hpPerStack;
	}

	public override void update() {
		base.update();
		//Non-local players end here.
		if (!ownedByLocalPlayer) return;
	}

	public enum MeleeIds {
		None = -1,
		IdleSlash,
	}

	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"slashman_attack" => MeleeIds.IdleSlash,
			_ => MeleeIds.None
		});
	}

	public override Projectile? getMeleeProjById(int id, Point projPos, bool addToLevel = true) {
		return id switch {
			(int)MeleeIds.IdleSlash => new GenericMeleeProj(
				new Weapon(), projPos, ProjIds.SlashmanIdle, player,
				2, Global.halfFlinch,
				addToLevel: addToLevel
			),
			_ => null
		};
	}

	public override bool attackCtrl() {
		return base.attackCtrl();
	}

	public override bool canCrouch() {
		return false;
	}

	public override bool canMove() {
		return base.canMove();
	}

	public override bool canWallClimb() {
		return true;
	}

	public override (float, float) getGlobalColliderSize() {
		return (24, 30);
	}

	public override string getSprite(string spriteName) {
		return "slashman_" + spriteName;
	}

	public override (string, int) getBaseHpSprite() {
		return ("hud_health_base", 2);
	}

	public override List<byte> getCustomActorNetData() {
		// Get base arguments.
		List<byte> customData = base.getCustomActorNetData();
		return customData;
	}

	public override void updateCustomActorNetData(byte[] data) {
		// Update base arguments.
		if (data[0] > 1) { base.updateCustomActorNetData(data); }
		data = data[data[0]..];
	}
}

