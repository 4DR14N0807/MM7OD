using System;

namespace MMXOnline;

public class RemoteMine : Weapon {
	public static RemoteMine netWeapon = new();

	public RemoteMine() : base() {
		index = (int)BassWeaponIds.RemoteMine;
		displayName = "REMOTE MINE";
		maxAmmo = 16;
		ammo = maxAmmo;
		weaponSlotIndex = index;
		weaponBarBaseIndex = index;
		weaponBarIndex = index;
		fireRate = 45;
	}

	public override bool canShoot(int chargeLevel, Player player) {
		Bass? bass = player.character as Bass;

		return
		bass?.rMine == null && bass?.rMineExplosion == null &&
		base.canShoot(chargeLevel, player);
	}
	public override void shoot(Character character, params int[] args) {
		Point shootPos = character.getShootPos();
		Player player = character.player;
		Bass bass = character as Bass ?? throw new NullReferenceException();

		new RemoteMineProj(bass, shootPos, bass.getShootXDir(), player.getNextActorNetId(), true);
	}
}


public class RemoteMineProj : Projectile {
	bool exploded;
	bool landed;
	Character? host;
	Anim? anim;
	string animName = "remote_mine_anim";
	Bass bass = null!;

	public RemoteMineProj(
		Actor owner, Point pos, int xDir, ushort? netProjId, 
		bool rpc = false, Player? altPlayer = null
	) : base (
			pos, xDir, owner, "remote_mine_proj", netProjId, altPlayer
	) {
		projId = (int)BassProjIds.RemoteMine;
		maxTime = 1.25f;
		bass = ownerPlayer.character as Bass ?? throw new NullReferenceException();
		destroyOnHit = false;

		vel.x = 240 * xDir;
		damager.hitCooldown = 30;

		if (bass != null && bass.ownedByLocalPlayer) {
			bass.rMine = this;
		}
		canBeLocal = false;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netProjId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new RemoteMineProj(
			arg.owner, arg.pos, arg.xDir, arg.netId, altPlayer: arg.player
		);
	}

	public override void onStart() {
		base.onStart();
		if (bass != null && ownedByLocalPlayer && bass.ownedByLocalPlayer) {
			anim = new Anim(
				getCenterPos(), animName, xDir,
				bass.player.getNextActorNetId(), false, true
			);
			anim.visible = false;
		}
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) {
			return;
		}
		if (time >= maxTime && !destroyed && bass != null && bass.ownedByLocalPlayer){
			//ruben: cant put this as fade anim or on destroy because it will conflict with the explosion anim
			new Anim(
				getCenterPos(), "remote_mine_fade_anim", xDir, 
				bass.player.getNextActorNetId(), true, true
			);
		}
		if (host != null) changePos(host.getCenterPos());
		if (anim != null) anim.changePos(getCenterPos());

		int moveY = owner.input.getYDir(owner);
		if (moveY != 0 && !landed) move(new Point(0, 90 * moveY ));

		if (ownedByLocalPlayer && bass?.rMine != null &&
			landed && owner.input.isPressed(Control.Shoot, owner) &&
			bass.currentWeapon is RemoteMine
		) {
			destroySelf();
		}
	}

	public override void render(float x, float y) {
		base.render(x,y);
		if (anim == null) return;

		Point center = getCenterPos();

		Global.sprites[animName].draw(
			anim.frameIndex, center.x, center.y, xDir, yDir,
			null, alpha, 1, 1, zIndex
		);
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (!ownedByLocalPlayer) {
			return;
		}
		var chr = other.gameObject as Character;
		var wall = other.gameObject as Wall;

		if (!landed && ((chr != null && chr != bass && chr.canBeDamaged(bass.player.alliance, bass.player.id, projId)) || wall != null)) {
			stopMoving();
			if (chr != null) host = chr; 
			changeSprite("remote_mine_land", true);
			playSound("remotemineStick", true);
			landed = true;
			maxTime = 3;
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		if (!ownedByLocalPlayer) {
			return;
		}
		if (!exploded) {
			explode();
		}
		if (anim != null) anim.destroySelf();
		if (bass != null) bass.rMine = null!;
	}

	void explode() {
		destroySelf();
		if (ownedByLocalPlayer) {
			new RemoteMineExplosionProj(bass, pos, xDir, damager.owner.getNextActorNetId(), true);
			playSound("remotemineExplode", true);
		}
	}
}


public class RemoteMineExplosionProj : Projectile {
	int expTime;
	Anim? part;
	int animLap;
	Bass? bass;

	public RemoteMineExplosionProj(
		Actor owner, Point pos, int xDir, ushort? netProjId, 
		bool rpc = false, Player? altPlayer = null
	) : base (
		pos, xDir, owner, "remote_mine_explosion", netProjId, altPlayer
	) {
		projId = (int)BassProjIds.RemoteMineExplosion;
		maxTime = 0.75f;
		destroyOnHit = false;
		shouldShieldBlock = false;

		damager.damage = 2;
		damager.flinch = Global.halfFlinch;
		damager.hitCooldown = 60;

		if (ownedByLocalPlayer) {
			bass = ownerPlayer.character as Bass;
			if (bass != null) {
				bass.rMineExplosion = this;
			}
		}
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netProjId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new RemoteMineExplosionProj(
			arg.owner, arg.pos, arg.xDir, arg.netId, altPlayer: arg.player
		);
	}

	public override void update() {
		base.update();

		if (expTime % 2 == 0) visible = true;
		else visible = false;

		expTime++;

		if (part == null) {
			animLap++;
			if (animLap > 4) animLap = 1;
			for (int i = 0; i < 5; i++) {
				if (!ownedByLocalPlayer) return;
				part = new Anim(pos, "remote_mine_explosion_part", xDir,
					damager.owner.getNextActorNetId(), true, true);
				
				switch (animLap) {
					case 1: 
					part.vel = Point.createFromByteAngle(23 + (i * 21)) * 180;
					break;

					case 2:
					part.vel.y = 90 * (i - 2);
					break;

					case 3:
					part.vel = Point.createFromByteAngle(64 + (i * 16)) * 180;
					break;

					default:
					part.vel = Point.createFromByteAngle(0 + (i * 16)) * 180;
					break;
				}
			}
		} else {
			if (expTime % 2 != 0) part.visible = true;
			else part.visible = false;

			if (part.destroyed) part = null;
		} 
	}

	public override void onDestroy() {
		base.onDestroy();
		if (bass != null) {
			bass.rMineExplosion = null;
		}
	}
}
