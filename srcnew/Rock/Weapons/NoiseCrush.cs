using System;
using System.Collections.Generic;

namespace MMXOnline;

public class NoiseCrush : Weapon {
	public static NoiseCrush netWeapon = new();

	public NoiseCrush() : base() {
		displayName = "NOISE CRUSH";
		index = (int)RockWeaponIds.NoiseCrush;
		weaponSlotIndex = (int)RockWeaponSlotIds.NoiseCrush;
		weaponBarBaseIndex = (int)RockWeaponBarIds.NoiseCrush;
		weaponBarIndex = weaponBarBaseIndex;
		killFeedIndex = 0;
		maxAmmo = 20;
		ammo = maxAmmo;
		fireRate = 30;
		descriptionV2 = [
			[ "Weak projectile that bounces on walls.\n" + "Catch it to get a stronger shot." ]
		];
	}

	public override void update() {
		base.update();
	}

	public override bool canShoot(int chargeLevel, Player player) {
		if (player.character is Rock rock && rock.hasChargedNoiseCrush) {
			return true;
		}
		return base.canShoot(chargeLevel, player);
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 0;
	}

	public override void shootRock(Rock rock, params int[] args) {
		base.shootRock(rock, args);
		Point shootPos = rock.getShootPos();
		int xDir = rock.getShootXDir();
		Player player = rock.player;
		bool charged = args[1] == 1;

		if (charged) {
			rock.playSound("noise_crush_charged");
			new NoiseCrushSpawnerRm(
				rock, this, shootPos, xDir, true, player.getNextActorNetId(), true
			);
			rock.hasChargedNoiseCrush = false;
			rock.noiseCrushAnimTime = 0;
		} else {
			rock.playSound("noise_crush", sendRpc: true);
			addAmmo(-1, player);
			new NoiseCrushSpawnerRm(
				rock, this, shootPos, xDir, false, player.getNextActorNetId(), true
			);
		}
	}
}

public class NoiseCrushSpawnerRm : Projectile {
	public bool charged;
	public float shootTime;
	float shootMaxTime = 1;
	int shotCount;
	int shotMaxCount = 5;
	Rock? rock;
	NoiseCrush? ncWeapon;

	public List<NoiseCrushRmProj> projectiles = [];
	NoiseCrushRmProj? tail = null;

	public NoiseCrushSpawnerRm(
		Actor owner, NoiseCrush ncWeapon, Point pos, int xDir, bool charged,
		ushort? netId, bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "empty", netId, altPlayer
	) {
		projId = (int)RockProjIds.NoiseCrushSpawner;
		maxTime = 1.25f;
		this.charged = charged;

		rock = owner as Rock;
		this.ncWeapon = ncWeapon;
	}

	public override void update() {
		base.update();

		if (shootTime <= 0 && shotCount < shotMaxCount && !destroyed) {
			shootProj();
			shotCount++;
			shootTime = shootMaxTime;
		} else if (shotCount >= shotMaxCount) {
			destroySelf();
		}
		Helpers.decrementFrames(ref shootTime);
	}

	public void shootProj() {
		if (!ownedByLocalPlayer || rock == null || ncWeapon == null || shotCount >= 5) {
			return;
		}
		int[] types = charged ? [0, 0, 1, 2, 3] : [0, 0, 1, 1, 2];

		tail = new NoiseCrushRmProj(
			rock, pos, xDir, charged, types[shotCount], tail, 
			damager.owner.getNextActorNetId(), this, true
		) {
			zIndex = zIndex - shotCount
		};
		projectiles.Add(tail);
	}

	public void bounce() {
		int i = 0;

		foreach (NoiseCrushRmProj proj in projectiles) {
			if (proj.destroyed) {
				continue;
			}
			if (!proj.bouncing && proj.bounceTimer <= 0) {
				proj.startBounce((i * 2) + 11);
			}
			i++;
		}
	}

	public void destroyProjs(Point fadePos) {
		if (!ownedByLocalPlayer) return;

		for (int i = 0; i < projectiles.Count; i++) {
			projectiles[i].destroySelf();
		}
		projectiles.Clear();
		if (!destroyed) {
			destroySelf();
		}
	}
}

public class NoiseCrushRmProj : Projectile {
	public NoiseCrushRmProj? front;
	public int bounces;
	public bool charged;
	public bool bouncing;
	public float bounceTimer;
	NoiseCrushSpawnerRm? spawn;
	Rock? rock;

	public NoiseCrushRmProj(
		Actor owner, Point pos, int xDir, bool charged, int type,  NoiseCrushRmProj? front,
		ushort? netId, NoiseCrushSpawnerRm? spawn = null,
		bool sendRpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "noise_crush_top", netId, altPlayer
	) {
		projId = (int)RockProjIds.NoiseCrush;
		maxTime = 0.75f;
		destroyOnHit = false;

		canBeLocal = false;

		vel.x = 240 * xDir;
		damager.damage = 2;
		damager.hitCooldown = 30;
		this.charged = charged;
		this.front = front;
		this.spawn = spawn;
		rock = owner as Rock;
		fadeSprite = "noise_crush_fade";
		fadeOnAutoDestroy = true;

		string sprite = "noise_crush_top";
		if (!charged) {
			sprite = type switch {
				1 => "noise_crush_middle",
				2 => "noise_crush_bottom",
				_ => "noise_crush_top"
			};
		} else {
			sprite = type switch {
				1 => "noise_crush_charged_middle",
				2 => "noise_crush_charged_middle2",
				3 => "noise_crush_charged_bottom",
				_ => "noise_crush_charged_top"
			};
		}
		changeSprite(sprite, true);

		if (sendRpc) {
			byte[] extraArgs = [(byte)(charged ? 1 : 0), (byte)type];
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, extraArgs);
		}

		if (charged) {
			projId = (int)RockProjIds.NoiseCrushCharged;
			damager.damage = 2;
			damager.flinch = Global.halfFlinch;
			destroyOnHit = true;
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new NoiseCrushRmProj(
			arg.owner, arg.pos, arg.xDir, arg.extraData[0] == 1, arg.extraData[1], null, arg.netId
		);
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) {
			return;
		}
		if (rock != null && pos.distanceTo(rock.getCenterPos()) <= 20 && bounces >= 1) {
			chargeRockNoiseCrush();
		}

		if (bounceTimer >= 0) {
			bounceTimer -= speedMul;
			if (bounceTimer <= 10 && bouncing) {
				bouncing = false;
				bounce();
			}
		}
	}

	public override void postUpdate() {
		base.postUpdate();

		if (!bouncing && front?.destroyed == false && front.xDir == xDir) {
			int dist = charged ? -8 : -6;
			float newXPos = front.pos.x + dist * xDir;

			if (bounceTimer <= 10) {
				newXPos = Helpers.lerp(pos.x, newXPos, 1 - (bounceTimer / 10));
			}
			changePos(newXPos, pos.y);
		}
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);

		if (bouncing) {
			return;
		}
		if (bounces < 4) {
			bounceOnEnemy();
		} else {
			destroySelf();
		}
	}

	public void chargeRockNoiseCrush() {
		if (!ownedByLocalPlayer) {
			return;
		}
		charged = true;
		rock?.hasChargedNoiseCrush = true;
		destroySelf();
	}

	public void bounceOnEnemy() {
		if (!ownedByLocalPlayer || spawn == null || bouncing)  {
			return;
		}
		spawn.bounce();
	}

	public void startBounce(float timer = 0) {
		bouncing = true;
		bounces++;
		bounceTimer = timer;
	}

	public void bounce() {
		vel.x *= -1;
		xDir *= -1;
		time = 0;
		bouncing = false;
	}

	public override void onDamageEX(IDamagable damagable) {
		base.onDamageEX(damagable);
		if (!ownedByLocalPlayer || destroyOnHit) {
			return;
		}
		spawn?.bounce();
	}
}


public class NoiseCrushChargedRmProj : Projectile {
	NoiseCrushSpawnerRm? spawn;

	public NoiseCrushChargedRmProj(
		Actor owner, Point pos, int xDir, int type,
		ushort? netProjId, NoiseCrushSpawnerRm? spawn = null,
		bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "noise_crush_charged_top", netProjId, altPlayer
	) {

		projId = (int)RockProjIds.NoiseCrushCharged;
		maxTime = 1f;

		vel.x = 240 * xDir;
		damager.damage = 2;
		damager.hitCooldown = 20;

		if (type == 1) changeSprite("noise_crush_charged_middle", true);
		else if (type == 2) changeSprite("noise_crush_charged_middle2", true);
		else if (type == 3) {
			changeSprite("noise_crush_charged_bottom", true);
		}

		if (ownedByLocalPlayer) {
			this.spawn = spawn;
		}

		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type };

			rpcCreate(pos, owner, ownerPlayer, netProjId, xDir, extraArgs);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new NoiseCrushChargedRmProj(
			arg.owner, arg.pos, arg.xDir,
			arg.extraData[0], arg.netId, altPlayer: arg.player
		);
	}

	public override void onDestroy() {
		base.onDestroy();
		if (!ownedByLocalPlayer) return;

		if (damagedOnce) {
			spawn?.destroyProjs(pos);
		}
	}
}
