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
		fireRate = 90;
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
	Character character;
	int stoneAngle = 120;
	float radius = 10;
	int type;

	public PowerStoneProj(
		Point pos, int xDir, Player player, int type, ushort? netId, bool rpc = false
	) : base(
		PowerStone.netWeapon, pos, xDir, 0, 2, player, "power_stone_proj",
		0, 0.25f, netId, player.ownedByLocalPlayer
	) {
		projId = (int)BluesProjIds.PowerStone;
		maxTime = 1;

		character = player.character;
		this.type = type;
		stoneAngle = type * 120;
		zIndex = ZIndex.Character - 10;

		changePos(new Point(
			character.getCenterPos().x + Helpers.cosd(stoneAngle) * radius,
			character.getCenterPos().y + Helpers.sind(stoneAngle) * radius
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

		pos.x = character.getCenterPos().x + Helpers.cosd(stoneAngle) * radius;
		pos.y = character.getCenterPos().y + Helpers.sind(stoneAngle) * radius;

		stoneAngle += 8;
		if (stoneAngle >= 360) {
			stoneAngle = 0;
		}
		radius += 1.25f;
	}

	public override void onDestroy() {
		base.onDestroy();
		Anim.createGibEffect("power_stone_pieces_big", pos, null!, zIndex: zIndex);
	}
}
