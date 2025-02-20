using System;
using System.Collections.Generic;

namespace MMXOnline;

public class TenguBlade : Weapon {
	public static TenguBlade netWeapon = new();

	public TenguBlade() : base() {
		index = (int)BassWeaponIds.TenguBlade;
		displayName = "TENGU BLADE";
		maxAmmo = 24;
		ammo = maxAmmo;
		weaponSlotIndex = index;
		weaponBarBaseIndex = index;
		weaponBarIndex = index;
		fireRate = 60;
		hasCustomAnim = true;
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Point shootPos = character.getShootPos();
		Player player = character.player;

		if (character.charState is LadderClimb lc) character.changeState(new TenguBladeLadder(lc.ladder), true);
		else character.changeState(new TenguBladeState(), true);
	}
}


public class TenguBladeStart : Projectile {
	Character? character;
	Point distance;

	public TenguBladeStart(
		Actor owner, Point pos, int xDir, ushort? netId,
		bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "tengu_blade_spawn", netId, altPlayer
	) {
		projId = (int)BassProjIds.TenguBladeStart;
		maxTime = 1;
		damager.damage = 2;
		damager.hitCooldown = 20;
		setIndestructableProperties();
		canBeLocal = false;

		if (ownedByLocalPlayer) {
			character = ownerPlayer.character;
			if (character != null) {
				distance = pos.directionTo(character.getCenterPos());
			}
		}

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}

		projId = (int)BassProjIds.TenguBladeProj;
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new TenguBladeStart(
			arg.owner, arg.pos, arg.xDir, arg.netId, altPlayer: arg.player
		);
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;

		if (character != null) {
			changePos(character.getCenterPos().subtract(distance));
		}

		if (isAnimOver()) destroySelf(doRpcEvenIfNotOwned: true);
	}

	public override void onDestroy() {
		base.onDestroy();

		if (!ownedByLocalPlayer || character == null) return;
		new TenguBladeProj(character, pos, xDir, character.player.getNextActorNetId(), true);
		playSound("tengublade", true);
	}
}


public class TenguBladeState : CharState {
	bool fired;

	public TenguBladeState() : base("tblade") {
		normalCtrl = false;
		attackCtrl = false;
		airMove = true;
		useDashJumpSpeed = true;
		canStopJump = true;
	}

	public override void update() {
		base.update();

		if (!fired && character.currentFrame.getBusterOffset() != null) {
			Point shootPos = character.getFirstPOI() ?? character.getShootPos();
			Player player = character.player;

			new TenguBladeStart(character, shootPos, character.xDir, player.getNextActorNetId(), true, player);
			fired = true;
		}

		if (player.input.isPressed(Control.Jump, player) && checkJump()) {
				character.vel.y = -character.getJumpPower();
				character.playSound("jump", true);
				character.dashedInAir++;
				
				if (!character.grounded)  {
					new Anim(
						character.pos, "double_jump_anim", character.xDir, 
						player.getNextActorNetId(), true, true
					);
				}
			}

		if (character.isAnimOver()) character.changeToIdleOrFall();
	}

	bool checkJump() {
		if (character.grounded) {
			return character.canJump();
		} return character.canAirJump();
	}
}


public class TenguBladeProj : Projectile {
	bool bouncedOnce;
	const float maxSpeed = 240;
	int hits;
	public TenguBladeProj(
		Actor owner, Point pos, int xDir, ushort? netProjId,
		bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "tengu_blade_proj", netProjId, altPlayer
	) {
		fadeSprite = "tengu_blade_proj_fade";
		maxTime = 40f / 60f;
		projId = (int)BassProjIds.TenguBladeProj;

		vel.x = 120 * xDir;
		damager.damage = 2;
		damager.hitCooldown = 30f;

		canBeLocal = false;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netProjId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new TenguBladeProj(
			arg.owner, arg.pos, arg.xDir, arg.netId, altPlayer: arg.player
		);
	}

	public override void update() {
		base.update();

		if (bouncedOnce && vel.y < 240) {
			vel.y -= Global.speedMul * (60f * 0.175f);
		}
		if (Math.Abs(vel.x) < maxSpeed) {
			vel.x += xDir * Global.speedMul * (60f * 0.25f);
		}
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		if (!ownedByLocalPlayer) return;

		if (other.isCeilingHit()) destroySelf();

		bouncedOnce = true;
		incPos(new Point(6 * -xDir, 0));
		xDir *= -1;
		vel.x *= -1;
		vel.y *= -1;
		
		time = 0;
		hits++;
		if (hits >= 4) destroySelf();
	}
}


public class TenguBladeMelee : GenericMeleeProj {
	
	public TenguBladeMelee(Point pos, Player player, bool addToLevel) : base(
		TenguBlade.netWeapon, pos, ProjIds.TenguBladeDash,
		player, 2, 0, 0.375f * 60, addToLevel: addToLevel
	) {
	}

	/* public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		if (damagable.canBeDamaged(damager.owner.alliance, damager.owner.id, projId)) {
			if (damagable.projectileCooldown.ContainsKey(projId + "_" + owner.id) &&
				damagable.projectileCooldown[projId + "_" + owner.id] >= damager.hitCooldown
			) {
				if (damagable is Character chr && chr != null) chr.xPushVel = xDir * 180;	
			}
		}
	} */
}

public class TenguBladeDash : CharState {
	int startXDir;
	int inputXDir;
	Anim? dashSpark;
	Bass bass = null!;
	public TenguBladeDash() : base("tblade_dash") {
		normalCtrl = true;
		attackCtrl = false;
		enterSound = "slide";
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		bass = character as Bass ?? throw new NullReferenceException();
		character.isDashing = true;
		//character.xPushVel = character.xDir * character.getDashSpeed() * 2;
		dashSpark = new Anim(
			character.getDashSparkEffectPos(character.xDir),
			"dash_sparks", character.xDir, player.getNextActorNetId(),
			true, sendRpc: true
		);
		startXDir = character.xDir;
		bass.currentWeapon?.addAmmo(-1, player);
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		character.isDashing = false;
		bass.tBladeDashCooldown = 10;
	}

	public override void update() {
		base.update();

		//tengu blade dash dust
		inputXDir = player.input.getXDir(player);
		if (stateTime > 0.1 && !character.isUnderwater()) {
			stateTime = 0;
			new Anim(
				character.getDashDustEffectPos(character.xDir),
				"dust", character.xDir, player.getNextActorNetId(), true,
				sendRpc: true
			);
		}

		if (inputXDir != startXDir && inputXDir != 0) character.changeToIdleOrFall(); 
		else if (stateFrames >= 16) character.changeState(new TenguBladeDashEnd(), true);
		Point move = new Point();
		move.x = character.xDir * character.getDashSpeed();
		character.move(move);
	}
}


public class TenguBladeDashEnd : CharState {
	public TenguBladeDashEnd() : base("tblade_dash_end") {
		normalCtrl = true;
		attackCtrl = true;
	}

	public override void update() {
		base.update();

		if (character.isAnimOver()) character.changeToIdleOrFall();
	}
}


public class TenguBladeLadder : CharState {

	bool fired;
	public Ladder ladder; 

	public TenguBladeLadder(Ladder ladder) : base("ladder_tblade") {
		normalCtrl = false;
		this.ladder = ladder;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.stopMoving();
		character.useGravity = false;
	}

	public override void update() {
		base.update();

		if (!fired && character.currentFrame.getBusterOffset() != null) {
			Point shootPos = character.getFirstPOI() ?? character.getShootPos();
			Player player = character.player;
			new TenguBladeStart(character, shootPos, character.getShootXDir(), player.getNextActorNetId(), true, player);
			fired = true;
		}

		if (character.isAnimOver()) {
			float midX = ladder.collider.shape.getRect().center().x;
			character.changeState(new LadderClimb(ladder, midX), true);
		}
	}
}
