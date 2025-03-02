using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Converters;

namespace MMXOnline;

public class IceWall : Weapon {
	public static IceWall netWeapon = new();
	public Actor wall = null!;

	public IceWall() : base() {
		index = (int)BassWeaponIds.IceWall;
		displayName = "ICE WALL";
		maxAmmo = 14;
		ammo = maxAmmo;
		weaponSlotIndex = index;
		weaponBarBaseIndex = index;
		weaponBarIndex = index;
		fireRate = 30;
	}

	public override bool canShoot(int chargeLevel, Character character) {
		return base.canShoot(chargeLevel, character) && (wall == null || wall?.destroyed == true);
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Bass bass = character as Bass ?? throw new NullReferenceException();
		Point shootPos = character.getShootPos().addxy(0, 2);
		Player player = character.player;

		wall = new IceWallStart(bass, shootPos, bass.getShootXDir(), player.getNextActorNetId(), player, this, true);
		bass.playSound("icewall", true);
	}
}


public class IceWallStart : Anim {
	Player player;
	Actor owner;
	Weapon weapon;

	public IceWallStart(
		Actor owner, Point pos, int xDir, ushort? netId, Player player,
		Weapon weapon, bool sendRpc = false, bool ownedByLocalPlayer = true
	) : base(
		pos, "ice_wall_spawn", xDir, netId, true, 
		sendRpc, ownedByLocalPlayer
	) {
		this.player = player;
		this.owner = owner;
		this.weapon = weapon;
	}

	public override void onDestroy() {
		base.onDestroy();
		if (ownedByLocalPlayer) {
			var wall = new IceWallProj(owner, pos, xDir, player.getNextActorNetId(), weapon, true, player);
			if (weapon is IceWall iw) iw.wall = wall;
		}
	}
}
	
public class IceWallProj : Projectile, IDamagable {
	public bool startedMoving;
	public bool isFalling;
	public float health = 2;
	float maxSpeed = 3f * 60;
	Weapon weapon;

	public IceWallProj(
		Actor owner, Point pos, int xDir, ushort? netId,
		Weapon weapon, bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "ice_wall_proj", netId, altPlayer
	) {
		projId = (int)BassProjIds.IceWall;
		damager.damage = 1;
		damager.hitCooldown = 140;

		fadeSprite = "ice_wall_fade";
		fadeOnAutoDestroy = true;
		useGravity = true;
		canBeLocal = false;
		base.xDir = xDir;
		isSolidWall = true;
		maxTime = 2f;
		destroyOnHit = false;
		splashable = true;
		this.weapon = weapon;
		Global.level.modifyObjectGridGroups(this, isActor: true, isTerrain: true);
		selectiveSolididyFunc = selectiveSolidity;

		if (rpc) rpcCreate(pos, owner, ownerPlayer, netId, xDir);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new IceWallProj(
			arg.owner, arg.pos, arg.xDir, arg.netId, IceWall.netWeapon, altPlayer: arg.player
		);
	}
	
	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) {
			return;
		}
		if (startedMoving && Math.Abs(vel.x) < maxSpeed) {
			vel.x += xDir * 0.1f * 60f;
			if (Math.Abs(vel.x) > maxSpeed) vel.x = maxSpeed * xDir;
		}
		if (isUnderwater()) {
			grounded = false;
			gravityModifier = -1;
			if (Math.Abs(vel.y) > Physics.MaxUnderwaterFallSpeed * 0.5f) {
				vel.y = Physics.MaxUnderwaterFallSpeed * 0.5f * gravityModifier;
			}
		} else {
			gravityModifier = 1;
		}
		isFalling = deltaPos.y > 0;
	}

	public bool selectiveSolidity(GameObject other) {
		if (other is not Character chara) {
			return false;
		}
		if (!chara.canBeDamaged(damager.owner.alliance, damager.owner.id, projId) && chara.player != damager.owner) {
			return false;
		}

		// Fully solid for enemies.
		if ((chara.player == damager.owner || chara.player.alliance != damager.owner.alliance) &&
			chara.charState is not LadderClimb
		) {
			return true;
		}
		// Platform-like behaviour for allies.
		if (chara.pos.y <= getTopY() + 16) {
			return true;
		}
		return false;
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		// Wall hit.
		if (other.gameObject is Wall) {
			if (other.isSideWallHit()) {
				xDir *= -1;
				vel.x *= -1;
				pos.y += xDir;
				playSound("ding");
			}
			return;
		}
		// Hit enemy.
		if (other.gameObject is not Actor actor || !actor.ownedByLocalPlayer || actor is not Character) {
			return;
		}
		Character? ownChar = damager.owner?.character;
		// Movement start.
		if (other.gameObject == ownChar && !startedMoving) {
			if (ownChar.pos.y >= getTopY() + 10 && ownChar.charState is Dash or Run or TenguBladeDash) {
				startedMoving = true;
				xDir = MathF.Sign(pos.x - ownChar.pos.x) >= 0 ? 1 : -1;
				vel.x = xDir * 30;
			}
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		if (ownedByLocalPlayer && weapon is IceWall iw) iw.wall = null!;
	}

	public void applyDamage(float damage, Player owner, Actor? actor, int? weaponIndex, int? projId) {
		health -= damage;
		if (health <= 0) {
			destroySelf();
		}
	}
	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) {
		return health > 0 && damagerAlliance != damager.owner.alliance && projId == (int)BassProjIds.IceWall;
	}
	public bool isInvincible(Player attacker, int? projId) => false;
	public bool canBeHealed(int healerAlliance) => false;
	public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = true) { }
	public bool isPlayableDamagable() => false;

	public override List<byte> getCustomActorNetData() {
		return [Helpers.boolArrayToByte([
			startedMoving,
			isFalling
		])];
	}
	public override void updateCustomActorNetData(byte[] data) {
		bool[] flags = Helpers.byteToBoolArray(data[0]);
		startedMoving = flags[0];
		isFalling = flags[1];
	}
}
