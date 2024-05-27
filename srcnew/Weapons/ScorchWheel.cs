using System;
using System.Collections.Generic;

namespace MMXOnline;

public class ScorchWheel : Weapon {
   
    public static ScorchWheel netWeapon = new ScorchWheel();
    public ScorchWheel() : base() {
        index = (int)RockWeaponIds.ScorchWheel;
        weaponSlotIndex = (int)RockWeaponSlotIds.ScorchWheel;
        weaponBarBaseIndex = (int)RockWeaponBarIds.ScorchWheel;
        weaponBarIndex = weaponBarBaseIndex;
        killFeedIndex = 0;
        maxAmmo = 14;
        ammo = maxAmmo;
        rateOfFire = 1f;
        description = new string[] {"A weapon able to burn enemies.", "Hold SHOOT to keep the barrier for longer."};
    }

	public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId) {
        base.getProjectile(pos, xDir, player, chargeLevel, netProjId);

        if (player.character.ownedByLocalPlayer) {
            /*if (chargeLevel >= 2 && player.hasBusterLoadout()) {
                player.character.changeState(new RockChargeShotState(player.character.grounded), true);
            }
            else if (player.character is Rock rock)*/
            if (player.character.charState is LadderClimb) {
                player.character.changeState(new ShootAltLadder(this, (int)chargeLevel), true);
            } else {
                player.character.changeState(new ShootAlt(this, (int)chargeLevel), true);
            }
        }
    }
}


public class ScorchWheelSpawn : Projectile {
    
    public Rock? rock;
    bool hasHeld;
	public ScorchWheelSpawn(
        Weapon weapon, Point pos, int xDir, 
        Player player, ushort netProjId, 
        bool rpc = false
    ) : base (
        weapon, pos, xDir, 0, 0, 
        player, "scorch_wheel_spawn", 0, 0, 
        netProjId, player.ownedByLocalPlayer
    ) {
        projId = (int)RockProjIds.ScorchWheelSpawn;
        rock = (player.character as Rock);
		useGravity = false;
        maxTime = 1;
        destroyOnHit = false;

        if (rpc) rpcCreate(pos, player, netProjId, xDir);
	}

    public static Projectile rpcInvoke(ProjParameters arg) {
        return new ScorchWheelSpawn(
            ScorchWheel.netWeapon, arg.pos, arg.xDir, arg.player,
            arg.netId
        );
    }
    
	public override void update() {
        base.update();

        if (!ownedByLocalPlayer) return;

        //code that allow you to keep the wheel arround of you. Ruben: I swear if it doesnt work i kysm 
        //may be used as a base for future fixes
        if (owner.input.isHeld(Control.Shoot, owner)) {
            hasHeld = true;
        } else {
            hasHeld = false;
        }
        
        if (isAnimOver() && hasHeld == true) {
            new ScorchWheelProj(new ScorchWheel(), pos, xDir, damager.owner, damager.owner.getNextActorNetId(), rpc: true);
            destroySelf();
            playSound("scorch_wheel", true, true);        
        } else if (isAnimOver() 
                && hasHeld == false
        ) {
            destroySelf();
            new ScorchWheelMoveProj(weapon, pos, xDir, damager.owner, damager.owner.getNextActorNetId(), rpc: true);
            playSound("scorch_wheel", true, true);
            rock.shootTime = 1f;
        }

	}

    public override void postUpdate() {
		base.postUpdate();
		if (!ownedByLocalPlayer) return;
		if (destroyed) return;

        if (rock != null) {
            xDir = rock.getShootXDir();
            pos = rock.getCenterPos();
        }
	}
    
    /*public void moveWell(){
        if (time > 0.49f) {
			vel.x = xDir * 200;
		}
        if (!ownedByLocalPlayer) return;
        if (owner.character is Rock){ rock.sWell = null;}
    }*/
}


public class ScorchWheelProj : Projectile {

    float projAngle;
    Rock? rock;
    Player player;
    Point centerPos;
    public List<Sprite> fireballs = new List<Sprite>();
    int radius = 15;
    float holdTime;
    bool hasHeld;

    public ScorchWheelProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
    base(weapon, pos, xDir, 0, 1, player, "scorch_wheel_proj", 0, 0.5f, netProjId, player.ownedByLocalPlayer) {
        
        projId = (int)RockProjIds.ScorchWheel;
        destroyOnHit = false;
        this.player = player;
        rock = player.character as Rock;
        if (rock != null) rock.sWell = this;
        canBeLocal = false;

        for (var i = 0; i <4; i++) {
			var fireball = Global.sprites["scorch_wheel_fireball"].clone();
            fireballs.Add(fireball);
		}

        if (rpc) rpcCreate(pos, player, netProjId, xDir);
    }

    public static Projectile rpcInvoke(ProjParameters arg) {
        return new ScorchWheelProj(
            ScorchWheel.netWeapon, arg.pos, arg.xDir, arg.player,
            arg.netId
        );
    }


    public override void update() {
        base.update();
        if (projAngle >= 256) projAngle = 0;
        projAngle += 3;
        
        if (ownedByLocalPlayer) {
            if (player.input.isHeld(Control.Shoot, owner)) {
                hasHeld = true;
                holdTime += Global.spf;
            } else {
                hasHeld = false;
                holdTime = 0;
            }
            if (rock != null) {
                xDir = rock.getShootXDir();
                pos = rock.getCenterPos();
            }

            if (rock == null || rock.charState is Die || (rock.player.weapon is not ScorchWheel)) {
                destroySelf();
                return;
            }

            
            if (!hasHeld || holdTime >= 2) {
                destroySelf();
                new ScorchWheelMoveProj(weapon, pos, xDir, damager.owner, damager.owner.getNextActorNetId(true), rpc: true);
                playSound("scorch_wheel", true, true);
                rock.shootTime = 1f;
                return; 
            }
        }
    }

    
    public override void render(float x, float y) {
        base.render(x, y);

        if (rock != null) centerPos = rock.getCenterPos();
		
		//main pieces render
		for (var i = 0; i < 4; i++) {
			float extraAngle = projAngle + i*64;
			if (extraAngle >= 256) extraAngle -= 256;
			float xPlus = Helpers.cosd(extraAngle * 1.40625f) * radius;
			float yPlus = Helpers.sind(extraAngle * 1.40625f) * radius;
			if (rock != null) xDir = rock.getShootXDir();
			fireballs[i].draw(frameIndex, centerPos.x + xPlus, centerPos.y + yPlus, xDir, yDir, getRenderEffectSet(), 1, 1, 1, zIndex);
		}
    }

    public override void onDestroy() {
		base.onDestroy();
		if (rock != null) rock.sWell = null;
	}
}


public class ScorchWheelMoveProj : Projectile {
	public Rock? rock;
    //public int maxSpeed = 200;
	public ScorchWheelMoveProj(
        Weapon weapon, Point pos, int xDir, 
        Player player, ushort netProjId, 
        bool rpc = false
    ) : base (
        weapon, pos, xDir, 240, 1, 
        player, "scorch_wheel_grounded_proj", 0, 1, 
        netProjId, player.ownedByLocalPlayer
    ) {
		projId = (int)RockProjIds.ScorchWheelMove;
        useGravity = true;
        maxTime = 1.25f;
        //destroyOnHit = false;
        canBeLocal = false;

        if (rpc) {
            rpcCreate(pos, player, netProjId, xDir);
        }
	}

    public static Projectile rpcInvoke(ProjParameters arg) {
        return new ScorchWheelMoveProj(
            ScorchWheel.netWeapon, arg.pos, arg.xDir, arg.player,
            arg.netId
        );
    }

    public override void update() {
		base.update();
        if (!ownedByLocalPlayer) return;
        
		if (collider != null) {
			collider.isTrigger = false;
			collider.wallOnly = true;
		}
        checkUnderwater();
    }

    public override void onHitWall(CollideData other) {
		if (!ownedByLocalPlayer) return;

		var normal = other.hitData.normal ?? new Point(0, -1);
        
		if (normal.isSideways()) {
            destroySelf();
			//vel.x *= -1;
			//incPos(new Point(5 * MathF.Sign(vel.x), 0));
		}
	}

    public void checkUnderwater() {
		if (isUnderwater()) {
			new BubbleAnim(pos, "bubbles") { vel = new Point(0, -60) };
			Global.level.delayedActions.Add(new DelayedAction(() => { new BubbleAnim(pos, "bubbles_small") { vel = new Point(0, -60) }; }, 0.1f));
            
            if (rock == null || rock.charState is Die || (rock.player.weapon is not JunkShield)) {
			    destroySelf();
			    return;
		    }
	    }
    }
}


public class UnderwaterScorchWheelProj : Projectile {

    int bubblesAmount;
    int counter = 0;
    int bubblesSmallAmount;
    int counterSmall = 0;
    Rock? rock;

    public UnderwaterScorchWheelProj(
        Weapon weapon, Point pos, int xDir, 
        Player player, ushort netProjId,
        bool rpc = false
    ) : base (
        weapon, pos, xDir, 0, 2, 
        player, "empty", 0, 1, 
        netProjId, player.ownedByLocalPlayer
    ) {
        maxTime = 1.5f;
        destroyOnHit = false;
        rock = player.character as Rock;
        if (rock != null) rock.underwaterScorchWheel = this;
        
        bubblesAmount = Helpers.randomRange(2, 8);
        bubblesSmallAmount = Helpers.randomRange(2, 8);

        if (rpc) rpcCreate(pos, player, netProjId, xDir);
    }

    public static Projectile rpcInvoke(ProjParameters arg) {
        return new UnderwaterScorchWheelProj(
            ScorchWheel.netWeapon, arg.pos, arg.xDir, arg.player,
            arg.netId
        );
    }

    public override void update() {
        base.update();
        if (!ownedByLocalPlayer) return;

        Point centerPos = rock.getCenterPos();

        if (counter < bubblesAmount) {
            int xOffset = Helpers.randomRange(-12, 12);
            int yOffset = Helpers.randomRange(-12, 12);
            centerPos = rock.getCenterPos();
            Global.level.delayedActions.Add(new DelayedAction( () => { 
                new BubbleAnim(new Point(centerPos.x + xOffset, centerPos.y + yOffset), "bubbles") { vel = new Point(0, -60)}; },
                0.1f * counter));
            counter++;
        }

        if (counterSmall < bubblesSmallAmount) {
            int xOffset = Helpers.randomRange(-12, 12);
            int yOffset = Helpers.randomRange(-12, 12);
            centerPos = rock.getCenterPos();
            Global.level.delayedActions.Add(new DelayedAction( () => { 
                new BubbleAnim(new Point(centerPos.x + xOffset, centerPos.y + yOffset), "bubbles_small") { vel = new Point(0, -60)}; },
                0.1f * counterSmall));
            counterSmall++;
        }
    }
}

public class Burning : CharState {
	public float burningTime = 2;
    public const int maxStacks = 5;
    float burnDamageCooldown;
	public Burning() : base("burning") {
        //invincible = true;
	}

	public override bool canEnter(Character character) {
		if (!base.canEnter(character) ||
			character.burnInvulnTime > 0 ||
			character.isInvulnerable() ||
			character.charState.stunResistant ||
			character.grabInvulnTime > 0 ||
			character.charState.invincible
		) {
			return false;
		}
		return true;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		if (!character.ownedByLocalPlayer) return;
		if (character.vel.y < 0) character.vel.y = 0;
        character.useGravity = false;
        character.stopMoving();
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		if (!character.ownedByLocalPlayer) return;
		character.burnInvulnTime = 2;
        character.burnStateStacks = 0;
        character.useGravity = true;
	}

	public override void update() {
		base.update();
		if (!character.ownedByLocalPlayer) return;

        /*if (burnDamageCooldown > 0) burnDamageCooldown -= Global.spf;
        if (burnDamageCooldown <= 0) {
            character.applyDamage(player, (int)RockWeaponIds.ScorchWheel, 1, (int)RockProjIds.ScorchWheel);
            Global.playSound("hurt");
            burnDamageCooldown = Global.spf * 45;
        }*/

		burningTime -= Global.spf;
		if (burningTime <= 0) {
			burningTime = 0;
			character.changeToIdleOrFall();
		}
	}
}
