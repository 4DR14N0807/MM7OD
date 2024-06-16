using System;
using System.Collections.Generic;

namespace MMXOnline;


public class ProtoBlock : CharState {

    ProtoMan? protoman;

    public ProtoBlock() : base("block") {
        immuneToWind = true;
        exitOnAirborne = true;
        //normalCtrl = true;
        //attackCtrl = true;
    }

    public override void onEnter(CharState oldState) {
        base.onEnter(oldState);
        protoman = character as ProtoMan;
        protoman.isShieldActive = true;
    }

    public override void onExit(CharState newState) {
        base.onExit(newState);
        protoman.isShieldActive = false;
    }


    public override void update() {
        base.update();

        var move = new Point(0, 0);
		float moveSpeed = character.getRunSpeed();

       if (player.input.isHeld(Control.Left, player)) {
			character.xDir = -1;
			if (player.character.canMove()) move.x = -moveSpeed;
		} else if (player.input.isHeld(Control.Right, player)) {
			character.xDir = 1;
			if (player.character.canMove()) move.x = moveSpeed;
		}
		if (move.magnitude > 0 && player.input.isLeftOrRightHeld(player)) {
			character.move(move);
		}

        bool isGuarding = player.input.isHeld(Control.Down, player);

        if (!isGuarding) {
            character.changeState(new Idle(), true);
            return;
        }
    }
}


public class ShieldDash : CharState {

    string initialSlideButton;
    int initialXDir;
    float dustTimer;

    public ShieldDash(string initialSlideButton) : base("shield_dash") {
        this.initialSlideButton = initialSlideButton;
        accuracy = 10;
    }

    public override void onEnter(CharState oldState) {
        base.onEnter(oldState);
		initialXDir = character.xDir;
		character.isDashing = true;
    }


    public override void update() {
        base.update();

        character.vel.y = 0;

        if (stateTime > Global.spf * 40) {
			character.frameIndex = 0;
			character.sprite.frameTime = 0;
			character.sprite.animTime = 0;
			character.sprite.frameSpeed = 0.1f;
			
			character.changeState(new Idle(), true);
			return;
		}

		var move = new Point(0, 0);
		move.x = character.getDashSpeed() * initialXDir;
		if (character.frameIndex >= 2) character.move(move);

        dustTimer += Global.spf;

        if (dustTimer >= Global.spf * 4) {
            new Anim(
				character.getDashDustEffectPos(initialXDir),
				"dust", initialXDir, player.getNextActorNetId(), true,
				sendRpc: true
			) { vel = new Point (0, -40) };
            dustTimer = 0;
        }
    }
}


public class ProtoAirShoot : CharState {

    int shotAngle = 90;
    int shotLastFrame = 10;
    ProtoMan? protoman;
    public ProtoAirShoot() : base("jump_shoot2") {
        airMove = true;
        attackCtrl = false;
    }

    public override void onEnter(CharState oldState) {
        base.onEnter(oldState);
        protoman = character as ProtoMan;
    }


    public override void update() {
        base.update();

        if (character.frameIndex != shotLastFrame) {

            float lemonSpeedX = ProtoBusterProj.projSpeed * Helpers.cosd(shotAngle) * character.xDir;
            float lemonSpeedY = ProtoBusterProj.projSpeed * Helpers.sind(shotAngle);
            Point lemonSpeed = new Point(lemonSpeedX, lemonSpeedY);

            new ProtoBusterProj(character.getShootPos(), character.getShootXDir(), player, player.getNextActorNetId(), lemonSpeed, true);
            protoman.resetCoreCooldown();

            shotAngle -= 18;
            shotLastFrame = character.frameIndex;
        }

        if (character.grounded) character.changeState(new Idle(), true);
    }
}


public class ProtoCharging : CharState {

    int chargeLvl;
    bool isCharging;

    public ProtoCharging() : base("charge") {

    }

    public override void update() {
        base.update();

        chargeLvl = character.getChargeLevel();
        isCharging = player.input.isHeld(Control.Shoot, player);

        if (!isCharging && chargeLvl >= 2) character.changeState(new ProtoChargeShotState(), true);
    }
}


public class ProtoChargeShotState : CharState {

    bool fired;
    ProtoMan protoman;

    public ProtoChargeShotState() : base("chargeshot") {
        
    }

    public override void onEnter(CharState oldState) {
        base.onEnter(oldState);
        protoman = character as ProtoMan;
    }


    public override void update() {
        base.update();

        if (!fired && character.frameIndex == 3) {
            
            new ProtoBusterChargedProj(character.getShootPos(), character.getShootXDir(), player, player.getNextActorNetId(), true);
            fired = true;
            protoman.addCoreAmmo(-2);
            character.stopCharge();
        }

        if (character.isAnimOver()) character.changeState(new Idle(), true);
    }
}


public class ProtoStrike : CharState {

    ProtoMan protoman;
    bool isUsingPStrike;
    bool shot;
    float coreCooldown;
    GenericMeleeProj pStrike;
    bool didUseAmmo;

    public ProtoStrike() : base("protostrike") {

    }


    public override void onEnter(CharState oldState) {
        base.onEnter(oldState);
        protoman = character as ProtoMan;
    }


    public override void update() {
        base.update();

        bool isShooting = player.input.isHeld(Control.Shoot, player);

        if (character.isAnimOver()) {
            if (isShooting) {
                character.frameIndex = 3;
            }
            else character.changeState(new ProtoStrikeEnd(), true);
            shot = false;
            if (pStrike != null) pStrike.destroySelf();
        }

        if (character.frameIndex >= 3) isUsingPStrike = true;
        else isUsingPStrike = false;

        if (isUsingPStrike) {
            if (!shot) {
                var shootPos = character.getShootPos();
                pStrike = new GenericMeleeProj(new Weapon(), shootPos, 0, player, 3, Global.halfFlinch, 1f);
                if (!didUseAmmo) {
                    protoman.addCoreAmmo(-3);
                    didUseAmmo = true;
                }
                var rect = new Rect(0, 0, 32, 24);
                pStrike.globalCollider = new Collider(rect.getPoints(), false, pStrike, false, false, 0, new Point());
                shot = true;
            }

            coreCooldown += Global.spf;
            
        }

        if (coreCooldown >= Global.spf * 15) {
            coreCooldown = 0;
            protoman.addCoreAmmo(1);
        }
    }
}


public class ProtoStrikeEnd : CharState {
    public ProtoStrikeEnd() : base("protostrike_end") {

    }

    public override void update() {
        base.update();

        if (character.isAnimOver()) character.changeState(new Idle(), true);
    }
}