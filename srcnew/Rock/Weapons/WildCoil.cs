using System;
using System.Collections.Generic;

namespace MMXOnline;

public class WildCoil : Weapon {
	public static WildCoil netWeapon = new();

	public WildCoil() : base() {
		displayName = "WILD COIL";
		index = (int)RockWeaponIds.WildCoil;
		weaponSlotIndex = (int)RockWeaponSlotIds.WildCoil;
		weaponBarBaseIndex = (int)RockWeaponBarIds.WildCoil;
		weaponBarIndex = weaponBarBaseIndex;
		killFeedIndex = 0;
		maxAmmo = 20;
		ammo = maxAmmo;
		fireRate = 60;
		switchCooldown = 45;
		descriptionV2 = [
			[ "Throws coils in both sides that\n" + 
			"can be charged to reach more height.\n" + 
			"Use Up/Down to change bounce patterns." ]
		];
	}

	public override void shootRock(Rock rock, params int[] args) {
		base.shootRock(rock, args);
		int chargeLevel = args[0];

		if (rock.charState is LadderClimb lc) {
			rock.changeState(new ShootAltLadder(lc.ladder, this, chargeLevel), true);
		} else {
			rock.changeState(new ShootAltRock(this, chargeLevel), true);
		}
	}

	public override void getProjs(Rock rock, params int[] args) {
		Point shootPos = rock.getFirstPOI() ?? rock.getCenterPos();
		Point shootPos1 = rock.getFirstPOI(1) ?? rock.getCenterPos();
		int xDir = rock.getShootXDir();
		Player player = rock.player;
		int chargeLv = args[0];

		if (chargeLv >= 2) {
			new WildCoilChargedProj(rock, shootPos, xDir, 0, player.getNextActorNetId(), true, player);
			new WildCoilChargedProj(rock, shootPos1, xDir, 1, player.getNextActorNetId(), true, player);
			rock.playSound("buster3", sendRpc: true);
		} else {
			new WildCoilProj(rock, shootPos, xDir, 0, player.getNextActorNetId(), true, player);
			new WildCoilProj(rock, shootPos1, xDir, 1, player.getNextActorNetId(), true, player);
			rock.playSound("buster2", sendRpc: true);
		}
	}

	public override float getAmmoUsage(int chargeLevel) {
		if (chargeLevel >= 2) return 2;
		return 1;
	}
}

public class WildCoilProj : Projectile {

	public int bouncePower = 240;
	float bounceMod = 1;
	float soundCooldown;
	float projSpeed = 120;
	int hits;
	bool bouncedOnce;
	public WildCoilProj(
		Actor owner, Point pos, int xDir, int type, 
		ushort? netProjId, bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "wild_coil_start", netProjId, altPlayer
	) {

		projId = (int)RockProjIds.WildCoil;
		useGravity = true;
		maxTime = 1.5f;
		fadeOnAutoDestroy = true;
		destroyOnHit = false;
		fadeSprite = "generic_explosion";
		canBeLocal = false;

		damager.damage = 2;
		damager.hitCooldown = 30;

		vel.y = -200;
		if (type == 0) vel.x = projSpeed * xDir;
		else vel.x = -projSpeed * xDir;

		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type };

			rpcCreate(pos, owner, ownerPlayer, netProjId, xDir, extraArgs);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new WildCoilProj(
			arg.owner, arg.pos, arg.xDir, arg.extraData[0], 
			arg.netId, altPlayer: arg.player
		);
	}

	public override void update() {
		base.update();

		if (!ownedByLocalPlayer) return;
		if (soundCooldown > 0) Helpers.decrementTime(ref soundCooldown);
	}


	public override void onHitWall(CollideData other) {
		var normal = other.hitData.normal ?? new Point(0, -1);

		if (normal.isSideways()) {
			destroySelf();
		} else {
			changeSprite("wild_coil_jump", true);
			if (frameIndex > 0) frameIndex = 0;
			if (soundCooldown <= 0) {
				playSound("wild_coil_bounce", true, true);
				soundCooldown = 6f / 60f;
			}
			vel.y *= -1;
			if (vel.y < 0) {
				if (vel.y != -bouncePower) vel.y = -bouncePower;
			} else {
				if (vel.y != -bouncePower) vel.y = bouncePower;
			}

			incPos(new Point(0, 5 * MathF.Sign(vel.y)));
			bouncedOnce = true;
		}
	}

	public override void onDamageEX(IDamagable damagable) {
		base.onDamageEX(damagable);
		Actor? actor = damagable as Actor;
		changeSprite("wild_coil_jump", true);
		hits++;
		bounceMod = Math.Max(bounceMod - 0.2f, 0);

		float mod = 1f;
		if (actor != null) {
			if (actor.getTopY() - pos.y is <= 8 and >= 0) {
				vel.y = -bouncePower * bounceMod * mod;
			} 	
			else if (actor.pos.y - getTopY() is <= 8 and >= 0) {
				vel.y = bouncePower * bounceMod * mod;
			} 
			else {
				if (vel.y < 0 && bouncedOnce) vel.y = bouncePower * bounceMod * mod;
				else vel.y = -bouncePower * bounceMod * mod;
			}	
		}
		else {
			if (vel.y < 0 && bouncedOnce) vel.y = bouncePower * bounceMod * mod;
			else vel.y = -bouncePower * bounceMod * mod;
		}

		bouncedOnce = true;
		if (hits >= 2) destroySelf();
	}
}


public class WildCoilChargedProj : Projectile {

	public int bouncePower = 330;
	public float bounceMod = 1;
	int hits;
	float soundCooldown;
	int bounceCounter;
	bool bouncedOnce;
	int frame;
	float projSpeed = 120;
	Player? player = null;

	public WildCoilChargedProj(
		Actor owner, Point pos, int xDir, int type, 
		ushort? netProjId, bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner,"wild_coil_charge_start", netProjId, altPlayer
	) {

		projId = (int)RockProjIds.WildCoilCharged;
		maxTime = 2f;
		useGravity = true;
		fadeOnAutoDestroy = true;
		destroyOnHit = false;
		fadeSprite = "generic_explosion";
		canBeLocal = false;
		damager.damage = 3;
		damager.flinch = Global.halfFlinch;
		damager.hitCooldown = 30;

		player = altPlayer;
		if (player != null) {
			if (player.input.isHeld(Control.Up, player)) bouncePower = 480;
			else if (player.input.isHeld(Control.Down, player)) bouncePower = 60;
		}	

		vel.y = -200;
		if (type == 0) vel.x = projSpeed * xDir;
		else vel.x = -projSpeed * xDir;

		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type };

			rpcCreate(pos, owner, ownerPlayer, netProjId, xDir, extraArgs);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new WildCoilChargedProj(
			arg.owner, arg.pos, arg.xDir, arg.extraData[0], 
			arg.netId, altPlayer: arg.player
		);
	}


	public override void update() {
		base.update();

		if (!ownedByLocalPlayer) return;
		if (soundCooldown > 0) Helpers.decrementTime(ref soundCooldown);

		frame = bouncedOnce ? frameIndex : 3;
	}


	public override void onHitWall(CollideData other) {
		//if (!ownedByLocalPlayer) return;
		var normal = other.hitData.normal ?? new Point(0, -1);

		if (normal.isCeilingNormal() || normal.isGroundNormal()) {
			changeSprite("wild_coil_charge_jump", true);
			bouncedOnce = true;

			if (soundCooldown <= 0) {
				playSound("wild_coil_bounce", true, true);
				soundCooldown = 6f / 60f;
			}
			vel.y *= -1;
			bounceCounter++;

			if (vel.y < 0) {
				if (vel.y != -bouncePower * bounceMod) vel.y = -bouncePower * bounceMod;
			} else {
				if (vel.y != -bouncePower * bounceMod) vel.y = bouncePower * bounceMod;
			}

			incPos(new Point(0, 5 * MathF.Sign(vel.y)));

			if (frameIndex > 0) {
				frameIndex = 0;
			}
		} else destroySelf(); 	
	}

	public override void onDamageEX(IDamagable damagable) {
		base.onDamageEX(damagable);
		Actor? actor = damagable as Actor;
		changeSprite("wild_coil_charge_jump", true);
		bounceCounter++;
		hits++;
		bounceMod = Math.Max(bounceMod - 0.25f, 0);

		float mod = 1f;
		if (actor != null) {
			if (actor.getTopY() - pos.y is <= 8 and >= 0) {
				vel.y = -bouncePower * bounceMod * mod;
			} 	
			else if (actor.pos.y - getTopY() is <= 8 and >= 0) {
				vel.y = bouncePower * bounceMod * mod;
			} 
			else {
				if (vel.y < 0 && bouncedOnce) vel.y = bouncePower * bounceMod * mod;
				else vel.y = -bouncePower * bounceMod * mod;
			}	
		}
		else {
			if (vel.y < 0 && bouncedOnce) vel.y = bouncePower * bounceMod * mod;
			else vel.y = -bouncePower * bounceMod * mod;
		}

		bouncedOnce = true;
		if (hits >= 3) destroySelf();
	}

	public override void render(float x, float y) {
		base.render(x, y);

		Global.sprites[getOutline()].draw(
			frame, pos.x, pos.y, xDir, yDir, getRenderEffectSet(), 1, 1, 1, zIndex
		);
	}

	string getOutline() {
		return "wild_coil_outline2";
	}

	public override List<byte> getCustomActorNetData() {
		return [
			(byte)frame
		];
	}

	public override void updateCustomActorNetData(byte[] data) {
		frame = data[0];
	}
}
