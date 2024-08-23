using System;
using System.Collections.Generic;

namespace MMXOnline;

public class TenguBlade : Weapon {

	public static TenguBlade netWeapon = new();
	public TenguBlade() : base() {
		index = (int)BassWeaponIds.TenguBlade;
		displayName = "TENGU BLADE";
		weaponSlotIndex = index;
		weaponBarBaseIndex = index;
		weaponBarIndex = index;
		fireRateFrames = 60;
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Point shootPos = character.getShootPos();
		Player player = character.player;

		if (character.charState is LadderClimb) character.changeState(new TenguBladeLadder(), true);
		else character.changeState(new TenguBladeState(), true);
	}
}


public class TenguBladeStart : Anim {
	Player player;
	Point distance;

	public TenguBladeStart(
		Point pos, int xDir, ushort? netId, Player player,
		bool sendRpc = false, bool ownedByLocalPlayer = true
	) : base(
		pos, "tengu_blade_spawn", xDir, netId, true, 
		sendRpc, ownedByLocalPlayer, player.character
	) {
		this.player = player;
		distance = pos.directionTo(player.character.getCenterPos());
	}

	public override void update() {
		base.update();

		changePos(player.character.getCenterPos().subtract(distance));
	}

	public override void onDestroy() {
		base.onDestroy();

		new TenguBladeProj(pos, xDir, player, player.getNextActorNetId(), true);
	}
}


public class TenguBladeState : CharState {
	bool fired;

	public TenguBladeState() : base("tblade") {
		normalCtrl = false;
		attackCtrl = false;
		airMove = true;
		useDashJumpSpeed = true;
	}

	public override void update() {
		base.update();

		if (!fired && character.currentFrame.getBusterOffset() != null) {
			Point shootPos = character.getFirstPOI() ?? character.getShootPos();
			Player player = character.player;

			new TenguBladeStart(shootPos, character.xDir, player.getNextActorNetId(), player, true);
			fired = true;
		}

		if (character.isAnimOver()) character.changeToIdleOrFall();
	}
}


public class TenguBladeProj : Projectile {
	bool bouncedOnce;
	const float maxSpeed = 240;
	public TenguBladeProj(
		Point pos, int xDir, Player player,
		ushort? netProjId, bool rpc = false
	) : base(
		TenguBlade.netWeapon, pos, xDir, 120, 2,
		player, "tengu_blade_proj", 0, 0.75f,
		netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 2;
		projId = (int)BassProjIds.TenguBladeProj;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new TenguBladeProj(
			arg.pos, arg.xDir, arg.player, arg.netId
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
		if (other.isCeilingHit()) destroySelf();

		bouncedOnce = true;
		incPos(new Point(6 * -xDir, 0));
		xDir *= -1;
		vel.x *= -1;
		vel.y *= -1;
	}
}


public class TenguBladeMelee : GenericMeleeProj {
	public TenguBladeMelee(Point pos, Player player) : base(
		TenguBlade.netWeapon, pos, ProjIds.TenguBladeDash,
		player, 2, 0, 0.375f
	) {
		projId = (int)BassProjIds.TenguBladeProj;
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		if (damagable.canBeDamaged(damager.owner.alliance, damager.owner.id, projId)) {
			if (damagable.projectileCooldown.ContainsKey(projId + "_" + owner.id) &&
				damagable.projectileCooldown[projId + "_" + owner.id] >= damager.hitCooldown
			) {
				if (damagable is Character chr && chr != null) chr.xPushVel = xDir * 180;	
			}
		}

		
	}
}


public class TenguBladeDash : CharState {

	int startXDir;
	int inputXDir;
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
		startXDir = character.xDir;
		player.weapon.addAmmo(-1, player);
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		character.isDashing = false;
		bass.tBladeDashCooldown = 10;
	}

	public override void update() {
		base.update();

		inputXDir = player.input.getXDir(player);

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
	List<CollideData> ladders;
	float midX; 

	public TenguBladeLadder() : base("ladder_tblade") {
		normalCtrl = false;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.stopMoving();
		character.useGravity = false;
		ladders = Global.level.getTriggerList(character, 0, 1, null, typeof(Ladder));
		midX = ladders[0].otherCollider.shape.getRect().center().x;
	}

	public override void update() {
		base.update();

		if (!fired && character.currentFrame.getBusterOffset() != null) {
			Point shootPos = character.getFirstPOI() ?? character.getShootPos();
			Player player = character.player;
			new TenguBladeStart(shootPos, character.getShootXDir(), player.getNextActorNetId(), player, true);
			fired = true;
		}

		if (character.isAnimOver()) {
			character.changeState(new LadderClimb(ladders[0].gameObject as Ladder, midX), true);
		}
	}
}
