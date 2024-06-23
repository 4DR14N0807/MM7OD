using System;
using System.Collections.Generic;

namespace MMXOnline;

public class SearchSnake : Weapon {
	public static SearchSnake netWeapon = new();

	public SearchSnake() : base() {
		displayName = "Search Snake";
		descriptionV2 = "Releases snake-like missiles that crawl across surfaces.";
		defaultAmmoUse = 2;

		index = (int)RockWeaponIds.SearchSnake;
		fireRateFrames = 30;
	}

	public override float getAmmoUsage(int chargeLevel) {
		return defaultAmmoUse;
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
        Point shootPos = character.getShootPos();
        int xDir = character.getShootXDir();
		new SearchSnakeProj(shootPos, xDir, character.player, character.player.getNextActorNetId(), true);
		character.playSound("buster", sendRpc: true);
	}
}
public class SearchSnakeProj : Projectile {
	public SearchSnakeProj(
		Point pos, int xDir, Player player, ushort netProjId, bool rpc = false
	) : base(
		SearchSnake.netWeapon, pos, xDir, 0, 2, player, "search_snake_proj",
		0, 0.5f, netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)BluesProjIds.SearchSnake;
		wallCrawlSpeed = 120;
		destroyOnHit = true;
		//useGravity = true;
		setupWallCrawl(new Point(xDir, yDir));
		wallCrawlUpdateAngle = true;
		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void update() {
		base.update();
		updateWallCrawl();
	}
}
