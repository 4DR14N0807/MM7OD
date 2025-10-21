using System;
using System.Collections.Generic;
using SFML.Graphics;

namespace MMXOnline;

public class DangerWrap : Weapon {
	public static DangerWrap netWeapon = new();
	public List<Projectile> dangerMines = new();

	public DangerWrap() : base() {
		displayName = "DANGER WRAP";
		index = (int)RockWeaponIds.DangerWrap;
		weaponBarBaseIndex = (int)RockWeaponBarIds.DangerWrap;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = (int)RockWeaponSlotIds.DangerWrap;
		killFeedIndex = 0;
		fireRate = 75;
		switchCooldown = 45;
		maxAmmo = 12;
		ammo = maxAmmo;
		descriptionV2 = [
			[ "Complex weapon able to catch foes.\n" +
			"Press UP/LEFT/RIGHT to change direction\n" + 
			"or press DOWN to leave a mine." ]
		];
	}

	public override void update() {
		base.update();
		for (int i = dangerMines.Count - 1; i >= 0; i--) {
			if (dangerMines[i].destroyed) {
				dangerMines.RemoveAt(i);
				continue;
			}
		}
	}

	public override void shootRock(Rock rock, params int[] args) {
		base.shootRock(rock, args);
		Point shootPos = rock.getShootPos();
		int xDir = rock.getShootXDir();
		Player player = rock.player;
		int input = player.input.getYDir(player);
		if (player.input.getXDir(player) != 0) {
			if (input == 0) {
				input = 2;
			} else if (input == -1) {
				input = 0;
			}
		}
		if (input == 1) {
			if (dangerMines.Count >= 3) {
				if (dangerMines[0] is DangerWrapLandProj dwarpground) {
					dwarpground.health = 0;
				}
				dangerMines[0].destroySelf();
				dangerMines.RemoveAt(0);
			}
		}
		if (input == 1) {
			dangerMines.Add(
				new DangerWrapMineProj(rock, shootPos, xDir, 0, player.getNextActorNetId(), true, player, weapon: this));
		} else {
			new DangerWrapBubbleProj(rock, shootPos, xDir, 0, player.getNextActorNetId(), input, true);
		}
		rock.playSound("buster2", sendRpc: true);
	}
}

public class DangerWrapBubbleProj : Projectile, IDamagable {
	public int type;
	int input;
	public float health = 1;
	public float heightMultiplier = 1f;
	Anim? bomb;
	Actor ownChr = null!;

	public DangerWrapBubbleProj(
		Actor owner, Point pos, int xDir, int type,
		ushort? netProjId, int input = 0, bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "danger_wrap_start", netProjId, altPlayer
	) {
		projId = (int)RockProjIds.DangerWrap;
		maxTime = 1.5f;
		fadeOnAutoDestroy = true;
		useGravity = false;
		canBeLocal = false;
		this.type = type;
		this.input = input;
		damager.hitCooldown = 30;

		if (type == 1) {
			vel.x = 60 * xDir;
			changeSprite("danger_wrap_bubble", false);
			fadeSprite = "generic_explosion";

			if (input == -1) {
				vel.x /= 7.5f;
				heightMultiplier = 1.6f;
			} else if (input == 2) {
				vel.x *= 3f;
				heightMultiplier = 0.65f;
			}

			if (ownedByLocalPlayer && ownerPlayer != null) {
				bomb = new Anim(pos, "danger_wrap_bomb", xDir, ownerPlayer.getNextActorNetId(), true, true);
			}
		}

		if (rpc && ownerPlayer != null) {
			rpcCreate(pos, owner, ownerPlayer, netProjId, xDir, (byte)(type + 128));
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new DangerWrapBubbleProj(
			arg.owner, arg.pos, arg.xDir, arg.extraData[0] - 128,
			arg.netId, altPlayer: arg.player
		);
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;

		if (type == 0 && isAnimOver()) {

			time = 0;
			if (ownedByLocalPlayer) {
				new DangerWrapBubbleProj(
					ownChr, pos, xDir, 1, damager.owner.getNextActorNetId(true),
					input, rpc: true, damager.owner
				);

			}
			destroySelfNoEffect();
		}

		if (type == 1) {
			vel.y -= Global.spf * (100 * heightMultiplier);
			if (Math.Abs(vel.x) > 25) {
				vel.x -= Global.spf * (75 * xDir);
			}
			bomb?.changePos(pos);
		}
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		destroySelf();
	}

	public override void onDestroy() {
		base.onDestroy();
		if (!ownedByLocalPlayer) return;

		if (type == 1) bomb?.destroySelf();
	}

	public void applyDamage(float damage, Player owner, Actor? actor, int? weaponIndex, int? projId) {
		if (damage > 0) {
			destroySelf();
		}
	}

	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) {
		return damager.owner.alliance != damagerAlliance;
	}

	public bool isInvincible(Player attacker, int? projId) {
		return false;
	}

	public bool canBeHealed(int healerAlliance) {
		return false;
	}

	public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) {
	}

	public bool isPlayableDamagable() {
		return false;
	}
}

public class DangerWrapMineProj : Projectile, IDamagable {
	public bool landed;
	public float health = 1;
	public Actor ownChr;
	public Weapon? wep;
	Player player;

	public DangerWrapMineProj(
		Actor owner, Point pos, int xDir, int type,
		ushort? netProjId, bool rpc = false, Player? altPlayer = null,
		Weapon? weapon = null
	) : base(
		pos, xDir, owner, "danger_wrap_fall", netProjId, altPlayer
	) {
		projId = (int)RockProjIds.DangerWrapMine;
		maxTime = 1.125f;
		useGravity = true;
		damager.damage = 3;
		damager.hitCooldown = 30;
		ownChr = owner;
		canBeGrounded = true;
		canBeLocal = false;
		this.player = ownerPlayer;

		if (ownedByLocalPlayer && weapon != null) {
			wep = weapon;
		}
		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type };

			rpcCreate(pos, owner, ownerPlayer, netProjId, xDir, extraArgs);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new DangerWrapMineProj(
			arg.owner, arg.pos, arg.xDir, arg.extraData[0],
			arg.netId, altPlayer: arg.player
		);
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (!ownedByLocalPlayer) {
			return;
		}
		if (!landed && (
			other.gameObject is Wall ||
			other.gameObject is MovingPlatform ||
			other.gameObject is Actor actor && (actor.isPlatform || actor.isSolidWall)
		)) {
			landed = true;
			destroySelf();

			var mine = new DangerWrapLandProj(
				ownChr, pos, xDir, player.getNextActorNetId(), true, player
			);
			(wep as DangerWrap)?.dangerMines.Add(mine);
		}
	}

	public void applyDamage(float damage, Player owner, Actor? actor, int? weaponIndex, int? projId) {
		health -= damage;
		if (ownedByLocalPlayer && health <= 0) {
			destroySelf();
		}
	}

	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) {
		return damager.owner.alliance != damagerAlliance;
	}

	public bool isInvincible(Player attacker, int? projId) {
		return false;
	}

	public bool canBeHealed(int healerAlliance) {
		return false;
	}

	public void heal(
		Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false
	) {
	}

	public bool isPlayableDamagable() {
		return false;
	}
}

public class DangerWrapLandProj : Projectile, IDamagable {
	public float health = 1;
	public Actor ownChr;
	Point collideDir;

	public DangerWrapLandProj(
		Actor owner, Point pos, int xDir, ushort? netProjId,
		bool rpc = false, Player? altPlayer = null
	) : base (
		pos, xDir, owner, "danger_wrap_land", netProjId, altPlayer
	) {
		useGravity = false;
		projId = (int)RockProjIds.DangerWrapMineLanded;
		maxTime = 20;
		fadeSprite = "generic_explosion";
		fadeOnAutoDestroy = true;
		destroyOnHit = false;

		if (collider != null) {
			collider.isTrigger = false;
			collider.wallOnly = true;
		}

		damager.damage = 3;
		damager.flinch = Global.defFlinch;
		damager.hitCooldown = 30;
		ownChr = owner;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netProjId, xDir);
		}
		projId = (int)RockProjIds.DangerWrapMine;
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new DangerWrapLandProj(
			arg.owner, arg.pos, arg.xDir,
			arg.netId, altPlayer: arg.player
		);
	}

	public override void update() {
		base.update();
		moveWithMovingPlatform(collideDir);

		if (time >= 18) {
			changeSprite("danger_wrap_land_active", true);
		}

		if (ownedByLocalPlayer) {
			useGravity = !(Global.level.checkTerrainCollisionOnce(this, 0, 1, checkPlatforms: true) != null);
		}
	}

	public override void onDamageEX(IDamagable damagable) {
		base.onDamageEX(damagable);
		destroySelf();
	}

	public override void onDestroy() {
		base.onDestroy();
		if (!ownedByLocalPlayer) {
			return;
		}
		if (health >= 1) {
			for (int i = 0; i < 6; i++) {
				float x = Helpers.cosd(i * 60) * 180;
				float y = Helpers.sind(i * 60) * 180;
				new Anim(pos, "generic_explosion", 1, damager.owner.getNextActorNetId(), false, true) {
					vel = new Point(x, y),
					ttl = 0.2f
				};
			}

			playSound("danger_wrap_explosion", sendRpc: true);
			new DangerWrapExplosionProj(ownChr, pos, xDir, damager.owner.getNextActorNetId(), true);
		}
	}

	public void applyDamage(float damage, Player owner, Actor? actor, int? weaponIndex, int? projId) {
		health -= damage;
		if (ownedByLocalPlayer && health <= 0) {
			destroySelf();
		}
	}

	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) {
		return damager.owner.alliance != damagerAlliance;
	}

	public bool isInvincible(Player attacker, int? projId) {
		return false;
	}

	public bool canBeHealed(int healerAlliance) {
		return false;
	}

	public void heal(
		Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false
	) {
	}

	public bool isPlayableDamagable() {
		return false;
	}
}

public class DangerWrapExplosionProj : Projectile {
	private int radius = 0;
	private double maxRadius = 60;

	public DangerWrapExplosionProj(
		Actor owner, Point pos, int xDir, ushort? netProjId,
		bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "empty", netProjId, altPlayer
	) {
		projId = (int)RockProjIds.DangerWrapExplosion;
		maxTime = 0.8f; // In case that for some reason it does not despawn.
		destroyOnHit = false;
		shouldShieldBlock = false;
		damager.damage = 2;
		damager.flinch = Global.halfFlinch;
		damager.hitCooldown = 30;

		if (rpc) rpcCreate(pos, owner, ownerPlayer, netProjId, xDir);
		projId = (int)RockProjIds.DangerWrapMine;
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new DangerWrapExplosionProj(
			arg.owner, arg.pos, arg.xDir, arg.netId, altPlayer: arg.player
		);
	}

	public override void update() {
		base.update();
		if (radius < maxRadius) radius += 4;
		else destroySelf();

		if (isRunByLocalPlayer()) {
			foreach (var go in Global.level.getGameObjectArray()) {
				var chr = go as Character;
				if (chr != null && chr.canBeDamaged(damager.owner.alliance, damager.owner.id, projId)
					&& chr.pos.distanceTo(pos) <= radius) {

					damager.applyDamage(chr, false, weapon, this, projId);
				}
			}
		}
	}

	public override void render(float x, float y) {
		base.render(x, y);
		float transparencyMultiplier = 1;
		if (radius + 16 >= maxRadius) {
			transparencyMultiplier = ((float)maxRadius - radius) / 16;
		}
		Color col1 = new(222, 41, 24, (byte)MathF.Ceiling(96 * transparencyMultiplier));
		Color col2 = new(255, 220, 220, (byte)MathF.Ceiling(128 * transparencyMultiplier));
		DrawWrappers.DrawCircle(
			pos.x + x, pos.y + y, radius, filled: true, col1, 4f,
			ZIndex.Background - 100, isWorldPos: true, col2
		);
	}
}

public class DWrapBigBubble : Actor, IDamagable {
	public Character? character;
	public float health = 3;
	public float bubbleFrames;
	public Anim? bomb;
	public Player? attacker;

	public DWrapBigBubble(
		Point pos, Player victim,
		int xDir, ushort? netId,
		bool ownedByLocalPlayer, bool rpc = false)
	: base
	(
		"danger_wrap_big_bubble", pos, netId, ownedByLocalPlayer, false
	) {
		netOwner = victim;
		character = victim.character;
		character?.stopMoving();
		useGravity = false;
		canBeLocal = false;

		if (ownedByLocalPlayer && netOwner != null) {
			bomb = new Anim(
				getCenterPos(), "danger_wrap_bomb",
				xDir, netOwner.getNextActorNetId(), false, true
			);
		}

		netActorCreateId = NetActorCreateId.DWrapBigBubble;
		if (rpc) {
			createActorRpc(victim.id);
		}
	}

	public void applyDamage(float damage, Player owner, Actor? actor, int? weaponIndex, int? projId) {
		health -= damage;
		if (health <= 0) {
			destroySelf();
		}
	}

	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) {
		return netOwner?.alliance == damagerAlliance;
	}

	public bool isInvincible(Player attacker, int? projId) {
		return false;
	}

	public bool canBeHealed(int healerAlliance) {
		return false;
	}

	public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) {
	}

	public override void preUpdate() {
		base.preUpdate();
		updateProjectileCooldown();
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;

		bubbleFrames++;
		if (character != null) {
			changePos(character.getCenterPos());
			if (character.isDWrapped) {
				character.grounded = true;
				if (bubbleFrames is <= 60 or >= 150) {
					if (character.vel.y > -60) character.vel.y -= 5;
					if (Math.Abs(character.vel.x) < 30 && bubbleFrames <= 60) character.vel.x += 3 * character.xDir;
				} else {
					if (character.vel.y < 30) character.vel.y += 2;
					if (Math.Abs(character.vel.x) > 0) character.vel.x -= 1 * character.xDir;
				}
			}
		}
		if (bomb != null) {
			bomb.changePos(getCenterPos());
			if (bubbleFrames >= 120) bomb.changeSprite("danger_wrap_bomb_active", false);
		}
		if (bubbleFrames >= 180 && (character == null || character.dWrapDamager != null)) {
			if (character?.ownedByLocalPlayer == true) {
				/* character.dWrapDamager?.applyDamage(
					character, false, new DangerWrap(), this, (int)RockProjIds.DangerWrapBubbleExplosion
				); */

				//Temporal fix.
				character?.applyDamage(
					4, attacker, character, (int)RockWeaponIds.DangerWrap, (int)RockProjIds.DangerWrapBubbleExplosion
				);
				character?.playSound("hurt", sendRpc: true);
				character?.setHurt(-character?.xDir ?? xDir, Global.defFlinch, false);
			}
			destroySelf();
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		if (netOwner?.ownedByLocalPlayer != true) return;
		if (character != null) {
			character.bigBubble = null;
			//character.removeBubble(true);
			character.dwrapEnd();
			character.dwrapInvulnTime = 3;
		}
		if (bomb != null) bomb.destroySelf();
		new Anim(pos, "danger_wrap_big_bubble_fade", xDir, netOwner.getNextActorNetId(), true);
	}

	public bool isPlayableDamagable() {
		return false;
	}
}
