using System;
using System.Collections.Generic;

namespace MMXOnline;

public class WildCoil : Weapon {
	public static WildCoil netWeapon = new();

	public WildCoil() : base() {
		index = (int)RockWeaponIds.WildCoil;
		weaponSlotIndex = (int)RockWeaponSlotIds.WildCoil;
		weaponBarBaseIndex = (int)RockWeaponBarIds.WildCoil;
		weaponBarIndex = weaponBarBaseIndex;
		killFeedIndex = 0;
		maxAmmo = 20;
		ammo = maxAmmo;
		fireRate = 60;
		switchCooldown = 45;
		description = new string[] { "Throws coils in both sides that", "can be charged to reach more height.", "Use Up/Down to change bounce patterns." };
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

	public int bounceSpeed = 240;
	float soundCooldown;
	float projSpeed = 120;
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
		fadeSprite = "generic_explosion";
		canBeLocal = false;

		damager.damage = 2;
		damager.hitCooldown = 6;

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
				if (vel.y != -bounceSpeed) vel.y = -bounceSpeed;
			} else {
				if (vel.y != -bounceSpeed) vel.y = bounceSpeed;
			}

			incPos(new Point(0, 5 * MathF.Sign(vel.y)));
		}
	}
}


public class WildCoilChargedProj : Projectile {

	public int bounceSpeed = 330;
	public float bouncePower = 1;
	float soundCooldown;
	int bounceReq = 1;
	int bounceCounter;
	int bounceBuff;
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
		fadeSprite = "generic_explosion";
		canBeLocal = false;
		damager.damage = 2;

		player = altPlayer;
		if (player != null) {
			if (player.input.isHeld(Control.Up, player)) bounceSpeed = 480;
			else if (player.input.isHeld(Control.Down, player)) {
				bounceReq = 6;
				bounceSpeed = 60;
			} else bouncePower = 1f;
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

		bounceBuff = (int)bounceCounter / bounceReq;

		if (bounceBuff < 3) {
			switch (bounceBuff) {
				case 1:
					updateDamager(3, 4);
					break;

				case 2:
					updateDamager(3, Global.halfFlinch);
					break;
			}
		}
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
				if (vel.y != -bounceSpeed * bouncePower) vel.y = -bounceSpeed * bouncePower;
			} else {
				if (vel.y != -bounceSpeed * bouncePower) vel.y = bounceSpeed * bouncePower;
			}

			incPos(new Point(0, 5 * MathF.Sign(vel.y)));

			if (frameIndex > 0) {
				frameIndex = 0;
			}
		} else destroySelf(); 	
	}

	public override void render(float x, float y) {
		base.render(x, y);

		Global.sprites[getOutline()].draw(
			frame, pos.x, pos.y, xDir, yDir, getRenderEffectSet(), 1, 1, 1, zIndex
		);
	}

	string getOutline() {
		return bounceBuff switch  {
			0 => "wild_coil_outline1",
			1 => "wild_coil_outline2",
			_ => "wild_coil_outline3"
		};
	}

	public override List<byte> getCustomActorNetData() {
		return [
			(byte)bounceBuff,
			(byte)frame
		];
	}

	public override void updateCustomActorNetData(byte[] data) {
		bounceBuff = data[0];
		frame = data[1];
	}
}
