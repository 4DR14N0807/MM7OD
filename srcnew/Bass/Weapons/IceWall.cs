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
		maxAmmo = 10;
		ammo = maxAmmo;
		weaponSlotIndex = index;
		weaponBarBaseIndex = index;
		weaponBarIndex = index;
		switchCooldown = 10;
		fireRate = 10;
		descriptionV2 = [
			[ "Pushes enemies away.\n" +
			"Can be used as a platform and transport\n" +
			"for both you and your teammates." ]
		];
	}

	public override void update() {
		base.update();
		if (ammo <= 0 || wall?.destroyed == false) {
			isStream = true;
		} else {
			isStream = false;
		}
		if (ammo <= 0 && wall?.destroyed != false) {
			fireRate = 18;
		} else {
			fireRate = 10;
		}		 
	}

	public override bool canShoot(int chargeLevel, Character character) {
		return true;
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 0;
	}

	public override void shoot(Character character, params int[] args) {
		if (character is not Bass bass) {
			return;
		}
		Point shootPos = character.getShootPos();
		Player player = character.player;
		if (!player.ownedByLocalPlayer) return;

		if (ammo > 0 && !isStream && wall?.destroyed != false) {
			wall = new IceWallProj(
				bass, shootPos.addxy(0, 23), bass.getShootXDir(),
				player.getNextActorNetId(), rpc: true
			);
			bass.playSound("icewall", true);
			addAmmo(-1, player);
		} else {
			int shootAngle = bass.getShootAngle(allowUp: false);
			new IceWallLemon(bass, shootPos, shootAngle, player.getNextActorNetId(), true);
			new Anim(
				shootPos, "bass_icewall_lemon_fade",
				character.xDir, player.getNextActorNetId(), true, true,
				host: bass, zIndex: ZIndex.Character + 2
			);
			bass.playSound("bassbuster", true);
		}
	}
}

public class IceWallProj : Projectile, IDamagable {
	public bool startedMoving;
	public bool isFalling;
	public float health = 6;
	float maxSpeed = 3.5f * 60;
	float groundTime;
	float soundCooldown;

	public IceWallProj(
		Actor owner, Point pos, int xDir, ushort? netId,
		bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "ice_wall_spawn", netId, altPlayer
	) {
		projId = (int)BassProjIds.IceWall;
		damager.damage = 1;
		damager.hitCooldown = 140;

		useGravity = true;
		fadeSprite = "ice_wall_fade";
		fadeOnAutoDestroy = true;
		canBeLocal = false;
		base.xDir = xDir;
		isSolidWall = true;
		maxTime = 2.5f;
		destroyOnHit = false;
		splashable = true;
		Global.level.modifyObjectGridGroups(this, isActor: true, isTerrain: true);
		selectiveSolididyFunc = selectiveSolidity;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
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

		Helpers.decrementFrames(ref soundCooldown);

		if (sprite.name == "ice_wall_spawn" && isAnimOver()) {
			changeSprite("ice_wall_proj", true);
		}
		if (health > 2 && health <= 4 && sprite.name != "ice_wall_proj_crack") {
			changeSprite("ice_wall_proj_crack", true);
		}
		if (health <= 2 && sprite.name != "ice_wall_proj_crack2") {
			changeSprite("ice_wall_proj_crack2", true);
		}
		if (startedMoving && Math.Abs(vel.x) < maxSpeed) {
			vel.x += xDir * 0.075f * 60f;
			if (Math.Abs(vel.x) > maxSpeed) {
				vel.x = maxSpeed * xDir;
			}
		}
		if (isUnderwater()) {
			grounded = false;
			gravityModifier = 0.5f;
			float terminalVel = Physics.MaxUnderwaterFallSpeed * 0.5f;
			if (Math.Abs(vel.y) > terminalVel) {
				vel.y = terminalVel * Math.Abs(vel.y);
			}
		} else {
			gravityModifier = 1;
		}
		isFalling = deltaPos.y > 0;

		if (grounded && startedMoving) {
			if (groundTime % 10 == 0) {
				new Anim(pos.addxy(-11 * xDir, 0), "ice_wall_sled", xDir, null, true, ownedByLocalPlayer);
			}
			groundTime += Global.speedMul;
		} else {
			groundTime = 0;
		}
	}

	public bool selectiveSolidity(GameObject other) {
		if (other is not Character chara) {
			return false;
		}
		if (!chara.canBeDamaged(damager.alliance, damager.owner.id, projId) && chara.player != damager.owner) {
			return false;
		}

		// Fully solid for enemies.
		if ((chara.player == damager.owner || chara.player.alliance != damager.alliance) &&
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
		if (other is RemoteMineProj or RemoteMineLandProj or
			DangerWrapLandRmProj or DangerWrapMineRmProj or IceWall
		) {
			return true;
		}
		return (
			other is Character chara &&
			chara.player.alliance == damager.alliance &&
			chara.pos.y <= getTopY() + 16
		);
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		// Wall hit.
		if (startedMoving && other.gameObject is Wall or OneWay or IceWall) {
			if (other.isSideWallHit()) {
				xDir *= -1;
				vel.x *= -1;
				incPos(xDir, 0);
				if (soundCooldown <= 0) {
					playSound("icewallBounce", ownedByLocalPlayer);
					soundCooldown = 15;
				}
			}
			return;
		}
		// Hit enemy.
		if (other.gameObject is not Actor actor || !actor.ownedByLocalPlayer || actor is not Character) {
			return;
		}
		Character? ownChar = damager.owner?.character;
		// Movement start.
		if (!startedMoving && other.gameObject == ownChar &&
			ownChar.pos.y >= getTopY() + 10 && (
				ownChar.charState is Dash or BaseRun or TenguBladeDash ||
				(ownChar.canMove() && ownChar.player.input.getXDir(ownChar.player) != 0)
			)
		) {
			startMove(MathF.Sign(pos.x - ownChar.pos.x) >= 0 ? 1 : -1);
		}
	}

	public void startMove(int moveDir) {
		if (startedMoving) {
			return;
		}
		startedMoving = true;
		xDir = moveDir;
		vel.x = xDir * 30;
		time = 0;
	}

	public override void onDestroy() {
		base.onDestroy();
		if (!ownedByLocalPlayer) return;
	}

	public override void afterDamage(IDamagable damagable, bool wasHit) {
		base.afterDamage(damagable, wasHit);
		if (!ownedByLocalPlayer) {
			return;
		}
		if (wasHit) {
			applyDamage(2, ownerPlayer, this, null, null);
		}
	}

	public void applyDamage(float damage, Player owner, Actor? actor, int? weaponIndex, int? projId) {
		health -= damage;
		if (health <= 0) {
			destroySelf();
		}
	}
	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) {
		return health > 0 && damagerAlliance != damager.alliance && (
			projId == null || projId == (int)BassProjIds.IceWall
		);
	}
	public bool isInvincible(Player attacker, int? projId) => false;
	public bool canBeHealed(int healerAlliance) => false;
	public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = true) { }
	public bool isPlayableDamagable() => false;

	public override Collider? getTerrainCollider() {
		if (spriteToColliderMatch(sprite.name, out Collider? overrideGlobalCollider)) {
			return overrideGlobalCollider;
		}
		(float xSize, float ySize) = (14, 30);

		return new Collider(
			new Rect(0, 0, xSize, ySize).getPoints(),
			false, this, false, false,
			HitboxFlag.Hurtbox, Point.zero
		);
	}

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
		Actor owner, Point pos, float byteAngle, ushort? netProjId, 
		bool rpc = false, Player? altPlayer = null
	) : base(
		pos, 1, owner, "bass_icewall_lemon", netProjId, altPlayer
	) {
		projId = (int)BassProjIds.IceWallLemon;
		maxTime = 36 / 60f;
		fadeSprite = "bass_icewall_lemon_fade";

		vel = Point.createFromByteAngle(byteAngle) * 240;
		this.byteAngle = byteAngle;
		damager.damage = 0.5f;
		destroyOnHitWall = true;

		if (rpc) {
			rpcCreateByteAngle(pos, owner, ownerPlayer, netProjId, this.byteAngle);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new IceWallLemon(
			arg.owner, arg.pos, arg.xDir, arg.netId
		);
	}
}
