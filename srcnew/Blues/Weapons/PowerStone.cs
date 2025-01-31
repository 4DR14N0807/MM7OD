using System;
using System.Collections.Generic;

namespace MMXOnline;

public class PowerStone : Weapon {
	public static PowerStone netWeapon = new();

	public PowerStone() : base() {
		displayName = "POWER STONE";
		descriptionV2 = "Summons three stones that spiral around.\nCan be used behind the shield.";
		defaultAmmoUse = 5;

		index = (int)BluesWeaponIds.PowerStone;
		fireRate = 55;
		hasCustomAnim = true;
	}

	public override float getAmmoUsage(int chargeLevel) {
		return defaultAmmoUse;
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Point shootPos = character.getShootPos();
		int xDir = character.getShootXDir();
		(character as Blues)?.resetCoreCooldown(fireRate + 10);

		if (args[1] == 1) {
			if (character.charState is not LadderClimb lc) character.changeState(new BluesShootAlt(this), true);
			else character.changeState(new BluesShootAltLadder(this, lc.ladder), true);
		} else if (args[1] == 2) {
			new PowerStoneProj(shootPos, xDir, character.player, 0, character.player.getNextActorNetId(), true);
			new PowerStoneProj(shootPos, xDir, character.player, 1, character.player.getNextActorNetId(), true);
			new PowerStoneProj(shootPos, xDir, character.player, 2, character.player.getNextActorNetId(), true);
		}
	}
}

public class PowerStoneProj : Projectile {
	Character? character;
	Point origin;
	int stoneAngle = 120;
	float radius = 10;

	public PowerStoneProj(
		Point pos, int xDir, Player player, int type, ushort? netId, bool rpc = false
	) : base(
		PowerStone.netWeapon, pos, xDir, 0, 2, player, "power_stone_proj",
		0, 1f, netId, player.ownedByLocalPlayer
	) {
		projId = (int)BluesProjIds.PowerStone;
		maxTime = 1;

		character = player.character;
		stoneAngle = type * 85;
		zIndex = ZIndex.Character - 10;
		destroyOnHit = false;
		canBeLocal = false;
		origin = pos;
		if (character != null) {
			origin = character.getCenterPos();
		}
		changePos(new Point(
			origin.x + Helpers.cosb(stoneAngle) * radius,
			origin.y + Helpers.sinb(stoneAngle) * radius
		));

		if (rpc) {
			rpcCreate(pos, player, netId, xDir, (byte)type);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new PowerStoneProj(
			args.pos, args.xDir, args.player, args.extraData[0], args.netId
		);
	}

	public override void update() {
		base.update();
		if (character != null) {
			origin = character.getCenterPos();
		}
		changePos(new Point(
			origin.x + Helpers.cosb(stoneAngle) * radius,
			origin.y + Helpers.sinb(stoneAngle) * radius
		));

		stoneAngle += 6;
		radius += 1.25f;
	}

	public override void onDestroy() {
		base.onDestroy();
		Anim.createGibEffect("power_stone_pieces_big", pos, null!, zIndex: zIndex);
	}
} 
