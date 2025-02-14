using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Converters;

namespace MMXOnline;

public class IceWall : Weapon {
	public static IceWall netWeapon = new();
	public IceWallProj wall = null!;

	public IceWall() : base() {
		index = (int)BassWeaponIds.IceWall;
		displayName = "ICE WALL";
		maxAmmo = 14;
		ammo = maxAmmo;
		weaponSlotIndex = index;
		weaponBarBaseIndex = index;
		weaponBarIndex = index;
		fireRate = 60;
	}

	public override bool canShoot(int chargeLevel, Character character) {
		return base.canShoot(chargeLevel, character) && (wall == null || wall?.destroyed == true);
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Bass bass = character as Bass ?? throw new NullReferenceException();
		Point shootPos = character.getShootPos().addxy(0, 2);
		Player player = character.player;

		wall = new IceWallProj(bass, shootPos, bass.getShootXDir(), player.getNextActorNetId(), true);
		bass.playSound("icewall", true);
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
			//new IceWallProj(pos, xDir, player, player.getNextActorNetId(), true);
		}
	}
}
	
public class IceWallProj : Projectile, IDamagable {
	float lastDeltaX = 0;
	float maxSpeed = 300;
	int bounces;
	bool startedMoving;
	List<Character> chrs = new();
	Player player;
	Collider? terrainCollider;
	float health = 4;

	public IceWallProj(
		Actor owner, Point pos, int xDir, ushort? netId, 
		bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "ice_wall_proj", netId, altPlayer
	) {
		projId = (int)BassProjIds.IceWall;
		useGravity = true;
		canBeLocal = false;
		base.xDir = xDir;
		this.player = ownerPlayer;
		//collider.isTrigger = false;
		isSolidWall = true;
		isPlatform = true;
		maxTime = 2f;
		destroyOnHit = false;
		splashable = true;
		fadeOnAutoDestroy = true;
		damager.hitCooldown = 60;
		Global.level.modifyObjectGridGroups(this, isActor: true, isTerrain: true);

		if (rpc) rpcCreate(pos, owner, ownerPlayer, netId, xDir);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new IceWallProj(
			arg.owner, arg.pos, arg.xDir, arg.netId, altPlayer: arg.player
		);
	}
	
	public override void update() {
		base.update();
		if (deltaPos.x != 0) {
			lastDeltaX = deltaPos.x;
		}
		if (!ownedByLocalPlayer) {
			return;
		}

		if (startedMoving && Math.Abs(vel.x) < maxSpeed) {
			vel.x += xDir * 5 *	Global.speedMul;
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
		// Hit enemy.
		if (other.gameObject is Character character) {
			if (character.player.alliance != damager.owner.alliance || character.player == damager.owner) {
				if (MathF.Sign(character.pos.x - pos.x) == MathF.Sign(lastDeltaX)) {
					character.move(new Point(lastDeltaX * 0.9f, 0), useDeltaTime: false);
				} else {
					character.move(new Point(
						2 * MathF.Sign(character.pos.x - pos.x), 0
					), useDeltaTime: false);
				}
			}
		}
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
	}

	public void applyDamage(float damage, Player owner, Actor? actor, int? weaponIndex, int? projId) {
		health -= damage;
	}
	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) => health > 0;
	public bool isInvincible(Player attacker, int? projId) => false;
	public bool canBeHealed(int healerAlliance) => false;
	public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = true) { }
	public bool isPlayableDamagable() => false;
}
