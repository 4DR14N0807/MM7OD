using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline;

public class JunkShield : Weapon {

	public static JunkShield netWeapon = new JunkShield();
	public JunkShield() : base() {
		shootSounds = new List<string>() { "", "", "", "" };
		rateOfFire = 1f;
		index = (int)RockWeaponIds.JunkShield;
		weaponBarBaseIndex = (int)RockWeaponBarIds.JunkShield;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = (int)RockWeaponSlotIds.JunkShield;
		killFeedIndex = 0;
		maxAmmo = 7;
		ammo = maxAmmo;
		description = new string[] {"Defenseless barrier that gets","damaged after rough use.", "Can be fired in up to 3 directions."};
	}

	public override bool canShoot(int chargeLevel, Player player) {
		Rock? rock = player.character as Rock;

		return rock?.junkShield == null && base.canShoot(chargeLevel, player);
	}


	public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId) {
		base.getProjectile(pos, xDir, player, chargeLevel, netProjId);
		player.setNextActorNetId(netProjId);

		if (player.character.ownedByLocalPlayer) {
			/*if (chargeLevel >= 2 && player.hasBusterLoadout()) {
            player.character.changeState(new RockChargeShotState(player.character.grounded), true);
        	} else*/ 
			if (player.character.charState is LadderClimb) {
                player.character.changeState(new ShootAltLadder(this, (int)chargeLevel), true);
            } else {
                player.character.changeState(new ShootAlt(this, (int)chargeLevel), true);
            }	
		}					 
	}
}


public class JunkShieldProj : Projectile {
	public int HP = 6;
	public float healthDecCooldown;
	Player player;
	Rock? rock;
	Point centerPos;
	public List<Sprite> mainProjs = new List<Sprite>();
	int mainProjsCount = 3;
	public List<Sprite> otherProjs = new List<Sprite>();
	int otherProjsCount = 6;
	float projAngle;
	float radius = 30;
	public List<int> randomPieces = new List<int>();
	LoopingSound sound;

	public JunkShieldProj(Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false) :
	base(weapon, pos, xDir, 0, 1, player, "junk_shield_proj", 0, 0.25f, netProjId, player.ownedByLocalPlayer ) {
		
		projId = (int)RockProjIds.JunkShield;
		destroyOnHit = false;
		rock = player.character as Rock;
		if (rock != null) rock.junkShield = this;
		sound = new LoopingSound("charge_start", "charge_loop", this);
		this.player = player;
		canBeLocal = false;

		for (var i = 0; i < mainProjsCount; i++) {
			var mainProjSprite = Global.sprites["junk_shield_pieces"].clone();
			mainProjSprite.frameIndex = 5;
			mainProjSprite.frameSpeed = 0;
			mainProjs.Add(mainProjSprite);
		}

		for (var i = 0; i < otherProjsCount; i++) {
			var otherProjSprite = Global.sprites["junk_shield_pieces"].clone();
			randomPieces.Add(Helpers.randomRange(0,4));
			otherProjSprite.frameSpeed = 0;
			otherProjs.Add(otherProjSprite);
		}

		if (rpc) rpcCreate(pos, player, netProjId, xDir);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
        return new JunkShieldProj(
            JunkShield.netWeapon, arg.pos, arg.xDir, arg.player,
            arg.netId
        );
    }

	public override void update(){
		base.update();

		if (projAngle >= 256) projAngle = 0;
		projAngle += 5;

		if (rock != null) {
			xDir = rock.getShootXDir();
			pos = rock.getCenterPos();
		}

		if (sound != null) sound.play();

		if (healthDecCooldown > 0) {
			healthDecCooldown += Global.spf;
			if (healthDecCooldown > damager.hitCooldown) healthDecCooldown = 0;
		}

		if (HP <= 0 ||rock == null || rock.charState is Die || (rock.player.weapon is not JunkShield)) {
			destroySelfNoEffect();
			return;
		}

		if (time >= Global.spf * 15 && player.input.isPressed(Control.Shoot, player)) {
			shootProjs();
			rock.shootTime = 1f;
		} 
	}

	public override void render(float x, float y){
		base.render(x, y);
		if (rock != null) centerPos = rock.getCenterPos();
		float hpCount = HP;
		float extra = HP % 2;
		mainProjsCount = (int)(hpCount + extra) / 2;
		
		//main pieces render
		for (var i = 0; i < mainProjsCount; i++) {
			float extraAngle = projAngle + i*85;
			if (extraAngle >= 256) extraAngle -= 256;
			float xPlus = Helpers.cosd(extraAngle * 1.40625f) * radius;
			float yPlus = Helpers.sind(extraAngle * 1.40625f) * radius;
			if (rock != null) xDir = rock.getShootXDir();
			mainProjs[i].draw(5, centerPos.x + xPlus, centerPos.y + yPlus, xDir, yDir, getRenderEffectSet(), 1, 1, 1, zIndex);
		}

		//small pieces render
		for (var i = 0; i < HP; i++) {
			float extraAngle = (projAngle + i*42.5f) - 10;
			if (extraAngle >= 256) extraAngle -= 256;
			float xPlus = Helpers.cosd(extraAngle * 1.40625f) * radius;
			float yPlus = Helpers.sind(extraAngle * 1.40625f) * radius;
			if (rock != null) xDir = rock.getShootXDir();
			otherProjs[i].draw(randomPieces[i], centerPos.x + xPlus, centerPos.y + yPlus, xDir, yDir, getRenderEffectSet(), 1, 1, 1, zIndex);
		}
	}

	
	public override void onHitDamagable(IDamagable damagable) {
		if (rock != null) base.onHitDamagable(rock);
		decHealth(1);
	}


	public void decHealth(float amount = 1) {
		if (healthDecCooldown == 0) {
			healthDecCooldown = Global.spf;
			HP--;
		}
	}

	public override void onDestroy(){
		base.onDestroy();
		sound.stop();
		if (rock != null) rock.junkShield = null;
	}

	public void shootProjs() {
		int hpCount = HP;
		int extra = HP % 2;
		int actualCount = (hpCount + extra) / 2;
		destroySelfNoEffect();

		for (var i = 0; i < actualCount; i++) {
			var angleToShoot = (int)projAngle + (85*i);
			if (angleToShoot >= 256) angleToShoot -= 256;
			//float x = 180 * Helpers.cosd(angleToShoot * 1.40625f);
			//float y = 180 * Helpers.sind(angleToShoot * 1.40625f);
			if (rock != null) new JunkShieldShootProj(weapon, rock.getCenterPos(), rock.getShootXDir(), damager.owner, angleToShoot, damager.owner.getNextActorNetId(true), true);
		}
		Global.playSound("thunder_bolt");
	}
}


public class JunkShieldShootProj : Projectile {
	
	int shootAngle;
	const int projSpeed = 180;
	Rock? rock;

	public JunkShieldShootProj(Weapon weapon, Point pos, int xDir, Player player, int angle, ushort netProjId, bool rpc = false) :
	base(weapon, pos, xDir, 0, 2, player, "junk_shield_shoot_proj", 0, 0.5f, netProjId, player.ownedByLocalPlayer) {
		projId = (int)RockProjIds.JunkShieldPiece;
		rock = player.character as Rock;
		maxTime = 0.75f;
		shootAngle = angle;
		rock = player.character as Rock;
		if (rock != null) base.xDir = rock.getShootXDir();
		canBeLocal = false;
		fadeOnAutoDestroy = true;

		if (base.xDir < 0) {
			int angleFix;
			if (shootAngle <= 128) angleFix = 128 - shootAngle;
			else angleFix = (256 - shootAngle) + 128;
			
			shootAngle = angleFix;
		}

		frameIndex = (int)shootAngle / 16;
		frameSpeed = 0;

		base.vel.x = projSpeed * Helpers.cosd(angle * 1.40625f);
		base.vel.y = projSpeed * Helpers.sind(angle * 1.40625f);

		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)angle };

            rpcCreate(pos, player, netProjId, xDir, extraArgs);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
        return new JunkShieldShootProj(
            JunkShield.netWeapon, arg.pos, arg.xDir, arg.player,
            arg.extraData[0], arg.netId
        );
    }

	public override void update(){
		base.update();
		if (!ownedByLocalPlayer) return;
	}
}


public class JunkShieldState : CharState {
	bool fired;
	public JunkShieldState(bool grounded) : base("shoot2", "", "","") {
		airMove = true;
	}

	public override void update() {
        base.update();

        if (!fired && character.frameIndex == 2) {
            
            fired = true;
            new JunkShieldProj(new JunkShield(), character.getCenterPos(), character.xDir, player, player.getNextActorNetId(), true);
        } 

        if (character.isAnimOver()) {
			if (character.grounded) character.changeState(new Idle(), true);
			else character.changeState(new Fall(), true);
		}
    }

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
        bool air = !character.grounded || character.vel.y < 0;
        sprite = "shoot2";
        defaultSprite = sprite;
        landSprite = "shoot2";
        if (air) {
			sprite = "shoot2_air";
			defaultSprite = sprite;
		}
        character.changeSpriteFromName(sprite, true);
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
	}
}