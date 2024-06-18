using System;
using System.Collections.Generic;

namespace MMXOnline;


public class HardKnuckle : Weapon {
    public HardKnuckle() : base() {
        index = (int)RockWeaponIds.HardKnuckle;
        fireRateFrames = 75;
    }


    public override void shoot(Character character, params int[] args) {
	    base.shoot(character, args);
        Point shootPos = character.getShootPos();
        int xDir = character.getShootXDir();
        character.changeState(new HardKnuckleShoot(false), true);
        
	}
}
public class HardKnuckleProj : Projectile {

    bool changedDir;
    Player player;
//public float projSpeed = 0; Ruben:this is where the code break. If i put it on the speed parts it will KABOOM

    public HardKnuckleProj(Weapon weapon, Point pos, int xDir, Player player, ushort? netId, bool rpc = false) : 
    base(weapon, pos, xDir, 0, 2, player, "generic_explosion", 0, 0, netId, player.ownedByLocalPlayer) {
        maxTime = 1f;
        
        projId = (int)RockProjIds.HardKnuckle;
        this.player =  player;
        canBeLocal = false;
        
    }


    public override void update() {
        base.update();
        if(isAnimOver()){
            changeSprite("hard_knuckle_proj", true);
            base.vel = new Point(180f, 0f);
        }
            if (player.input.isPressed(Control.Up, player)) {
                base.vel = new Point(180f, -180f);
            }
            else if (player.input.isPressed(Control.Down, player)) {
                base.vel = new Point(-180f, 180f);
            }
    }
}
public class HardKnuckleShoot : CharState {
	bool fired;
	public HardKnuckleShoot(bool grounded) : base("shoot", "", "","") {
		airMove = true;
	}

	public override void update() {
        base.update();

        if (!fired) {
            
            fired = true;
            new HardKnuckleProj(new HardKnuckle(), character.getCenterPos(), character.xDir, player, player.getNextActorNetId(), true);
        } 

        if (character.isAnimOver()) {
			if (character.grounded) character.changeState(new Idle(), true);
			else character.changeState(new Fall(), true);
		}
    }

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
        character.stopMoving();
        character.useGravity = false;
        bool air = !character.grounded || character.vel.y < 0;
        sprite = "shoot";
        defaultSprite = sprite;
        //landSprite = "shoot";
        if (air) {
			sprite = "shoot_air";
			defaultSprite = sprite;
		}
        character.changeSpriteFromName(sprite, true);
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
        character.useGravity = true;
	}
}