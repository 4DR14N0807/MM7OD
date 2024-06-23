using System;
using System.Collections.Generic;

namespace MMXOnline;

public class SearchSnake : Weapon {
    public SearchSnake() : base() {
        index = (int)RockWeaponIds.SearchSnake;
        fireRateFrames = 45;
    }


     public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
        Point shootPos = character.getShootPos();
        int xDir = character.getShootXDir();
			new SearchSnakeProj(this, shootPos, xDir, character.player, character.player.getNextActorNetId(), true);
	}
}
public class SearchSnakeProj : Projectile {
	public SearchSnakeProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
		base(weapon, pos, xDir, 0, 2, player, "search_snake_proj", 0, 0.5f, netProjId, player.ownedByLocalPlayer) {
		projId = (int)BluesProjIds.SearchSnake;
        wallCrawlSpeed = 120;
		destroyOnHit = true;
		fadeSprite = "generic_explosion";
		fadeOnAutoDestroy = true;
	   // useGravity = true;
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
    /*public override void onHitWall(CollideData other) {
		if (!other.gameObject.collider.isClimbable) return;
       // useGravity = false;
        }
   /* public override void postUpdate() {
		base.postUpdate();
		useGravity = false;
	}*/
    //TODO: ADD PROJECTILE RPC
}
