using System;
using System.Collections.Generic;

namespace MMXOnline;

public class SBassBuster : Weapon {

	public static SBassBuster netWeapon = new();
	public SBassBuster() : base() {
		index = (int)BassWeaponIds.SuperBassBuster;
		fireRateFrames = 15;
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

		if (chargeLevel >= 2) {
			//new SuperBassRP(shootPos, xDir, player, player.getNextActorNetId(), true);
			//character.playSound("super_adaptor_punch", sendRpc: true);
			new ChamoBuster(shootPos, xDir, player, player.getNextActorNetId(), true);
			character.playSound("buster3", sendRpc: true);
		} 
		else if (chargeLevel == 1) {
			new SBassShot(shootPos, xDir, player, player.getNextActorNetId(), true);
			character.playSound("buster2", sendRpc: true);
		}
		else {
			float ang = 32;
			if (xDir < 0) ang = -ang + 128;
			float speed = 360;

			new SBassLemon(shootPos, xDir, player, player.getNextActorNetId(), true)
				{ vel = Point.createFromByteAngle(-ang).times(speed) };

			new SBassLemon(shootPos, xDir, player, player.getNextActorNetId(), true);

			new SBassLemon(shootPos, xDir, player, player.getNextActorNetId(), true)
				{ vel = Point.createFromByteAngle(ang).times(speed) };

			character.playSound("buster");
		}	
	}
}


public class SBassLemon : Projectile {
	public SBassLemon(
		Point pos, int xDir, Player player,
		ushort? netId, bool rpc = false
	) : base(
		SBassBuster.netWeapon, pos, xDir, 360, 1,
		player, "bass_buster_proj", 0, 0.15f,
		netId, player.ownedByLocalPlayer
	) {
		projId = (int)BassProjIds.SuperBassLemon;
		maxTime = 0.3f;
	}
}

public class SBassShot : Projectile {

	public SBassShot(
		Point pos, int xDir, Player player,
		ushort? netId, bool rpc = false
	) : base(
		SBassBuster.netWeapon, pos, xDir, 300, 2,
		player, "bass_buster_proj2", 0, 0,
		netId, player.ownedByLocalPlayer
	) {
		projId = (int)BassProjIds.SuperBassShot;
		maxTime = 0.5f;
	}
}


public class ChamoBuster : Projectile {
	public ChamoBuster(
		Point pos, int xDir, Player player,
		ushort? netId, bool rpc = false
	) : base(
		SBassBuster.netWeapon, pos, xDir, 340, 3,
		player, "bass_chamobuster", Global.halfFlinch, 0,
		netId, player.ownedByLocalPlayer
	) {
		projId = (int)BassProjIds.ChamoBuster;
		maxTime = 0.5f;
	}
}


public class SuperBassRP : Projectile {

	Bass bass = null!;
	Player player;
	float maxReverseTime;
	bool reversed;
	Actor? target;

	public SuperBassRP(
		Point pos, int xDir, Player player,
		ushort? netId, bool rpc = false
	) : base(
		SBassBuster.netWeapon, pos, xDir, 240, 3,
		player, "sb_rocket_punch", Global.halfFlinch, 0.5f,
		netId, player.ownedByLocalPlayer
	) {
		projId = (int)BassProjIds.SuperBassRocketPunch;
		bass = player.character as Bass ?? throw new NullReferenceException();
		if (bass != null) bass.sbRocketPunch = this;
		maxReverseTime = 0.5f;
		this.player = player;
		destroyOnHit = false;
		canBeLocal = false;
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
			move(pos.directionToNorm(targetPos).times(speed));
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

			move(pos.directionToNorm(returnPos).times(speed));
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
		if (bass != null) bass.sbRocketPunch = null;
	}
}
