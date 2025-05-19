using System;
using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;

#region Buster Weapon

public class SBassBuster : Weapon {
	public static SBassBuster netWeapon = new();

	public SBassBuster() : base() {
		iconSprite = "hud_weapon_icon_bass";
		index = (int)BassWeaponIds.SuperBassBuster;
		fireRate = 15;
		drawAmmo = false;
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 0;
	} 

	public override void shoot(Character character, params int[] args) {
		Point shootPos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;
		int chargeLevel = args[0];
		Bass bass = character as Bass ?? throw new NullReferenceException();

		if (chargeLevel == 3) {
			new ChamoBuster(bass, shootPos.addxy(12 * xDir, 0), xDir, player.getNextActorNetId(), true);
			character.playSound("buster3", sendRpc: true);

			for (int i = 1; i < 3; i++) {
			Global.level.delayedActions.Add(new DelayedAction(
				() => {
					new SBassShot(
						bass, bass.getShootPos().addxy(11 * xDir, 0), bass.getShootXDir(), 
						player.getNextActorNetId(), true, superBass: bass.isSuperBass
					);
					character.playSound("buster2", sendRpc: true);
				},
				(i * 8) / 60f
			));
		}
		}
		else if (chargeLevel == 2) {
			new ChamoBuster(bass, shootPos, xDir, player.getNextActorNetId(), true);
			character.playSound("buster3", sendRpc: true);
		} 
		else if (chargeLevel == 1) {
			new SBassShot(bass, shootPos, xDir, player.getNextActorNetId(), true);
			character.playSound("buster2", sendRpc: true);
		}
		else {
			float ang = 32;
			if (xDir < 0) ang = -ang + 128;
			float speed = 360;

			new SBassLemon(bass, shootPos, xDir, player.getNextActorNetId(), true)
				{ vel = Point.createFromByteAngle(-ang).times(speed) };

			new SBassLemon(bass, shootPos, xDir, player.getNextActorNetId(), true);

			new SBassLemon(bass, shootPos, xDir, player.getNextActorNetId(), true)
				{ vel = Point.createFromByteAngle(ang).times(speed) };

			character.playSound("buster");
		}	
	}
}

#endregion

#region Rocket Punch Weapon

public class SBassRP : Weapon {
	public SBassRP() : base() {
		iconSprite = "hud_weapon_icon_bass";
		index = (int)BassWeaponIds.SuperBassRP;
		fireRate = 15;
		drawAmmo = false;
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 0;
	} 

	public override void shoot(Character character, params int[] args) {
		Point shootPos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;
		int chargeLevel = args[0];
		Bass bass = character as Bass ?? throw new NullReferenceException();

		if (chargeLevel >= 2) {
			new SuperBassRP(bass, shootPos, xDir, player.getNextActorNetId(), true);
			character.playSound("super_adaptor_punch", sendRpc: true);
		} 
		else if (chargeLevel == 1) {
			new SBassShot(bass, shootPos, xDir, player.getNextActorNetId(), true);
			character.playSound("buster2", sendRpc: true);
		}
		else {
			float ang = 32;
			if (xDir < 0) ang = -ang + 128;
			float speed = 360;

			new SBassLemon(bass, shootPos, xDir, player.getNextActorNetId(), true)
				{ vel = Point.createFromByteAngle(-ang).times(speed) };

			new SBassLemon(bass, shootPos, xDir, player.getNextActorNetId(), true);

			new SBassLemon(bass, shootPos, xDir, player.getNextActorNetId(), true)
				{ vel = Point.createFromByteAngle(ang).times(speed) };

			character.playSound("buster");
		}	
	}
}

#endregion


#region Projectiles
public class SBassLemon : Projectile {
	public SBassLemon(
		Actor owner, Point pos, int xDir, ushort? netId, 
		bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "bass_buster_proj", netId, altPlayer
	) {
		projId = (int)BassProjIds.SuperBassLemon;
		maxTime = 0.3f;

		vel.x = 360 * xDir;
		damager.damage = 1;
		damager.hitCooldown = 9;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new SBassLemon(
			arg.owner, arg.pos, arg.xDir, arg.netId, altPlayer: arg.player
		);
	}
}

public class SBassShot : Projectile {

	public SBassShot(
		Actor owner, Point pos, int xDir, ushort? netId, 
		bool rpc = false, Player? altPlayer = null, bool superBass = false
	) : base(
		pos, xDir, owner, "bass_buster_proj2", netId, altPlayer
	) {
		projId = (int)BassProjIds.SuperBassShot;
		maxTime = 0.5f;

		vel.x = 300 * xDir;
		damager.damage = 2;
		if (superBass) {
			damager.damage = 1;
			damager.flinch = Global.miniFlinch;
		}

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new SBassShot(
			arg.owner, arg.pos, arg.xDir, arg.netId, altPlayer: arg.player
		);
	}
}


public class ChamoBuster : Projectile {
	public ChamoBuster(
		Actor owner, Point pos, int xDir, ushort? netId, 
		bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "bass_chamobuster", netId, altPlayer
	) {
		projId = (int)BassProjIds.ChamoBuster;
		maxTime = 0.5f;

		vel.x = 350 * xDir;
		damager.damage = 3;
		damager.flinch = Global.halfFlinch;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new ChamoBuster(
			arg.owner, arg.pos, arg.xDir, arg.netId, altPlayer: arg.player
		);
	}
}


public class SuperBassRP : Projectile {
	Bass bass = null!;
	Player player;
	float maxReverseTime;
	bool reversed;
	Actor? target;
	float projSpeed = 240;

	public SuperBassRP(
		Actor owner, Point pos, int xDir, ushort? netId, 
		bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "sb_rocket_punch", netId, altPlayer
	) {
		projId = (int)BassProjIds.SuperBassRocketPunch;
		if (ownedByLocalPlayer) {
			bass = owner as Bass ?? throw new NullReferenceException();
			if (bass != null) bass.sbRocketPunch = this;
		}
		
		maxReverseTime = 0.5f;
		this.player = ownerPlayer;


		vel.x = projSpeed * xDir;
		damager.damage = 3;
		damager.flinch = Global.halfFlinch;
		damager.hitCooldown = 30;

		destroyOnHit = false;
		canBeLocal = false;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new SuperBassRP(
			arg.owner, arg.pos, arg.xDir, arg.netId, altPlayer: arg.player
		);
	}

	public override void update() {
		base.update();
		if (!locallyControlled) return;

		if (ownedByLocalPlayer && (bass == null || bass.destroyed)) {
			destroySelf("generic_explosion");
			return;
		}

		var targets = Global.level.getTargets(bass.pos, player.alliance, true);
		foreach (var t in targets) {
			if (bass.isFacing(t) && MathF.Abs(t.pos.y - bass.pos.y) < 80) {
				target = t;
				break;
			}
		}

		if (!reversed && target != null) {
			vel = new Point(0, 0);
			if (pos.x > target.pos.x) xDir = -1;
			else xDir = 1;
			Point targetPos = target.getCenterPos();
			move(pos.directionToNorm(targetPos).times(projSpeed));
			if (pos.distanceTo(targetPos) < 5) {
				reversed = true;
			}
		}

		if (!reversed && time > maxReverseTime) reversed = true;

		if (reversed) {
			vel = new Point(0, 0);
			if (pos.x > bass.pos.x) xDir = -1;
			else xDir = 1;

			Point returnPos = bass.getCenterPos();
			if (bass.sprite.name == "rock_rocket_punch") {
				Point poi = bass.pos;
				var pois = bass.sprite.getCurrentFrame()?.POIs;
				if (pois != null && pois.Length > 0) {
					poi = pois[0];
				}
				returnPos = bass.pos.addxy(poi.x * bass.xDir, poi.y);
			}

			move(pos.directionToNorm(returnPos).times(projSpeed));
			if (pos.distanceTo(returnPos) < 10) {
				destroySelf();
				Global.playSound("super_adaptor_punch_recover");
			}
		}
	}

	public void followOwner() {
		if (bass != null) {
			float targetPosX = bass.getCenterPos().x;
			float targetPosY =  bass.getCenterPos().y;
			float moveSpeed = speed;

			// X axis follow.
			if (pos.x < targetPosX) {
				move(new Point(moveSpeed, 0));
				if (pos.x > targetPosX) { pos.x = targetPosX; }
			} else if (pos.x > targetPosX) {
				move(new Point(-moveSpeed, 0));
				if (pos.x < targetPosX) { pos.x = targetPosX; }
			}
			// Y axis follow.
			if (pos.y < targetPosY) {
				move(new Point(0, moveSpeed));
				if (pos.y > targetPosY) { pos.y = targetPosY; }
			} else if (pos.y > targetPosY) {
				move(new Point(0, -moveSpeed));
				if (pos.y < targetPosY) { pos.y = targetPosY; }
			}
		}
	}

	public void followTarget() {
		if (target == null) {
			target = Global.level.getClosestTarget(
			new Point (pos.x, pos.y),
			damager.owner.alliance,
			false, 200
			);
		} else {
			Point enemyPos = target.getCenterPos();
			float moveSpeed = speed;

			// X axis follow.
			if (pos.x < enemyPos.x) {
				move(new Point(moveSpeed, 0));
				if (pos.x > enemyPos.x) { pos.x = enemyPos.x; }
			} else if (pos.x > enemyPos.x) {
				move(new Point(-moveSpeed, 0));
				if (pos.x < enemyPos.x) { pos.x = enemyPos.x; }
			}
			// Y axis follow.
			if (pos.y < enemyPos.y) {
				move(new Point(0, moveSpeed));
				if (pos.y > enemyPos.y) { pos.y = enemyPos.y; }
			} else if (pos.y > enemyPos.y) {
				move(new Point(0, -moveSpeed));
				if (pos.y < enemyPos.y) { pos.y = enemyPos.y; }
			}
		}
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		if (locallyControlled) {
			reversed = true;
		}
		if (isRunByLocalPlayer()) {
			reversed = true;
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		if (!ownedByLocalPlayer) return;
		if (bass != null) bass.sbRocketPunch = null;
	}
}


public class SweepingLaserProj : Projectile {

	int startHeight;
	Sprite? bodySprite;
	int spriteHeight;
	int maxPieces = 6;
	Point endPos;
	Anim? topAnim;
	Anim? bodyAnim;
	Anim? bottomAnim;
	bool ground;
	Bass bass = null!;
	int groundTime;
	int lastGroundTime;
	public SweepingLaserProj(
		Actor owner, Point pos, int xDir,
		ushort? netId, bool rpc = false, Player? player = null
	) : base(
		pos, xDir, owner, "sweeping_laser_top", netId, player
	) {

		projId = (int)BassProjIds.SweepingLaser;
		maxTime = 1;
		setIndestructableProperties();
		damager.damage = 3;
		damager.flinch = Global.halfFlinch;
		damager.hitCooldown = 30;

		if (ownedByLocalPlayer) {
			bass = owner as Bass ?? throw new NullReferenceException();
		}
	}

	public override void onStart() {
		base.onStart();
		if (!ownedByLocalPlayer) return;

		startHeight = (int)sprite.getCurrentFrame().rect.h();
		bodySprite = new Sprite("sweeping_laser");
		spriteHeight = (int)bodySprite.getCurrentFrame().rect.h();
		checkGround();

		topAnim = new Anim(pos, sprite.name, xDir, null, false) { visible = false };
		bodyAnim = new Anim(pos, bodySprite.name, xDir, null, false) { visible = false };
		bottomAnim = new Anim(pos, "sweeping_laser_bottom", xDir, null, false) { visible = false };
	}

	public override void update() {
		base.update();

		changePos(bass.getShootPos());
		checkGround();
		if (ground) groundTime++;

		if (groundTime != lastGroundTime && groundTime % 4 == 0 && ground) {
			new Anim(endPos, "dust_purple", xDir, damager.owner.getNextActorNetId(), true, true, zIndex: zIndex - 2)
			{ vel = new Point(0, -60) };
			lastGroundTime = groundTime;
		}
	}

	public override void render(float x, float y) {
		base.render(x,y);
		if (bodyAnim == null || bodySprite == null) return;

		int pieces = (MathInt.Floor(pos.distanceTo(endPos)) - startHeight) / spriteHeight;

		for (int i = 0; i < pieces; i++) {
			bodySprite.draw(
				bodyAnim.frameIndex, pos.x, pos.y + ((i * spriteHeight) + startHeight),
				xDir, yDir, null, alpha, 1, 1, zIndex
			);
		}

		if (ground && bottomAnim != null) {
			Global.sprites[bottomAnim.sprite.name].draw(
				bottomAnim.frameIndex, pos.x, endPos.y, 
				xDir, yDir, null, alpha, 1, 1, zIndex
			);
		}
	}

	public override void onDestroy() {
		base.onDestroy();

		topAnim?.destroySelf();
		bodyAnim?.destroySelf();
		bottomAnim?.destroySelf();
	}

	void checkGround() {
		var hits = Global.level.raycast(pos, pos.addxy(0, (maxPieces * spriteHeight) + startHeight), new List<Type>() { typeof(Wall), typeof(Ladder) });
		if (hits != null) {
			endPos = hits.getHitPointSafe();
			ground = true;
		} else {
			endPos = new Point(pos.x, pos.y + (maxPieces * spriteHeight) + startHeight);
			ground = false;
		}

		List<Point> points = new List<Point>() {
			new Point(pos.x - 6, pos.y),
			new Point(pos.x + 7, pos.y),
			new Point(pos.x + 7, endPos.y),
			new Point(pos.x - 6, endPos.y),
		};
		globalCollider = new Collider(points, true, null!, false, false, 0, Point.zero);
	}
}


public class DarkCometUpProj : Projectile {

	Actor? actor;
	Anim? anim;

	public DarkCometUpProj(
		Actor owner, Point pos, int xDir,
		ushort? netId, bool rpc = false, Player? player = null
	) : base(
		pos, xDir, owner, "dark_comet", netId, player
	) {
		projId = (int)BassProjIds.DarkCometUp;
		maxTime = 0.5f;
		damager.damage = 3;
		damager.flinch = Global.halfFlinch;
		
		vel.y = -240;
		yDir *= -1;

		if (ownedByLocalPlayer && owner != null) actor = owner;
	}

	public override void onStart() {
		base.onStart();
		anim = new Anim(pos, "dark_comet_anim", xDir, null, false) { visible = false };
	}

	public override void render(float x, float y) {
		if (anim != null) {
			Global.sprites[anim.sprite.name].draw(
				anim.frameIndex, pos.x, pos.y, 
				xDir, yDir, null, alpha, 1, 1, zIndex
			);
		}

		base.render(x,y);
	} 

	public override void onDestroy() {
		base.onDestroy();
		if (damagedOnce || !ownedByLocalPlayer || actor == null) return;

		for (int i = -1; i < 2; i++) {
			new DarkCometDownProj(
				actor, pos.addxy(48 * i, 0), xDir, 
				damager.owner.getNextActorNetId(), true, damager.owner
			);
		}
	}
}


public class DarkCometDownProj : Projectile {

	Anim? anim;

	public DarkCometDownProj(
		Actor owner, Point pos, int xDir,
		ushort? netId, bool rpc = false, Player? player = null
	) : base(
		pos, xDir, owner, "dark_comet", netId, player
	) {
		projId = (int)BassProjIds.DarkCometDown;
		maxTime = 1.5f;
		damager.damage = 3;
		damager.flinch = Global.halfFlinch;
		
		vel.y = 240;
	}

	public override void onStart() {
		base.onStart();
		anim = new Anim(pos, "dark_comet_anim", xDir, null, false) { visible = false };
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);

		var floor = other.gameObject as Wall;

		if (floor != null && other.isGroundHit()) {
			destroySelf();
			new Anim(pos, "dark_comet_land", xDir, damager.owner.getNextActorNetId(), true, true);
		}
	}

	public override void render(float x, float y) {
		if (anim != null) {
			Global.sprites[anim.sprite.name].draw(
				anim.frameIndex, pos.x, pos.y, 
				xDir, yDir, null, alpha, 1, 1, zIndex
			);
		}

		base.render(x,y);
	} 
}

#endregion
