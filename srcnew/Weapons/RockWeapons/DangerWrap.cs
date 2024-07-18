using System;
using System.Collections.Generic;
using SFML.Graphics;

namespace MMXOnline;

public class DangerWrap : Weapon {

	public List<DangerWrapMineProj> dangerMines = new List<DangerWrapMineProj>();
	public static DangerWrap netWeapon = new DangerWrap();

	public DangerWrap() : base() {
		index = (int)RockWeaponIds.DangerWrap;
		weaponBarBaseIndex = (int)RockWeaponBarIds.DangerWrap;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = (int)RockWeaponSlotIds.DangerWrap;
		//shootSounds = new List<string>() {"buster2", "buster2", "buster2", ""};
		killFeedIndex = 0;
		rateOfFire = 1.25f;
		maxAmmo = 10;
		ammo = maxAmmo;
		description = new string[] { "Complex weapon able to catch foes.", "Press UP/LEFT/RIGHT to change direction", "or press DOWN to leave a mine." };
	}

	public override bool canShoot(int chargeLevel, Player player) {
		if (!base.canShoot(chargeLevel, player)) {
			return false;
		}

		for (int i = dangerMines.Count - 1; i >= 0; i--) {
			if (dangerMines[i].destroyed) {
				dangerMines.RemoveAt(i);
				continue;
			}
		}

		return dangerMines.Count < 3;

	}


	public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId) {
		base.getProjectile(pos, xDir, player, chargeLevel, netProjId);
		if (player.character.ownedByLocalPlayer) {
			/*
            if (chargeLevel >= 2 && player.hasBusterLoadout()) {
                player.character.changeState(new RockChargeShotState(player.character.grounded), true);
            } else*/
			if (player.input.isHeld(Control.Down, player)) {

				dangerMines.Add(new DangerWrapMineProj(this, pos, xDir, player, 0, netProjId, true));
			} else {
				new DangerWrapBubbleProj(this, pos, xDir, player, 0, netProjId);
			}
			player.character.playSound("buster2", sendRpc: true);
		}
	}
}


public class DangerWrapBubbleProj : Projectile, IDamagable {

	public int type;
	public float health = 1;
	public float heightMultiplier = 1f;
	private bool spawnedBomb = false;
	Character? character;
	Anim? bomb;

	public DangerWrapBubbleProj(
		Weapon weapon, Point pos, int xDir,
		Player player, int type, ushort netProjId,
		bool rpc = false
	) : base(
		weapon, pos, xDir, 0, 0,
		player, "danger_wrap_start", 0, 0.5f, netProjId,
		player.ownedByLocalPlayer
	) {

		projId = (int)RockProjIds.DangerWrap;
		maxTime = 1.5f;
		fadeOnAutoDestroy = true;
		//destroyOnHitWall = true;
		useGravity = false;
		canBeLocal = false;
		this.type = type;

		if (type == 1) {
			vel.x = 60 * xDir;
			changeSprite("danger_wrap_bubble", false);
			fadeSprite = "generic_explosion";

			if (player.input.isHeld(Control.Up, player)) {
				vel.x /= 7.5f;
				heightMultiplier = 1.6f;
			} else if (player.input.isHeld(Control.Left, player) || player.input.isHeld(Control.Right, player)) {
				vel.x *= 3f;
				heightMultiplier = 0.65f;
			}

			if (spawnedBomb == false) {
				Point bombPos = pos;

				bomb = new Anim(bombPos, "danger_wrap_bomb", xDir, null, false);
				spawnedBomb = true;
			}
		}

		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type };

			rpcCreate(pos, player, netProjId, xDir, extraArgs);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new DangerWrapBubbleProj(
			DangerWrap.netWeapon, arg.pos, arg.xDir, arg.player,
			arg.extraData[0], arg.netId
		);
	}

	public override void update() {
		base.update();

		if (type == 0 && isAnimOver()) {

			time = 0;
			new DangerWrapBubbleProj(
				weapon, pos, xDir, damager.owner, 1,
				damager.owner.getNextActorNetId(true), rpc: true
			);
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
		if (type == 1) bomb?.destroySelf();
	}

	public void applyDamage(float damage, Player owner, Actor actor, int? weaponIndex, int? projId) {
		health -= damage;
		if (health <= 0) {
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

}


public class DangerWrapMineProj : Projectile, IDamagable {

	bool landed;
	bool didExplode;
	float health = 1;

	public DangerWrapMineProj(
		Weapon weapon, Point pos, int xDir,
		Player player, int type, ushort netProjId,
		bool rpc = false
	) : base(
		weapon, pos, xDir, 0, 2,
		player, "danger_wrap_fall", 0, 1,
		netProjId, player.ownedByLocalPlayer
	) {

		projId = (int)RockProjIds.DangerWrapMine;
		maxTime = 0.5f;
		useGravity = true;
		fadeSprite = "generic_explosion";

		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type };

			rpcCreate(pos, player, netProjId, xDir, extraArgs);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new DangerWrapMineProj(
			DangerWrap.netWeapon, arg.pos, arg.xDir, arg.player,
			arg.extraData[0], arg.netId
		);
	}


	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (other.gameObject is Wall) {

			vel = new Point();
			maxTime = 15;
			useGravity = false;
			changeSprite("danger_wrap_land", false);
			landed = true;
			damager.damage = 3;
			damager.flinch = Global.halfFlinch;

			if (time >= 2) changeSprite("danger_wrap_land_active", false);

			if (time >= 4) {
				didExplode = true;
				destroySelf();
			}
		}
	}

	public override void onDestroy() {
		base.onDestroy();

		if (landed && didExplode) {
			for (int i = 0; i < 6; i++) {
				float x = Helpers.cosd(i * 60) * 180;
				float y = Helpers.sind(i * 60) * 180;
				new Anim(pos, "generic_explosion", 1, null, true) { vel = new Point(x, y) };
			}

			playSound("danger_wrap_explosion");
			new DangerWrapExplosionProj(weapon, pos, xDir, damager.owner, damager.owner.getNextActorNetId(true), true);
		}
	}
	public void applyDamage(float damage, Player owner, Actor actor, int? weaponIndex, int? projId) {
		health -= damage;
		if (health <= 0) {
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
}


public class DangerWrapExplosionProj : Projectile {

	private int radius = 0;
	private double maxRadius = 80;

	public DangerWrapExplosionProj(
		Weapon weapon, Point pos, int xDir,
		Player player, ushort netProjId,
		bool rpc = false
	) : base(
		weapon, pos, xDir, 0, 4,
		player, "empty", Global.defFlinch, 0.5f,
		netProjId, player.ownedByLocalPlayer
	) {

		projId = (int)RockProjIds.DangerWrapExplosion;
		//maxTime = 0.2f;
		destroyOnHit = false;
		shouldShieldBlock = false;

		if (rpc) rpcCreate(pos, player, netProjId, xDir);
		projId = (int)RockProjIds.DangerWrapMine;
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new DangerWrapExplosionProj(
		DangerWrap.netWeapon, arg.pos, arg.xDir, arg.player,
			arg.netId
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
		double transparency = (time) / (0.4);
		if (transparency < 0) { transparency = 0; }
		Color col1 = new(222, 41, 24, 128);
		Color col2 = new(255, 255, 255, 255);
		DrawWrappers.DrawCircle(pos.x + x, pos.y + y, radius, filled: true, col1, 4f, zIndex - 10, isWorldPos: true, col2);
	}
}

public class DWrapped : CharState {
	bool isDone;
	bool flinch;
	public const float DWrapMaxTime = 3;
	public DWrapped(bool flinch) : base("idle", "shoot") {
		this.flinch = flinch;
		attackCtrl = true;
		normalCtrl = false;
	}
	public override bool canEnter(Character character) {
		if (!base.canEnter(character)) return false;
		if (character.dwrapInvulnTime > 0) return false;
		if (!character.ownedByLocalPlayer) return false;
		if (character.isInvulnerable()) return false;
		if (character.isVaccinated()) return false;
		return !character.isCCImmune() && !character.charState.invincible;
	}

	/*public override bool canExit(Character character, CharState newState) {
		if (newState is Hurt || newState is Die) return true;
		return isDone;
	}*/

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.dwrapStart();
		character.stopMoving();
		character.grounded = false;
		character.useGravity = false;
		Global.serverClient?.rpc(RPC.playerToggle, (byte)character.player.id, (byte)RPCToggleType.StartDWrap);
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		character.dwrapEnd();
		character.dwrapInvulnTime = 3;
		character.useGravity = true;
		character.frameSpeed = 1;
		character.vel.x = 0;
		Global.serverClient?.rpc(RPC.playerToggle, (byte)character.player.id, (byte)RPCToggleType.StopDwrap);
	}

	public override void update() {
		base.update();

		if (character.isDWrapped) {
			if (stateTime < 0.75) {
				if (character.vel.y > -60) character.vel.y -= 5;
				if (Math.Abs(character.vel.x) < 30) character.vel.x += 3 * character.xDir;
			} else {
				if (character.vel.y < 30) character.vel.y += 2;
				if (Math.Abs(character.vel.x) > 0) character.vel.x -= 1 * character.xDir;
			}
		}

		if (!character.hasBubble || character.dWrapDamager == null) {
			isDone = true;
			//character.changeState(new Fall(), true);
			//return;
		}

		/* if (character.dWrappedTime > 2 && !(character.charState is DWrapped)) {
			character.removeBubble(false);
		} */
	}
}


public class DWrapBigBubble : Actor, IDamagable {

	public Character character;
	public Player player => character.player;
	public float health = 6;
	public float bubbleFrames;
	public Anim? bomb;

	public DWrapBigBubble(
		Point pos, Player victim, Player attacker,
		int xDir, ushort? netId, 
		bool ownedByLocalPlayer, bool rpc = false)
	: base
	(
		"danger_wrap_big_bubble", pos, netId, ownedByLocalPlayer, false
	) {
		netOwner = victim;
		this.character = victim.character;
		useGravity = false;

		bomb = new Anim(getCenterPos(), "danger_wrap_bomb", 
			xDir, player.getNextActorNetId(), false, true);
	}

	public void applyDamage(float damage, Player owner, Actor actor, int? weaponIndex, int? projId) {
		health -= damage;
		if (health <= 0) {
			destroySelf();
		}
	}

	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) {
		return player.alliance == damagerAlliance;
	}

	public bool isInvincible(Player attacker, int? projId) {
		return false;
	}

	public bool canBeHealed(int healerAlliance) {
		return false;
	}

	public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) {
	}

	public override void update() {
		base.update();
		bubbleFrames++;

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

		if (bomb != null) {
			bomb.changePos(getCenterPos());
			if(bubbleFrames >= 120) bomb.changeSprite("danger_wrap_bomb_active", true);
		}

		if (bubbleFrames >= 180 && character.dWrapDamager != null) {
			character.dWrapDamager.applyDamage(character, false, new DangerWrap(), this, (int)RockProjIds.DangerWrapBubbleExplosion);
			destroySelf();
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		character.bigBubble = null!;
		if (bomb != null) bomb.destroySelf();
		//character.removeBubble(true);
		character.dwrapEnd();
		character.dwrapInvulnTime = 3;
	}
}
