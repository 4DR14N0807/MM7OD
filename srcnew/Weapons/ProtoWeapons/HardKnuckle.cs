using System;

namespace MMXOnline;

public class HardKnuckle : Weapon {
	public static HardKnuckle netWeapon = new();

    public HardKnuckle() : base() {
        index = (int)RockWeaponIds.HardKnuckle;
        fireRateFrames = 75;
		hasCustomAnim = true;
    }

	public override float getAmmoUsage(int chargeLevel) {
		return 2;
	}

    public override void shoot(Character character, params int[] args) {
	    base.shoot(character, args);
        Point shootPos = character.getShootPos();
        int xDir = character.getShootXDir();
        character.changeState(new HardKnuckleShoot(), true);
		character.playSound("super_adaptor_punch", sendRpc: true);
	}
}

public class HardKnuckleProj : Projectile {
    bool changedDir;
    Player player;

    public HardKnuckleProj(
		Point pos, int xDir, Player player, ushort? netId, bool rpc = false
	) : base(
		HardKnuckle.netWeapon, pos, xDir, 200, 2, player, "hard_knuckle_proj",
		Global.halfFlinch, 0, netId, player.ownedByLocalPlayer
	) {
        maxTime = 1f;
        projId = (int)RockProjIds.HardKnuckle;
        this.player =  player;
		fadeSprite = "generic_explosion";
		fadeOnAutoDestroy = true;
    }

    public override void update() {
        base.update();
		if (!ownedByLocalPlayer) {
			return;
		}
		int inputYDir = player.input.getYDir(player);
		vel.y = 100f * inputYDir;
		if (inputYDir != 0) {
			forceNetUpdateNextFrame = true;
		}
    }

	public override void onStart() {
		base.onStart();
	}
}

public class HardKnuckleShoot : CharState {
	bool fired;

	public HardKnuckleShoot() : base("shoot", "", "","") {
		airMove = true;
		landSprite = "shoot";
		airSprite = "shootAir";
		useGravity = false;
	}
	public override void update() {
        base.update();

        if (!fired) {
            fired = true;
            new HardKnuckleProj(
				character.getCenterPos(), character.xDir,
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
        character.useGravity = false;
	}
}
