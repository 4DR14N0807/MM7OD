using System;
using System.Collections.Generic;

namespace MMXOnline;

public class RemoteMine : Weapon {
	public static RemoteMine netWeapon = new();
	public RemoteMineProj? mine;

	public RemoteMine() : base() {
		iconSprite = "hud_weapon_icon_bass";
		index = (int)BassWeaponIds.RemoteMine;
		displayName = "REMOTE MINE";
		maxAmmo = 16;
		ammo = maxAmmo;
		weaponSlotIndex = index;
		weaponBarBaseIndex = index;
		weaponBarIndex = index;
		fireRate = 45;
		switchCooldown = 30;
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

		mine = new RemoteMineProj(bass, shootPos, bass.getShootXDir(), player.getNextActorNetId(), true);
	}
}


public class RemoteMineProj : Projectile {
	bool exploded;
	bool landed;
	bool landedOnce;
	bool wallLanded;
	Character? host;
	Anim? anim;
	string animName = "remote_mine_anim";
	Bass bass = null!;
	Wall? cWall;

	public RemoteMineProj(
		Actor owner, Point pos, int xDir, ushort? netProjId, 
		bool rpc = false, Player? altPlayer = null
	) : base (
			pos, xDir, owner, "remote_mine_proj", netProjId, altPlayer
	) {
		projId = (int)BassProjIds.RemoteMine;
		maxTime = 1.25f;
		destroyOnHit = false;

		vel.x = 240 * xDir;
		damager.hitCooldown = 30;

		if (ownedByLocalPlayer && owner.ownedByLocalPlayer) {
			bass = ownerPlayer.character as Bass ?? throw new NullReferenceException();
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

		if (host != null) changePos(host.getCenterPos());


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

		if (anim != null) anim.changePos(getCenterPos());

		int moveY = owner.input.getYDir(owner);
		if (moveY != 0 && !landedOnce) move(new Point(0, 90 * moveY ));

		if (ownedByLocalPlayer && bass?.rMine != null &&
			landed && owner.input.isPressed(Control.Shoot, owner) &&
			bass.currentWeapon is RemoteMine
		) {
			if (!exploded) {
				explode();
			}	
		}
	}

	public override void render(float x, float y) {
		base.render(x,y);
		if (anim == null || !visible) return;

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
		if (other.gameObject is KillZone) return;

		var chr = other.gameObject as Character;
		bool characterLand = (
			chr != null && chr != bass && 
			chr.canBeDamaged(bass.player.alliance, bass.player.id, projId)
		);
		
		if (!landed && (
			other.gameObject is Wall ||
			other.gameObject is MovingPlatform ||
			other.gameObject is Actor actor && (actor.isPlatform || actor.isSolidWall)
		)) {
			wallLanded = true;
			useGravity = false;
			destroySelf();
			playSound("remotemineStick", true);
			return;
		}
		if (!landed && (characterLand || wallLanded)) {
			if (characterLand) {
				host = chr;
				visible = false;
			}
			stopMoving(); 
			changeSprite("remote_mine_land", true);
			playSound("remotemineStick", true);
			landed = true;
			landedOnce = true;
			maxTime = 10;
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		if (!ownedByLocalPlayer || bass == null) {
			return;
		}

		if (anim != null) anim.destroySelf();
		if (bass != null) bass.rMine = null!;

		if (time >= maxTime && !exploded && landed) explode();
		if (wallLanded) bass.rMine = new RemoteMineLandProj(bass, pos, xDir, damager.owner.getNextActorNetId(), true);
	}

	public void explode() {
		destroySelf();
		if (ownedByLocalPlayer) {
			new RemoteMineExplosionProj(bass, pos, xDir, damager.owner.getNextActorNetId(), true);
			playSound("remotemineExplode", true);
		}
		exploded = true;
	}

	public override List<byte> getCustomActorNetData() {
		return [Helpers.boolArrayToByte([
			visible
		])];
	}
	public override void updateCustomActorNetData(byte[] data) {
		bool[] flags = Helpers.byteToBoolArray(data[0]);
		visible = flags[0];
	}
}


public class RemoteMineLandProj : Projectile {

	Bass bass = null!;
	Anim? anim;
	string animName = "remote_mine_anim";
	bool landed;
	bool wallLanded;
	bool characterLand;
	bool exploded;
	Character? host;
	public RemoteMineLandProj(
		Actor owner, Point pos, int xDir, ushort? netId,
		bool rpc = false, Player? player = null
	) : base(
		pos, xDir, owner, "remote_mine_land", netId, player
	) {
		projId = (int)BassProjIds.RemoteMineLand;
		maxTime = 10;
		destroyOnHit = false;

		if (ownedByLocalPlayer && owner.ownedByLocalPlayer) {
			bass = ownerPlayer.character as Bass ?? throw new NullReferenceException();
			bass.rMine = this;
		}
		canBeLocal = false;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new RemoteMineLandProj(
			arg.owner, arg.pos, arg.xDir, arg.netId, player: arg.player
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

		if (host != null) changePos(host.getCenterPos());
		else {
			moveWithMovingPlatform();
			if (!isColliding()) {
				explode();
				return;
			}
		}

		if (ownedByLocalPlayer && bass?.rMine != null &&
			owner.input.isPressed(Control.Shoot, owner) &&
			bass.currentWeapon is RemoteMine
		) {
			if (!exploded) {
				explode();
			}	
		}
	}

	public override void render(float x, float y) {
		base.render(x,y);
		if (anim == null || !visible) return;

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
		if (other.gameObject is KillZone) return;

		var chr = other.gameObject as Character;
		bool characterLand = (
			chr != null && chr != bass && 
			chr.canBeDamaged(bass.player.alliance, bass.player.id, projId)
		);
		
		if (!landed && characterLand) {
			if (characterLand) {
				host = chr;
				visible = false;
			}
			stopMoving(); 
			playSound("remotemineStick", true);
			landed = true;
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		if (!ownedByLocalPlayer) {
			return;
		}

		if (anim != null) anim.destroySelf();
		if (bass != null) bass.rMine = null!;

		if (time >= maxTime && !exploded && landed) explode();
	}

	public void explode() {
		destroySelf();
		if (ownedByLocalPlayer) {
			new RemoteMineExplosionProj(bass, pos, xDir, damager.owner.getNextActorNetId(), true);
			playSound("remotemineExplode", true);
		}
		exploded = true;
	}

	bool isColliding() {
		List<CollideData> collideDatas = Global.level.getTerrainTriggerList(this, new Point(0, 1));
		foreach (CollideData collideData in collideDatas) {
			if (collideData.gameObject is Wall or MovingPlatform) {
				return true;
			}
			if (collideData.gameObject is Actor actor &&
				(actor.isSolidWall || actor.isPlatform)
			) {
				return true;

			}
		}
		return false;
	}

	public override List<byte> getCustomActorNetData() {
		return [Helpers.boolArrayToByte([
			visible
		])];
	}
	public override void updateCustomActorNetData(byte[] data) {
		bool[] flags = Helpers.byteToBoolArray(data[0]);
		visible = flags[0];
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


public class RemoteMineAnim : Anim {

	Character? chara;
	Player? player;
	Anim? anim;
	public RemoteMineAnim(
		Character chara, Player player, ushort? netId
	) : base(
		chara.getCenterPos(), "remote_mine_proj", chara.xDir, netId, false, true
	) {
		zIndex = chara.zIndex + 3;
		frameSpeed = 0;
		if (ownedByLocalPlayer) {
			this.chara = chara;
			this.player = player;
		} 
	}

	public override void onStart() {
		base.onStart();
		if (player == null) return;

		anim = new Anim(
			getCenterPos(), "remote_mine_anim", xDir, player.getNextActorNetId(), false
		) { visible = false };
	}

	public override void update() {
		base.update();
		if (chara != null) {
			changePos(chara.getCenterPos());
		}

		if (time >= 10) destroySelf();
	}

	public override void render(float x, float y) {
		base.render(x,y);
		if (anim == null) return;

		Point center = getCenterPos();

		Global.sprites[anim.sprite.name].draw(
			anim.frameIndex, center.x, center.y, xDir, yDir,
			null, alpha, 1, 1, zIndex
		);
	}

	public override void onDestroy() {
		base.onDestroy();
		anim?.destroySelf();
	}
}
