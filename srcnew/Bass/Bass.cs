using System;
using System.Collections.Generic;

namespace MMXOnline;


public class Bass : Character {

	float weaponCooldown;
	int shootAngle;
	string angleSprite;
	public CopyVisionClone cVclone;
	public SpreadDrillProj sDrill;
	public SpreadDrillMediumProj sDrillM;
	public Weapon specialWeapon;
	public List<Weapon> weaponsList = new();
	int weaponCursor;

    public Bass(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer,
		bool isWarpIn = true
	) : base(
		player, x, y, xDir, isVisible, netId, ownedByLocalPlayer, isWarpIn, false, false
	) {
        charId = CharIds.Bass;
		weaponsList = getAllWeapons();
		specialWeapon = weaponsList[0];
    }

    public override bool canCrouch() {
        return false;
    }

	public override bool canMove() {
		if (shootAnimTime > 0 && grounded) return false;

		return base.canMove();
	}

	public override bool canShoot() {
		if (weaponCooldown > 0 ||
			charState is Dash)
		return false;
		
		return base.canShoot();
	}

	public override bool canAirJump() {
		return dashedInAir == 0;
	}
	public override bool canAirDash() {
		return false;
	}

	public override bool canWallClimb() {
		return false;
	}
	
	public override bool canChangeWeapons() {
		return base.canChangeWeapons();
	}

	public override void update() {
		base.update();

		Helpers.decrementFrames(ref weaponCooldown);

		player.changeWeaponControls();

		// For the shooting animation.
		if (shootAnimTime > 0) {
			shootAnimTime -= Global.spf;
		}

		if (shootAnimTime <= 0) {
				shootAnimTime = 0;
				if (sprite.name.EndsWith("_shoot")) {
					changeSpriteFromName(charState.defaultSprite, false);
					if (charState is WallSlide) {
						frameIndex = sprite.frames.Count - 1;
					}
				}
			}
	}

    public override string getSprite(string spriteName) {
		return "bass_" + spriteName;
	}

	public override bool attackCtrl() {
		bool shootHeld = player.input.isHeld(Control.Shoot, player);
		bool shootPressed = player.input.isPressed(Control.Shoot, player);
		bool specialPressed = player.input.isPressed(Control.Special1, player);
		bool downHeld = player.input.isHeld(Control.Down, player);

		var shootCommand = player.weapon is BassBuster ? shootHeld : shootPressed;

		if (!isCharging()) {
			if (shootCommand) {
				lastShootPressed = Global.frameCount;
			}
			int framesSinceLastShootPressed = Global.frameCount - lastShootPressed;
			if (shootCommand || framesSinceLastShootPressed < 6) {
				if (weaponCooldown <= 0) {
					shoot(0);
					return true;
				}
			}
		}
		return base.attackCtrl();
	}

	public void setShootAnim() {
		changeSprite(getSprite(getShootSprite(grounded)), true);
		string shootSprite = getSprite(charState.shootSprite);
		if (!Global.sprites.ContainsKey(shootSprite)) {
			
			if (grounded) {
				//shootSprite = getSprite("shoot");
				shootSprite = getSprite(getShootSprite(true));
			} else {
				//shootSprite = getSprite("jump_shoot");
				shootSprite = getSprite(getShootSprite(false));
			}
		}
		/*if (shootAnimTime == 0) {
			changeSprite(shootSprite, true);
		} else if (charState is Idle) {
			frameIndex = 0;
			frameTime = 0;
		}*/

		//changeSprite(shootSprite, false);

		if (charState is Idle) {
			frameIndex = 0;
			frameTime = 0;
		}

		if (charState is LadderClimb) {
			if (player.input.isHeld(Control.Left, player)) {
				this.xDir = -1;
			} else if (player.input.isHeld(Control.Right, player)) {
				this.xDir = 1;
			}
		}
		shootAnimTime = 0.3f;
	}

	public void shoot(int chargeLevel) {
		if (!canShoot() || !player.weapon.canShoot(0, player)) return;
		//int lemonNum = -1;
		// Cancel non-invincible states.
		if (!charState.attackCtrl && !charState.invincible) {
			changeToIdleOrFall();
		}
		// Shoot anim and vars.
		float oldShootAnimTime = shootAnimTime;
		setShootAnim();
		Point shootPos = getShootPos();
		int xDir = getShootXDir();
		

		player.weapon.shoot(this, 0);
		if (oldShootAnimTime <= 0.25f) {
			shootAnimTime = 0.25f;
		}
		weaponCooldown = player.weapon.fireRateFrames;
	}

	public int getShootAngle() {
		int dirY = (player.input.getYDir(player) * 2);
		int dirX = (int)MathF.Abs(player.input.getXDir(player)) * xDir * (-dirY / 2);
		int dirXAlt = xDir > 0 ? 0 : 4;
		int dir = dirY != 0 ? dirY + dirX : dirXAlt;
		dir *= 32;
		if (dir == 64) dir -= 32 * xDir;
		return dir;
	}

	public string getShootSprite(bool grounded) {
		string name;
		int dir = 0;

		if (player.input.isHeld(Control.Down, player)) dir += 2;
		else if (player.input.isHeld(Control.Up, player)) {
			dir -= 2;
			if (player.input.getXDir(player) != 0) dir += 1;
		} 


		switch(dir) {
			case -1: name = "shoot_up_diag"; break;
			case -2: name = "shoot_up"; break;	
			case 2: name = "shoot_down_diag"; break;
			default: name = "shoot"; break;
		}

		if (!grounded) name = "jump_" + name;
		return name;
	}

	public static List<Weapon> getAllWeapons() {
		return new List<Weapon>() 
		{
				new BassBuster(),
				new IceWall(),
				new CopyVision(),
				new SpreadDrill(),
				new WaveBurner(),
				new RemoteMine(),
				new LightingBolt(),
				new TenguBlade(),
				new MagicCard(),
				
		};
	}
}

