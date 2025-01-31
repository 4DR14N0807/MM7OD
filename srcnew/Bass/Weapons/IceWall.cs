using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Converters;

namespace MMXOnline;

public class IceWall : Weapon {
	public static IceWall netWeapon = new();

	public IceWall() : base() {
		index = (int)BassWeaponIds.IceWall;
		displayName = "ICE WALL";
		maxAmmo = 10;
		ammo = maxAmmo;
		weaponSlotIndex = index;
		weaponBarBaseIndex = index;
		weaponBarIndex = index;
		fireRate = 120;
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Point shootPos = character.getShootPos().addxy(0, 2);
		Player player = character.player;

		new IceWallProj(shootPos, character.getShootXDir(), player, player.getNextActorNetId(), true);
		character.playSound("icewall", true);
	}
}


public class IceWallStart : Anim {
	Player player;

	public IceWallStart(
		Point pos, int xDir, ushort? netId, Player player,
		bool sendRpc = false, bool ownedByLocalPlayer = true
	) : base(
		pos, "ice_wall_spawn", xDir, netId, true, 
		sendRpc, ownedByLocalPlayer, player.character
	) {
		this.player = player;
	}

	public override void onDestroy() {
		base.onDestroy();
		if (ownedByLocalPlayer) {
			new IceWallProj(pos, xDir, player, player.getNextActorNetId(), true);
		}
	}
}
	
public class IceWallProj : Projectile {
	float maxSpeed = 300;
	int bounces;
	bool startedMoving;
	List<Character> chrs = new();
	Player player;
	Collider? terrainCollider;

	public IceWallProj(
		Point pos, int xDir, Player player,
		ushort? netId, bool rpc = false
	) : base(
		IceWall.netWeapon, pos, xDir, 0, 0, player, "ice_wall_proj", 0, 0, netId, player.ownedByLocalPlayer
	) {
		projId = (int)BassProjIds.IceWall;
		useGravity = true;
		canBeLocal = false;
		base.xDir = xDir;
		this.player = player;
		collider.isTrigger = false;
		isSolidWall = true;
		maxTime = 2f;
		destroyOnHit = false;
		splashable = true;
		Global.level.modifyObjectGridGroups(this, isActor: true, isTerrain: true);

		if (rpc) {
			rpcCreate(pos, player, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new IceWallProj(
			arg.pos, arg.xDir, arg.player, arg.netId
		);
	}
	
	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) {
			return;
		}

		if (startedMoving && Math.Abs(vel.x) < maxSpeed) {
			vel.x += xDir * Global.speedMul * 7.5f;
			if (Math.Abs(vel.x) > maxSpeed) vel.x = maxSpeed * xDir;
		}

		if (isUnderwater()) {
			grounded = false;
			gravityModifier = -1;
			if (Math.Abs(vel.y) > Physics.MaxUnderwaterFallSpeed * 0.5f) {
				vel.y = Physics.MaxUnderwaterFallSpeed * 0.5f * gravityModifier;
			}
		} else gravityModifier = 1;

		if (bounces >= 3) {
			destroySelf();
		}
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (!ownedByLocalPlayer) {
			return;
		}
		Character? ownChar = damager.owner?.character;
		// Wall hit.
		if (other.gameObject is Wall) {
			if (other.isSideWallHit()) {
				xDir *= -1;
				vel.x *= -1;
				pos.y += xDir;
				playSound("ding");
				bounces++;
			}
			return;
		}
		// Movement start.
		if (other.gameObject == ownChar && !startedMoving) {
			if (ownChar.pos.y > getTopY() + 10 && ownChar.charState is Run or Dash) {
				startedMoving = true;
				xDir = ownChar.xDir;
			}
		}
		// Hit enemy.
		else if (other.gameObject is Character chara && chara.player.alliance != damager?.owner?.alliance) {
			if (other.isSideWallHit()) {
				foreach (var enemy in chrs) {
					if (chara != enemy) {
						chrs.Add(chara);
						maxSpeed -= 100;
					} 
				}
			} else if (other.isGroundHit() && vel.y > 120 && 
				chara.canBeDamaged(player.alliance, player.id, (int)BassProjIds.IceWall)) {

				chara.applyDamage(3, player, chara, (int)BassWeaponIds.IceWall, (int)BassProjIds.IceWall);
			}
		}
	}
}
