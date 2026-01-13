using System;
using System.Collections.Generic;

namespace MMXOnline;

public class FreezeMCrystalPiece : Anim {
    Point bounceVel;
    public FreezeMCrystalPiece(
        Point pos, int xDir, int yDir, ushort? netId, Point vel, Point bounceVel, int frame
    ) : base(
        pos, "freezem_crystal_pieces", xDir, netId, false
    ) {
        this.yDir = yDir;
        this.vel = vel;
        this.bounceVel = bounceVel;
        useGravity = true;
        frameSpeed = 0;
        frameIndex = frame;
        if (collider != null) collider.wallOnly = true;
    }

	public override void update() {
		base.update();

        visible = Global.isOnFrameCycle(2);
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);

        if (
            (other.gameObject is Wall || 
            other.gameObject is MovingPlatform || 
            other.gameObject is Actor { isSolidWall: true }) &&

            collider?.wallOnly == true
        ) {
            vel = bounceVel;
            if (collider != null) collider.wallOnly = false;
        }
	}
}

public class FreezeMWarpIn : CharState {

    FreezeMan? freezem = null;

    public FreezeMWarpIn() : base("warp_in") {
        useGravity = false;
        invincible = true;
        statusEffectImmune = true;
    }

    public override void onEnter(CharState oldState) {
        base.onEnter(oldState);
        character.changePos(character.pos.addxy(0, 1));
        freezem = character as FreezeMan;
    }

	public override void update() {
		base.update();
        if (character.frameIndex == 5 && !once) {
            freezem?.breakIce();
            once = true;
        }

        if (character.isAnimOver()) {
            if (!player.warpedInOnce) character.changeState(new WarpIdle(player.warpedInOnce));
            else character.changeToIdleOrFall();
        }
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
        if (character.ownedByLocalPlayer) {
			character.invulnTime = player.warpedInOnce ? 2 * 60 : 0;
		}
        player.warpedInOnce = true;
	}
}


public class FreezeMAttackState : CharState {

    Point input;
    bool freeze;
    FreezeMan? freezem = null;

    public FreezeMAttackState(Point input, bool freeze) : base("spritent") {
        sprite = getSprite(input);
        landSprite = sprite;
        airSprite = sprite + "_air";
        fallSprite = airSprite;
        airMove = true;
        this.input = input;
        this.freeze = freeze;
    }

    string getSprite(Point input) {
        if (input.x == 0) {
            if (input.y == -1) {
                return "attack_up";
            } else if (input.y == 1) {
                return "attack_down";
            } 
        }
        return "attack";
    }  

    public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		bool air = !character.grounded || character.vel.y < 0;
		if (air) character.changeSpriteFromName(airSprite, true);
        freezem = character as FreezeMan;
	}
    
	public override void update() {
		base.update();

        if (!once && character.currentFrame.getBusterOffset() != null) {
            Point spawnPos = character.getShootPos();
            int ang = (int)input.byteAngle;
            if (input == Point.zero) ang = character.xDir > 0 ? 0 : 128;

            Point offset = Point.zero;
            if (sprite == "attack") offset = new Point(character.xDir * 4, -2);

            new Anim(
                spawnPos.add(offset), "freezem_fcracker_muzzle", character.xDir, player.getNextActorNetId(), true, true
            );

            if (freeze) {
                new FreezeMFreezeProj(
                    character, spawnPos, character.xDir, ang, player.getNextActorNetId(), rpc: true
                );
                character.playSound("freezemShoot", sendRpc: true);
                freezem?.triggerCooldown((int)FreezeMan.AttackIds.Freeze);
            } else {
                new FreezeMProj(
                    character, spawnPos, character.xDir, ang, player.getNextActorNetId(), rpc: true
                );
                character.playSound("buster2", sendRpc: true);
                freezem?.triggerCooldown((int)FreezeMan.AttackIds.Attack);
            }
            once = true;
        }

        if (character.isAnimOver()) character.changeToIdleOrFall();
	}
}


public class FreezeMChargeState : CharState {
    FreezeMan? freezem = null;
    public FreezeMChargeState() : base("charge") {
        normalCtrl = true;
        attackCtrl = true;
    }

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
        freezem = character as FreezeMan ?? throw new NullReferenceException();
	}

	public override void update() {
		base.update();

        if (stateFrames > 0 && stateFrames % 15 == 0 && freezem?.freezeAmmo < freezem?.freezeMaxAmmo) {
            freezem?.freezeAmmo += 1;
            freezem?.playSound("heal", sendRpc: true);
        }

        if (
            (!player.input.isHeld(Control.WeaponLeft, player)) || 
            (freezem?.freezeAmmo >= freezem?.freezeMaxAmmo && stateFrames % 15 >= 14)
        ) {
            character.changeToIdleOrFall(); 
        }  
	}
}


public class FreezeMGuardState : CharState {

    FreezeMan? freezem = null;
    bool hasAmmo => freezem?.freezeAmmo > 0;

    public FreezeMGuardState() : base("guard") {
        superArmor = true;
        pushImmune = true;
    }

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
        freezem = character as FreezeMan;
	}

	public override void update() {
		base.update();

        if ((!player.input.isHeld(Control.WeaponRight, player) || !hasAmmo) && freezem?.isGuarding == true) {
            character.changeState(new FreezeMGuardExitState(), true);
        }
	}
}


public class FreezeMGuardExitState : CharState {

    FreezeMan? freezem = null;
    bool hasAmmo => freezem?.freezeAmmo > 0;
    public FreezeMGuardExitState() : base("guard_exit") {
        
    }

    public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
        freezem = character as FreezeMan;
	}

	public override void update() {
		base.update();

        if (character.frameIndex == 1 && !once) {
            if (hasAmmo) {
                for (int i = 0; i < 3; i++) {
                    new FreezeMSplitProj2(
                        character, character.getCenterPos(), -1,
                        96 + (i * 32), player.getNextActorNetId(), true
                    );
                }
                for (int i = 0; i < 3; i++) {
                    new FreezeMSplitProj2(
                        character, character.getCenterPos(), 1,
                        -32 + (i * 32), player.getNextActorNetId(), true
                    );
                }
            }
            freezem?.breakIce();
            once = true;
        }

        if (character.isAnimOver()) character.changeToIdleOrFall();
	}
}