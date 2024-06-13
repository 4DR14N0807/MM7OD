using System;
using System.Collections.Generic;
using System.Linq;


namespace MMXOnline;


public class ProtoMan : Character {

    public List<ProtoBusterProj> protoLemonsOnField = new();
    public float lemonCooldown;
	public const int coreMaxAmmo = 28;
	public int coreAmmo;
	public const float coreAmmoMaxCooldown = 30f / 60f;
	public float coreAmmoIncreaseCooldown;
	public float coreAmmoDecreaseCooldown = coreAmmoMaxCooldown;
   
   public ProtoMan(
    Player player, float x, float y, int xDir,
	bool isVisible, ushort? netId, bool ownedByLocalPlayer,
	bool isWarpIn = true
    ) : base(
    player, x, y, xDir, isVisible, netId, ownedByLocalPlayer, isWarpIn, false, false
    ) {
        charId = CharIds.ProtoMan;
    }

    public override bool canTurn() {
		if (charState is ProtoAirShoot) return false;
		return base.canTurn();
	}
	
	public override bool canDash() {
        return false;
    }

    public override bool canAirDash() {
        return false;
    }

	public override bool canWallClimb() {
		return false;
	}

	public override bool canCrouch() {
		return false;
	}

	public override bool canCharge() {
		if (charState is ProtoStrike) return false;
		return base.canCharge();
	}

    public bool canBlock() {
        return true;
    }

	public bool canShieldDash() {
		if (
			charState is ShieldDash ||
			!grounded
		) return false;

		return true;
	}

    public override string getSprite(string spriteName) {
        return "protoman_" + spriteName;
    }

    /*public override Collider getBlockCollider() {
		Rect rect = Rect.createFromWH(0, 0, 23, 55);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(6, 0));
	}*/

    public override Projectile? getProjFromHitbox(Collider collider, Point centerPoint) {
		Projectile? proj = sprite.name switch {
            "protoman_block" when collider.isHurtBox() => new GenericMeleeProj(
				new Weapon(), centerPoint, ProjIds.ShieldBlock, player, 0, 0, 0, isShield: true
			),
			
			_ => null
        };

        return proj;
	}

    public override bool chargeButtonHeld() {
		return player.input.isHeld(Control.Shoot, player);
	}
	public override void increaseCharge() {
		float factor = 0.75f;
		chargeTime += Global.spf * factor;
	}


    public override void update() {
        base.update();

        if (!ownedByLocalPlayer) return;

        Helpers.decrementTime(ref lemonCooldown);
		
		
		if (isCharging()) {
			coreAmmoIncreaseCooldown += Global.spf;
			coreAmmoDecreaseCooldown = Global.spf * 15;
		} 
		else {
			Helpers.decrementTime(ref coreAmmoDecreaseCooldown);
			Helpers.decrementTime(ref coreAmmoIncreaseCooldown);
		} 

		if (coreAmmoIncreaseCooldown >= Global.spf * 15) {
			if (coreAmmo < coreMaxAmmo) coreAmmo++;
			coreAmmoIncreaseCooldown = 0;
		}

		if (coreAmmoDecreaseCooldown <= 0) {
			if (coreAmmo > 0) coreAmmo--;
			coreAmmoDecreaseCooldown = Global.spf * 15;
		}

        if (shootAnimTime > 0) {
			shootAnimTime -= Global.spf;
			if (shootAnimTime <= 0) {
				shootAnimTime = 0;
				changeSpriteFromName(charState.defaultSprite, false);
			}
		}
       
        chargeLogic(shoot);

		if (isCharging() && grounded && charState is ProtoBlock) changeState(new ProtoCharging(), true);


    }

	public override void onFlinchOrStun(CharState newState) {
			if (newState is Hurt) addCoreAmmo(3);
			base.onFlinchOrStun(newState);
		}


    public override bool normalCtrl() {
        bool isGuarding = player.input.isHeld(Control.Down, player);

        if (isGuarding && canBlock()) {
            changeState(new ProtoBlock(), true);
        }

		if (player.dashPressed(out string slideControl) && canShieldDash() ) {
			changeState(new ShieldDash(slideControl), true);
		}

        return base.normalCtrl();
    }

    public override bool attackCtrl() {
		bool shootPressed = player.input.isPressed(Control.Shoot, player);
		bool specialPressed = player.input.isPressed(Control.Special1, player);
		if (specialPressed) {
			if (!grounded) {
				changeState(new ProtoAirShoot(), true);
				return true;
			}
		}
		if (!isCharging()) {
			if (shootPressed) {
				lastShootPressed = Global.frameCount;
			}
			int framesSinceLastShootPressed = Global.frameCount - lastShootPressed;
			if (shootPressed || framesSinceLastShootPressed < 6) {
				if (lemonCooldown <= 0) {
					shoot(0);
					return true;
				}
			}
		}
		return base.attackCtrl();
	}

    public void shoot(int chargeLevel) {
		if (chargeLevel == 0) {
			for (int i = protoLemonsOnField.Count - 1; i >= 0; i--) {
				if (protoLemonsOnField[i].destroyed) {
					protoLemonsOnField.RemoveAt(i);
				}
			}
			if (protoLemonsOnField.Count >= 3) { return; }
		}
		string shootSprite = getSprite(charState.shootSprite);
		if (!Global.sprites.ContainsKey(shootSprite)) {
			if (grounded) { shootSprite = "protoman_shoot"; } else { shootSprite = "protoman_jump_shoot"; }
		}
		if (shootAnimTime == 0) {
			changeSprite(shootSprite, false);
		} else if (charState is Idle) {
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
		Point shootPos = getShootPos();
		int xDir = getShootXDir();

		if (chargeLevel == 0 || chargeLevel == 1) {
			var lemon = new ProtoBusterProj( 
				shootPos, xDir, player, player.getNextActorNetId(), rpc: true
			);
			protoLemonsOnField.Add(lemon);
			resetCoreCooldown();
			lemonCooldown = 0.3f;
		} /*else if (chargeLevel == 1) {
			new DZBuster2Proj(
				shootPos, xDir, player, player.getNextActorNetId(), rpc: true
			);
			lemonCooldown = 22f / 60f;
		}*/else if (chargeLevel == 2) {
			if (player.input.isHeld(Control.Up, player)) changeState(new ProtoStrike(), true);
			else {
				new ProtoBusterChargedProj(
					shootPos, xDir, player, player.getNextActorNetId(), rpc: true
				);
				addCoreAmmo(-2);
				lemonCooldown = 0.3f;
			}
		}
		/*if (chargeLevel >= 1) {
			stopCharge();
		}*/
	}

	public void addCoreAmmo(int amount) {
		coreAmmo += amount;
		if (coreAmmo > coreMaxAmmo) coreAmmo = coreMaxAmmo;
		if (coreAmmo < 0) coreAmmo = 0;
		resetCoreCooldown();
	}

	public void resetCoreCooldown() {
		coreAmmoIncreaseCooldown = 0;
		coreAmmoDecreaseCooldown = coreAmmoMaxCooldown;
	}
}