using System;

namespace MMXOnline;

public class RemoteMine : Weapon {

	public static RemoteMine netWeapon = new();

	public RemoteMine() : base() {
		index = (int)BassWeaponIds.RemoteMine;
		displayName = "REMOTE MINE";
		weaponSlotIndex = index;
		weaponBarBaseIndex = index;
		weaponBarIndex = index;
		fireRateFrames = 45;
	}

	public override bool canShoot(int chargeLevel, Player player) {
		Bass? bass = player.character as Bass;

		return bass?.rMine == null && base.canShoot(chargeLevel, player);
	}
	public override void shoot(Character character, params int[] args) {
		Point shootPos = character.getShootPos();
		Player player = character.player;

		new RemoteMineProj(shootPos, character.getShootXDir(), player, player.getNextActorNetId(), true);
	}
}


public class RemoteMineProj : Projectile {
	bool exploded;
	bool landed;
	Character? host;
	Anim? anim;
	Bass? bass;

	public RemoteMineProj(
		Point pos, int xDir, Player player, 
		ushort? netProjId, bool rpc = false
	) : base (
			RemoteMine.netWeapon, pos, xDir, 240, 0,
			player, "remote_mine_proj", 0, 0.5f, 
			netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)BassProjIds.RemoteMine;
		maxTime = 1.25f;
		bass = player.character as Bass;
		if (bass != null) bass.rMine = this;
		anim = new Anim(getCenterPos(), "remote_mine_anim", xDir, player.getNextActorNetId(), false, true);
		canBeLocal = false;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new RemoteMineProj(
			arg.pos, arg.xDir, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();
		if (time >= maxTime){
			new Anim(getCenterPos(), "remote_mine_fade_anim", xDir, 
			netId, true, true);}
		if (host != null) changePos(host.getCenterPos());
		if (anim != null) anim.changePos(getCenterPos());

		int moveY = owner.input.getYDir(owner);
		if (moveY != 0 && !landed) move(new Point(0, 90 * moveY ));

		if (ownedByLocalPlayer && bass?.rMine != null &&
			landed && owner.input.isPressed(Control.Shoot, owner)
		) {
			destroySelf();
		}
	}

	public override void onCollision(CollideData other) {
		var chr = other.gameObject as Character;
		var wall = other.gameObject as Wall;

		if (!landed && (chr != null || wall != null)) {
			stopMoving();

			if (chr != null) host = chr; 
			changeSprite("remote_mine_land", true);
			landed = true;
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		if (!exploded) {
			explode();
		}
		if (anim != null) anim.destroySelf();
		if (bass != null) bass.rMine = null!;
	}

	void explode() {
		destroySelf();
		if (ownedByLocalPlayer) {
			new RemoteMineExplosionProj(pos, xDir, damager.owner, damager.owner.getNextActorNetId(), true);
		}
	}
}


public class RemoteMineExplosionProj : Projectile {

	int expTime;
	Anim? part;
	int animLap;

	public RemoteMineExplosionProj(
		Point pos, int xDir, Player player, 
		ushort? netProjId, bool rpc = false
	) : base (
		RemoteMine.netWeapon, pos, xDir, 0, 2,
		player, "remote_mine_explosion", Global.halfFlinch, 1,
		netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)BassProjIds.RemoteMineExplosion;
		maxTime = 0.75f;
		destroyOnHit = false;
		shouldShieldBlock = false;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new RemoteMineExplosionProj(
			arg.pos, arg.xDir, arg.player, arg.netId
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
				part = new Anim(pos, "remote_mine_explosion_part", xDir,
					owner.getNextActorNetId(), true, true);
				
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
}
