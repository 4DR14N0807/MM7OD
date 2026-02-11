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

	float shootTime;
	float shootMaxTime = 3;
	int shotCount;
	int shotMaxCount = 5;
	bool charged;
	Rock rock = null!;
	NoiseCrush wep = null!;
	public List<NoiseCrushRmProj> projectiles = new();
	List<NoiseCrushChargedRmProj> projectilesCharged = new();
	public NoiseCrushSpawnerRm(
		Actor owner, Weapon weapon, Point pos, int xDir, bool charged,
		ushort? netId, bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "empty", netId, altPlayer
	) {
		projId = (int)RockProjIds.NoiseCrushSpawner;
		maxTime = 1.25f;
		this.charged = charged;

		if (ownedByLocalPlayer) {
			rock = owner as Rock ?? throw new NullReferenceException();
			wep = weapon as NoiseCrush ?? throw new NullReferenceException();
		}
	}

	public override void update() {
		base.update();

		Helpers.decrementFrames(ref shootTime);
		if (shootTime <= 0 && shotCount < shotMaxCount && !destroyed) {
			shootProj();
			shotCount++;
			shootTime = shootMaxTime;
		} else if (shotCount >= shotMaxCount) {
			destroySelf();
		}
	}

	void shootProj() {
		if (!ownedByLocalPlayer) return;

		if (charged) {
			int[] types = new[] { 0, 0, 1, 2, 3 };
			projectilesCharged.Add(
				new NoiseCrushChargedRmProj(
					rock, pos, xDir, types[shotCount],
					damager.owner.getNextActorNetId(), this, true
				) { zIndex = this.zIndex - shotCount }
			);
		} else {
			int[] types = new[] { 0, 0, 1, 1, 2 };
			projectiles.Add(
				new NoiseCrushRmProj(
					rock, pos, xDir, wep, types[shotCount],
					damager.owner.getNextActorNetId(), this, true
				) { zIndex = this.zIndex - shotCount }
			);
		}
	}

	public void bounce() {
		int i = 0;
		foreach (var proj in projectiles) {

			if (i == 0) {
				proj.bounce();
			} else {
				Global.level.delayedActions.Add(
					new DelayedAction(() => {
						proj.bounce();
					},
						(3 / 60f) * i
					)
				);
			}

			i++;
		}
	}

	public void destroyProjs(Point fadePos) {
		if (!ownedByLocalPlayer) return;

		foreach (NoiseCrushRmProj proj in projectiles) {
			proj.destroySelf();
		}
		foreach (NoiseCrushChargedRmProj projC in projectilesCharged) {
			projC.destroySelf();
		}
		projectiles.Clear();
		projectilesCharged.Clear();

		if (!destroyed) destroySelf();
		new Anim(fadePos, "noise_crush_fade", xDir, damager.owner.getNextActorNetId(), true, true);
	}
}


public class NoiseCrushRmProj : Projectile {
	NoiseCrushSpawnerRm spawn = null!;
	Rock rock = null!;
	int bounces;
	bool charged;

	public NoiseCrushRmProj(
		Actor owner, Point pos, int xDir, Weapon wep,
		int type, ushort? netId, NoiseCrushSpawnerRm? spawn = null,
		bool rpc = false, Player? player = null
	) : base(
		pos, xDir, owner, "noise_crush_top", netId, player
	) {
		projId = (int)RockProjIds.NoiseCrush;
		maxTime = 0.75f;
		destroyOnHit = false;

		canBeLocal = false;

		vel.x = 240 * xDir;
		damager.damage = 2;
		damager.hitCooldown = 30;

		if (type == 1) changeSprite("noise_crush_middle", true);
		else if (type == 2) changeSprite("noise_crush_bottom", true);

		if (ownedByLocalPlayer) {
			this.rock = owner as Rock ?? throw new NullReferenceException();
			this.spawn = spawn ?? throw new NullReferenceException();
		}

		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type };

			rpcCreate(pos, owner, ownerPlayer, netId, xDir, extraArgs);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new NoiseCrushRmProj(
			arg.owner, arg.pos, arg.xDir,
			NoiseCrush.netWeapon, arg.extraData[0], arg.netId
		);
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;

		if (pos.distanceTo(rock.getCenterPos()) <= 20 && bounces >= 1) {
			chargeRockNoiseCrush();
		}
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);

		if (bounces < 4) {

			if (other.isSideWallHit()) {
				bounce();
			}
		}
	}

	void chargeRockNoiseCrush() {
		if (!ownedByLocalPlayer) return;

		charged = true;
		rock.hasChargedNoiseCrush = true;
		destroySelf();
	}

	void bounceOnEnemy() {
		if (!ownedByLocalPlayer) return;

		spawn.bounce();
	}

	public void bounce() {
		if (!ownedByLocalPlayer) return;

		vel.x *= -1;
		xDir *= -1;
		//incPos(new Point(5 * xDir, 0));
		bounces++;
		time = 0;
	}

	public override void onDamageEX(IDamagable damagable) {
		base.onDamageEX(damagable);
		if (!ownedByLocalPlayer) return;

		bounceOnEnemy();
	}

	public override void onDestroy() {
		base.onDestroy();
		if (!ownedByLocalPlayer) return;

		if (damagedOnce || charged) spawn.destroyProjs(pos);
	}
}


public class NoiseCrushChargedRmProj : Projectile {
	NoiseCrushSpawnerRm spawn = null!;

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
			this.spawn = spawn ?? throw new NullReferenceException();
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

		if (damagedOnce) spawn.destroyProjs(pos);
	}
}
