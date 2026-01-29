using System;
using System.Collections.Generic;

namespace MMXOnline;

public class Freezeman : Character {
	public FreezemanWeapon mainWeapon;
	public bool isBlocking;
	public bool onFreezeZone;
	public bool onIceAlt;

	public Freezeman(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer, bool isWarpIn = true
	) : base(
		player, x, y, xDir, isVisible, netId, ownedByLocalPlayer, isWarpIn
	) {
		spriteFrameToSounds["freezem_run/0"] = "freezemWalk";
		spriteFrameToSounds["freezem_run/4"] = "freezemWalk";

		mainWeapon = new FreezemanWeapon();
		weapons = [mainWeapon];
	}

	public override void preUpdate() {
		base.preUpdate();

		if (ownedByLocalPlayer) {
			return;
		}
		// Reset variables.
		onFreezeZone = false;
		onIceAlt = false;
		// Look for ice zones.
		List<CollideData> fmzList = Global.level.getTerrainTriggerList(
			getTerrainCollider().shape, typeof(FreezemanZone)
		);
		if (fmzList.Count > 0) {
			onFreezeZone = true;
			onIceAlt = true;
		}
		if (groundedIce) {
			onIceAlt = true;
		}
		if (!onIceAlt) {
			CollideData? floor = Global.level.checkTerrainCollisionOnce(
				this, 0, 1, checkPlatforms: true, checkQuicksand: true,
				condition: (go) => go is Wall { slippery: true }
			);
			if (floor != null) {
				onIceAlt = true;
			}
		}
	}

	public override bool attackCtrl() {
		return false;
	}
	
	public override float getRunSpeed() {
		if (flag != null) {
			return base.getRunSpeed();
		}
		float speed = Physics.WalkSpeed;
		if (onIceAlt || groundedIce || onFreezeZone) {
			speed = 2;
		}
		return speed * getRunDebuffs();
	}
	public override bool canCrouch() => false;
	public override bool canDash() => false;
	public override bool canAirDash() => false;
	public override bool canWallClimb() => onFreezeZone && base.canWallClimb();
	public override (float x, float y) getGlobalColliderSize() => (18, 32);
	public override bool isStunImmune() => base.isStunImmune();

	public enum MeleeIds {
		None = -1,
		DownAttack
	}
	
	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			_ => MeleeIds.None
		});
	}

	public override Projectile? getMeleeProjById(int id, Point projPos, bool addToLevel = true) {
		Projectile? proj = id switch {
			(int)MeleeIds.DownAttack => new GenericMeleeProj(
				mainWeapon, projPos, ProjIds.FreezeMDownAttack,
				player, 1, Global.miniFlinch, 30, addToLevel: addToLevel
			),
			_ => null
		};
		return proj;

	}

	public override List<byte> getCustomActorNetData() {
		// Get base arguments.
		List<byte> customData = base.getCustomActorNetData();

		// Per-character data.
		bool[] flags = [
			onFreezeZone
		];
		customData.Add(Helpers.boolArrayToByte(flags));

		return customData;
	}

	public override void updateCustomActorNetData(byte[] data) {
		// Update base arguments.
		base.updateCustomActorNetData(data);
		data = data[data[0]..];

		// Per-character data.
		bool[] flags = Helpers.byteToBoolArray(data[0]);
		onFreezeZone = flags[0];
	}
}