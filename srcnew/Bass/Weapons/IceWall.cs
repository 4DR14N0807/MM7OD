using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Converters;

namespace MMXOnline;

public class IceWall : Weapon {
	public static IceWall netWeapon = new();
	public IceWallProj? wall;

	public IceWall() : base() {
		iconSprite = "hud_weapon_icon_bass";
		index = (int)BassWeaponIds.IceWall;
		displayName = "ICE WALL";
		maxAmmo = 14;
		ammo = maxAmmo;
		weaponSlotIndex = index;
		weaponBarBaseIndex = index;
		weaponBarIndex = index;
		//fireRate = 30;
		fireRate = 10;
		descriptionV2 = [
			[ "Pushes enemies away.\n" +
			"Can be used as a platform and transport\n" +
			"both you and your teammates." ]
		];
	}

	public override void update() {
		base.update();
		if (ammo <= 0 || wall?.destroyed == false) {
			isStream = true;
		} else {
			isStream = false;
		}
	}

	public override bool canShoot(int chargeLevel, Character character) {
		return true;
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 0;
	}

	public override void shoot(Character character, params int[] args) {
		Point shootPos = character.getShootPos();
		Player player = character.player;
		if (!player.ownedByLocalPlayer) return;
		Bass bass = character as Bass ?? throw new NullReferenceException();
	
		if (ammo > 0 && !isStream && wall?.destroyed != false) {
			wall = new IceWallProj(
				bass, shootPos, bass.getShootXDir(),
				player.getNextActorNetId(), rpc: true
			);
			bass.playSound("icewall", true);
			addAmmo(-1, player);
		} else {
			new IceWallLemon(bass, shootPos, bass.xDir, player.getNextActorNetId(), true);
			bass.playSound("bassbuster", true);
		}
	}
}

public class IceWallProj : Projectile, IDamagable {
	public bool startedMoving;
	public bool isFalling;
	public float health = 2;
	float maxSpeed = 3f * 60;

	public IceWallProj(
		Actor owner, Point pos, int xDir, ushort? netId,
		bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "ice_wall_spawn", netId, altPlayer
	) {
		projId = (int)BassProjIds.IceWall;
		damager.damage = 1;
		damager.hitCooldown = 140;

		fadeSprite = "ice_wall_fade";
		fadeOnAutoDestroy = true;
		canBeLocal = false;
		base.xDir = xDir;
		isSolidWall = true;
		maxTime = 2f;
		destroyOnHit = false;
		splashable = true;
		Global.level.modifyObjectGridGroups(this, isActor: true, isTerrain: true);
		selectiveSolididyFunc = selectiveSolidity;

		if (rpc) rpcCreate(pos, owner, ownerPlayer, netId, xDir);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new IceWallProj(
			arg.owner, arg.pos, arg.xDir, arg.netId, altPlayer: arg.player
		);
	}
	
	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) {
			return;
		}
		if (sprite.name == "ice_wall_spawn" && isAnimOver()) {
			changeSprite("ice_wall_proj", true);
			useGravity = true;
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

	public override bool canBePlatform(GameObject other) {
		if (other is RemoteMineProj or RemoteMineLandProj) return true;

		return (
			other is Character chara &&
			chara.player.alliance == damager.owner.alliance &&
			chara.pos.y <= getTopY() + 16
		); 
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (sprite.name == "ice_wall_spawn") return;
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
			if (ownChar.pos.y >= getTopY() + 10 && (ownChar.charState is Dash or Run or TenguBladeDash || (ownChar.canMove() && ownChar.player.input.getXDir(ownChar.player) != 0))) {
				startedMoving = true;
				xDir = MathF.Sign(pos.x - ownChar.pos.x) >= 0 ? 1 : -1;
				vel.x = xDir * 30;
			}
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		if (!ownedByLocalPlayer) return;
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

public class IceWallLemon : Projectile {
	public IceWallLemon(
		Actor owner, Point pos, int xDir, ushort? netProjId, 
		bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "copy_vision_lemon", netProjId, altPlayer
	) {
		projId = (int)BassProjIds.IceWallLemon;
		maxTime = 0.525f;
		fadeSprite = "copy_vision_lemon_fade";

		vel.x = 240 * xDir;
		damager.damage = 0.5f;
		damager.hitCooldown = 9;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netProjId, xDir);
		}
		addRenderEffect(RenderEffectType.ChargeBlue);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new IceWallLemon(
			arg.owner, arg.pos, arg.xDir, arg.netId
		);
	}
}
