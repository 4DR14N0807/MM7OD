using System;

namespace MMXOnline;

public class TenguBlade : Weapon {

	public static TenguBlade netWeapon = new();
	public TenguBlade() : base() {
		index = (int)BassWeaponIds.TenguBlade;
		weaponSlotIndex = 7;
		fireRateFrames = 60;
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Point shootPos = character.getShootPos();
		Player player = character.player;

		new TenguBladeStart(shootPos, character.getShootXDir(), player.getNextActorNetId(), player, true);
	}
}


public class TenguBladeStart : Anim {

	Player player;

	public TenguBladeStart(
		Point pos, int xDir, ushort? netId, Player player,
		bool sendRpc = false, bool ownedByLocalPlayer = true
	) : base(
		pos, "tengu_blade_spawn", xDir, netId, true, 
		sendRpc, ownedByLocalPlayer, player.character
	) {
		this.player = player;
	}

	public override void onDestroy() {
		base.onDestroy();

		new TenguBladeProj(pos, xDir, player, player.getNextActorNetId(), true);
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
		xDir *= -1;
		vel.x *= -1;
		vel.y = 0;
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
		var chr = damagable as Character;

		if (chr != null) chr.xPushVel = xDir * 100;
	}
}


public class TenguBladeDash : CharState {

	int startXDir;
	int inputXDir;
	Bass bass = null!;
	public TenguBladeDash() : base("tblade_dash") {
		normalCtrl = false;
		attackCtrl = false;
		enterSound = "slide";
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		bass = character as Bass ?? throw new NullReferenceException();
		character.isDashing = true;
		character.xPushVel = character.xDir * character.getDashSpeed() * 2;
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

		if ((inputXDir != startXDir && inputXDir != 0) || stateFrames > 16) character.changeToIdleOrFall();
	}
}
