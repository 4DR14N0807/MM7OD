using System;
using System.Collections.Generic;
using Newtonsoft.Json.Converters;

namespace MMXOnline;

public class IceWall : Weapon {

	public static IceWall netWeapon = new();

	public IceWall() : base() {
		index = (int)BassWeaponIds.IceWall;
		displayName = "ICE WALL";
		weaponSlotIndex = index;
		weaponBarBaseIndex = index;
		weaponBarIndex = index;
		fireRateFrames = 120;
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Point shootPos = character.getShootPos();
		Player player = character.player;

		new IceWallActor(shootPos, character.getShootXDir(), player, player.getNextActorNetId(), true);
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

		new IceWallActor(pos, xDir, player, player.getNextActorNetId(), true);
	}
}
	

public class IceWallActor : Actor {

	float maxSpeed = 300;
	int bounces;
	bool startedMoving;
	List<Character> chrs = new();
	Player player;
	public IceWallActor(
		Point pos, int xDir, Player player,
		ushort? netId, bool rpc = false
	) : base(
		"ice_wall_proj", pos, netId, player.ownedByLocalPlayer, false
	) {
		
		useGravity = true;
		canBeLocal = false;
		base.xDir = xDir;
		this.player = player;
		collider.wallOnly = true;
		collider.isTrigger = false;
	}
	
	public override void update() {
		base.update();

		if (startedMoving && Math.Abs(vel.x) < maxSpeed) {
			vel.x += xDir * Global.speedMul * 7.5f;
			
			if (Math.Abs(vel.x) > maxSpeed) vel.x = maxSpeed * xDir;
		}

		if (bounces >= 3) destroySelf();
	}


	public override void onCollision(CollideData other) {
		base.onCollision(other);
		var wall = other.gameObject as Wall;
		var own = netOwner?.character;
		var chr = other.gameObject as Character;

		//Wall hit.
		if (wall != null) {
			if (other.isSideWallHit()) {
				xDir *= -1;
				playSound("ding");
				bounces++;
			}
		}

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
				chr.canBeDamaged(player.alliance, player.id, (int)BassProjIds.IceWall)) {

				chr.applyDamage(3, player, chr, (int)BassWeaponIds.IceWall, (int)BassProjIds.IceWall);
			}
		}
	}
}
