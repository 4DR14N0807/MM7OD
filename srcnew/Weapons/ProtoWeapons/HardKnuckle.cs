using System;

namespace MMXOnline;

public class HardKnuckle : Weapon {
	public static HardKnuckle netWeapon = new();

    public HardKnuckle() : base() {
        index = (int)RockWeaponIds.HardKnuckle;
        fireRateFrames = 75;
    }

    public override void shoot(Character character, params int[] args) {
	    base.shoot(character, args);
        Point shootPos = character.getShootPos();
        int xDir = character.getShootXDir();
        character.changeState(new HardKnuckleShoot(), true);
	}
}

public class HardKnuckleProj : Projectile {
    bool changedDir;
    Player player;

    public HardKnuckleProj(
		Point pos, int xDir, Player player, ushort? netId, bool rpc = false
	) : base(
		HardKnuckle.netWeapon, pos, xDir, 0, 2, player, "generic_explosion",
		0, 0, netId, player.ownedByLocalPlayer
	) {
        maxTime = 1f;
        projId = (int)BluesProjIds.HardKnuckle;
        this.player =  player;
        canBeLocal = false;
    }

    public override void update() {
        base.update();
        if (isAnimOver() && sprite.name == "generic_explosion") {
			changeSprite("hard_knuckle_proj", true);
			vel.x = 200 * xDir;
        }
        if(sprite.name != "generic_explosion"){
		int inputYDir = player.input.getYDir(player);
		vel.y = 100f * inputYDir;}
    }
     //TODO: ADD RPC CODE.
}

public class HardKnuckleShoot : CharState {
	bool fired;

	public HardKnuckleShoot() : base("hardknuckle", "", "","") {
		airMove = true;
		useGravity = false;
	}
	public override void update() {
        base.update();

        if (!fired && character.frameIndex == 1) {
            fired = true;
            new HardKnuckleProj(
				character.getShootPos(), character.xDir,
				player, player.getNextActorNetId(), true
			);
        } 
        if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
    }

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
        character.stopMovingWeak();
        bool air = !character.grounded;
        sprite = "hardknuckle";
        defaultSprite = sprite;
        landSprite = "hardknuckle_air";
        if (air) {
			sprite = "hardknuckle_air";
			defaultSprite = sprite;
		}
        character.changeSpriteFromName(sprite, true);
        character.useGravity = false;
        
	}
}
