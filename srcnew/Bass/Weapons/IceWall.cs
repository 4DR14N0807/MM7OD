using System;
using System.Collections.Generic;
using Newtonsoft.Json.Converters;

namespace MMXOnline;

public class IceWall : Weapon {

	public static IceWall netWeapon = new();

	public IceWall() : base() {
		index = (int)BassWeaponIds.IceWall;
		weaponSlotIndex = 1;
		fireRateFrames = 120;
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Point shootPos = character.getShootPos();
		Player player = character.player;

		new IceWallProj(shootPos, character.getShootXDir(), player, player.getNextActorNetId(), true);
	}
}


public class IceWallStart : Anim {

	Player player;

	public IceWallStart(
		Point pos, int xDir, ushort? netId, Player player,
		bool sendRpc = false, bool ownedByLocalPlayer = true
	) : base(
		pos, "ice_wall_spawn", xDir, netId, true, 
		sendRpc, ownedByLocalPlayer, player.character
	) {
		this.player = player;
	}

	public override void onDestroy() {
		base.onDestroy();

		new IceWallProj(pos, xDir, player, player.getNextActorNetId(), true);
	}
}
	

public class IceWallProj : Projectile {

	float maxSpeed = 300;
	int bounces;
	bool startedMoving;
	List<Character> chrs = new();
	public IceWallProj(
		Point pos, int xDir, Player player,
		ushort? netProjId, bool rpc = false
	) : base(
		IceWall.netWeapon, pos, xDir, 0, 0,
		player, "ice_wall_proj", 0, 1,
		netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 5f;
		destroyOnHit = false;
		useGravity = true;
		isPlatform = true;
		collider.wallOnly = true;
		fadeSprite = "ice_wall_fade";
	}


	public override void update() {
		base.update();

		if (startedMoving && Math.Abs(vel.x) < maxSpeed) {
			vel.x += xDir * Global.speedMul * 7.5f;
			
			if (Math.Abs(vel.x) > maxSpeed) vel.x = maxSpeed * xDir;
		}

		if (bounces >= 3) destroySelf();
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		if (other.isSideWallHit()) {		
			xDir *= -1;
		}
	}


	public override void onCollision(CollideData other) {
		base.onCollision(other);
		var wall = other.gameObject as Wall;
		var own = netOwner?.character;
		var chr = other.gameObject as Character;

		/*//Wall hit.
		if (wall != null) {
			if (other.isSideWallHit()) {
				xDir *= -1;
				playSound("ding");
				bounces++;
			}
		}*/

		//Movement start.
		if (own != null) {
			if (other.isSideWallHit() && own.charState is Run or Dash) {
				startedMoving = true;
			}
		}

		if (chr != null) {
			if (other.isSideWallHit()) {
				foreach (var enemy in chrs) {
					if (chr != enemy) {
						chrs.Add(chr);
						maxSpeed -= 100;
					} 
				}
			} else if (other.isGroundHit() && vel.y < 120 && 
				chr.canBeDamaged(damager.owner.alliance, damager.owner.id, projId)) {

				damager.applyDamage(chr, false, weapon, this, projId, 3, Global.halfFlinch);
			}
		}
	}
}
