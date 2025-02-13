using System;
using System.Collections.Generic;

namespace MMXOnline;

public class PowerStone : Weapon {
	public static PowerStone netWeapon = new();

	public PowerStone() : base() {
		displayName = "POWER STONE";
		descriptionV2 = "Summons three stones that spiral around.\nCan be used behind the shield.";
		defaultAmmoUse = 4;

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
		Blues blues = character as Blues ?? throw new NullReferenceException();

		if (args[1] == 1) {
			if (character.charState is not LadderClimb lc) character.changeState(new BluesShootAlt(this), true);
			else character.changeState(new BluesShootAltLadder(this, lc.ladder), true);
			blues.playSound("super_adaptor_punch");
		} else if (args[1] == 2) {
			new PowerStoneProj(blues, shootPos, xDir, 0, character.player.getNextActorNetId(), true);
			new PowerStoneProj(blues, shootPos, xDir, 1, character.player.getNextActorNetId(), true);
			new PowerStoneProj(blues, shootPos, xDir, 2, character.player.getNextActorNetId(), true);
		}
	}
}

public class PowerStoneProj : Projectile {
	Character? character;
	Point origin;
	int stoneAngle = 120;
	float radius = 10;

	public PowerStoneProj(
		Actor owner, Point pos, int xDir, int type, 
		ushort? netId, bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "power_stone_proj", netId, altPlayer
	) {
		projId = (int)BluesProjIds.PowerStone;
		maxTime = 1;

		character = ownerPlayer.character;
		stoneAngle = type * 85;
		zIndex = ZIndex.Character - 10;
		destroyOnHit = false;
		canBeLocal = false;

		damager.damage = 2;
		damager.hitCooldown = 60;
		
		origin = pos;
		if (character != null) {
			origin = character.getCenterPos();
		}
		changePos(new Point(
			origin.x + Helpers.cosb(stoneAngle) * radius,
			origin.y + Helpers.sinb(stoneAngle) * radius
		));

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new PowerStoneProj(
			args.owner, args.pos, args.xDir, args.extraData[0], args.netId, altPlayer: args.player
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

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);

		if (damagable.canBeDamaged(damager.owner.alliance, damager.owner.id, projId)) {
			if (damagable.projectileCooldown.ContainsKey(projId + "_" + owner.id) &&
				damagable.projectileCooldown[projId + "_" + owner.id] >= damager.hitCooldown
			) {
				destroySelfNoEffect(disableRpc: true, true);
			}
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		Anim.createGibEffect("power_stone_pieces_big", pos, null!, zIndex: zIndex);
	}
} 
