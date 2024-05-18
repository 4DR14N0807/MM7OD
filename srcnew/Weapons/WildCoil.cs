using System;
using System.Collections.Generic;

namespace MMXOnline;

public class WildCoil : Weapon {

    public static WildCoil netWeapon = new WildCoil();

    public WildCoil() : base() {
        index = (int)RockWeaponIds.WildCoil;
        weaponSlotIndex = (int)RockWeaponSlotIds.WildCoil;
        weaponBarBaseIndex = (int)RockWeaponBarIds.WildCoil;
        weaponBarIndex = weaponBarBaseIndex;
        //shootSounds = new List<string>() {"buster2", "buster2", "buster3", "buster3"};
        killFeedIndex = 0;
        maxAmmo = 28;
        ammo = maxAmmo;
        rateOfFire = 1.25f;
        description = new string[] {"Throws coils in both sides that","can be charged to reach more height.", "Use Up/Down to change bounce patterns."};
    }


	public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId) {

        if (player.character.ownedByLocalPlayer) {
            if (player.character.charState is LadderClimb) {
                player.character.changeState(new ShootAltLadder(this, (int)chargeLevel), true);
            } else {
                player.character.changeState(new ShootAlt(this, (int)chargeLevel), true);
            }
		}
	}

    public override float getAmmoUsage(int chargeLevel) {
        if (chargeLevel >= 2) return 4;
        return 1;
    }
}

public class WildCoilProj : Projectile {

    public int bounceSpeed  = 240;
    float soundCooldown;
    public WildCoilProj(
        Weapon weapon, Point pos, int xDir, 
        Player player, int type, ushort netProjId, 
        bool rpc = false
    ) : base (
        weapon, pos, xDir, 120, 2, 
        player, "wild_coil_start", 0, 0.5f, 
        netProjId, player.ownedByLocalPlayer
    ) {

        projId = (int)RockProjIds.WildCoil;
        useGravity = true;
        maxTime = 1.5f;
        fadeOnAutoDestroy = true;
        fadeSprite = "generic_explosion";
        canBeLocal = false;
        
        vel.y = -200;
        if (type == 0) vel.x = speed * xDir;
		else vel.x = -speed * xDir;

        if (rpc) {
            byte[] extraArgs = new byte[] { (byte)type };

            rpcCreate(pos, player, netProjId, xDir, extraArgs);
        }
    }

    public static Projectile rpcInvoke(ProjParameters arg) {
        return new WildCoilProj(
            WildCoil.netWeapon, arg.pos, arg.xDir, arg.player,
            arg.extraData[0], arg.netId
        );
    }

    public override void update() {
        base.update();

        if (!ownedByLocalPlayer) return;
        if (soundCooldown > 0) Helpers.decrementTime(ref soundCooldown);
    }

    
	public override void onHitWall(CollideData other) {
		if (!ownedByLocalPlayer) return;

		var normal = other.hitData.normal ?? new Point(0, -1);

        
		if (normal.isSideways()) {
            destroySelf();
			//vel.x *= -1;
			//incPos(new Point(5 * MathF.Sign(vel.x), 0));
		} else {
            changeSprite("wild_coil_jump", true);
            if (frameIndex > 0) frameIndex = 0;
            if (soundCooldown <= 0) {
                playSound("wild_coil_bounce", true, true);
                soundCooldown = 6f / 60f;
            }
			vel.y *= -1;
			if (vel.y < 0) {
                if (vel.y != -bounceSpeed) vel.y = -bounceSpeed;
            } else {
                if (vel.y != -bounceSpeed) vel.y = bounceSpeed;
            }

            incPos(new Point(0, 5 * MathF.Sign(vel.y)));
		}
	}
}


public class WildCoilChargedProj : Projectile {

    public int bounceSpeed = 330;
    public float bouncePower  = 1;
    public float animCooldown = 0.06f;
    float soundCooldown;
    int bounceReq = 1;
    int bounceCounter;
    int bounceBuff;

    public WildCoilChargedProj(
        Weapon weapon, Point pos, int xDir, 
        Player player, int type, ushort netProjId, 
        bool rpc = false
    ) : base (
        weapon, pos, xDir, 120, 2, 
        player, "wild_coil_charge_start", 0, 0.5f, 
        netProjId, player.ownedByLocalPlayer
    ) {

        projId = (int)RockProjIds.WildCoilCharged;
        maxTime = 2f;
        useGravity = true;
        fadeOnAutoDestroy = true;
        fadeSprite = "generic_explosion";
        canBeLocal = false;

        if (player.input.isHeld(Control.Up, player)) bounceSpeed = 480;
        else if (player.input.isHeld(Control.Down, player)) {
            bounceReq = 6;
            bounceSpeed = 30;
        } 
        else bouncePower = 1f;

        vel.y = -200;
        if (type == 0) vel.x = speed * xDir;
		else vel.x = -speed * xDir;

        if (rpc) {
            byte[] extraArgs = new byte[] { (byte)type };

            rpcCreate(pos, player, netProjId, xDir, extraArgs);
        }
    }

    public static Projectile rpcInvoke(ProjParameters arg) {
        return new WildCoilChargedProj(
            WildCoil.netWeapon, arg.pos, arg.xDir, arg.player,
            arg.extraData[0], arg.netId
        );
    }


    public override void update() {
        base.update();

        if (!ownedByLocalPlayer) return;
        if (animCooldown > 0) Helpers.decrementTime(ref animCooldown);
        if (soundCooldown > 0) Helpers.decrementTime(ref soundCooldown);

        bounceBuff = (int)bounceCounter / bounceReq;

        if (bounceBuff < 3) {
            switch (bounceBuff) {
                case 1: 
                damager.damage = 3;
                damager.flinch = 4;
                break;

                case 2:
                damager.damage = 3;
                damager.flinch = Global.halfFlinch;
                break;
            }
        }
    }

    
	public override void onHitWall(CollideData other) {
		if (!ownedByLocalPlayer) return;

		var normal = other.hitData.normal ?? new Point(0, -1);


		if (normal.isSideways()) {
            destroySelf();
			//vel.x *= -1;
			//incPos(new Point(5 * MathF.Sign(vel.x), 0));
		} else {
            changeSprite("wild_coil_charge_jump", true);
            if (soundCooldown <= 0) {
                playSound("wild_coil_bounce", true, true);
                soundCooldown = 6f / 60f;
            }
			vel.y *= -1;
            bounceCounter++;

            if (vel.y < 0) {
                if (vel.y != -bounceSpeed * bouncePower) vel.y = -bounceSpeed * bouncePower;
            } else {
                if (vel.y != -bounceSpeed * bouncePower) vel.y = bounceSpeed * bouncePower;
            }
			
			incPos(new Point(0, 5 * MathF.Sign(vel.y)));

            if (animCooldown <= 0 && frameIndex > 0) {
                frameIndex = 0;
                animCooldown = 0.06f;
            }
		}
	}
}
