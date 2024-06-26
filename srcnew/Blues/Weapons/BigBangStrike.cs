using System;
using System.Collections.Generic;
using SFML.Graphics;

namespace MMXOnline;

public class BigBangStrikeProj : Projectile {

    Anim? trail1;
    int trail1Time;
    Anim? trail2;
    int trail2Time;
    Player player;

    public BigBangStrikeProj(Point pos, int xDir, Player player, ushort? netId, bool rpc = false) :
    base(ProtoBuster.netWeapon, pos, xDir, 240, 6, player, "big_bang_strike_proj", Global.defFlinch, 3, netId, player.ownedByLocalPlayer) {
        projId = (int)BluesProjIds.BigBangStrike;
        maxTime = 0.75f;
        shouldShieldBlock = false;
        this.player = player;

        if (rpc) {
			rpcCreate(pos, player, netId, xDir);
		}
    }

    public static Projectile rpcInvoke(ProjParameters args) {
		return new BigBangStrikeProj(
			args.pos, args.xDir, args.player, args.netId
		);
	}


    public override void update() {
        base.update();

        trail1Time++;
        trail2Time++;

        //Yellow particles behaviour
        if (trail1Time >= 15) {
            trail1Time = 0;
            trail1 = new Anim(pos.addRand(6,12), "big_bang_strike_trail", xDir, player.getNextActorNetId(), true, true);
            trail1.useGravity = true;
            trail1.gravityModifier = -0.4f;
        }

        //Green particles behaviour
        if (trail2Time >= 6) {
            trail2Time = 0;
            trail2 = new Anim(pos.addRand(2,2), "big_bang_strike_trail2", xDir, player.getNextActorNetId(), true, true);
            trail2.useGravity = true;
            trail2.gravityModifier = -0.7f;
        }
    }

    public override void onDestroy() {
        base.onDestroy();

        new BigBangStrikeExplosionProj(pos, xDir, damager.owner, damager.owner.getNextActorNetId(true), true);
    }
}


public class BigBangStrikeExplosionProj : Projectile {

    float radius;

    public BigBangStrikeExplosionProj(Point pos, int xDir, Player player, ushort? netId, bool rpc = false) :
    base(ProtoBuster.netWeapon, pos, xDir, 0, 4, player, "empty", Global.halfFlinch, 2, netId, player.ownedByLocalPlayer) {
        projId = (int)BluesProjIds.BigBangStrikeExplosion;
        destroyOnHit = false;

        if (rpc) {
			rpcCreate(pos, player, netId, xDir);
		}

        projId = (int)BluesProjIds.BigBangStrike;
    }

    public static Projectile rpcInvoke(ProjParameters args) {
		return new BigBangStrikeExplosionProj(
			args.pos, args.xDir, args.player, args.netId
		);
	}


    public override void update() {
        base.update();

        if (radius <= 60) {
            radius += 2;
        } else destroySelf();

        foreach (var gameObject in Global.level.getGameObjectArray()) {
			if (gameObject is Actor actor &&
				actor.ownedByLocalPlayer &&
				gameObject is IDamagable damagable &&
				damagable.canBeDamaged(damager.owner.alliance, damager.owner.id, null) &&
				actor.pos.distanceTo(pos) <= radius
			) {
				damager.applyDamage(damagable, false, weapon, this, projId);
			}
		}
    }

    public override void render(float x, float y) {
		base.render(x, y);
		double transparency = (time) / (0.4);
		if (transparency < 0) { transparency = 0; }
		Color col1 = new(0, 0, 0, 64);
		Color col2 = new(255, 255, 255, (byte)(255.0 - 255.0 * (transparency)));
		DrawWrappers.DrawCircle(pos.x + x, pos.y + y, radius, filled: true, col1, 5f, zIndex - 10, isWorldPos: true);
	}
}


public class BigBangStrikeStart : CharState {

	Blues? blues;
    Anim? particle1;
    int particle1Time;
    Anim? particle2;
    int particle2Time;
	public BigBangStrikeStart() : base("charge") {

	}

	public override void onEnter(CharState oldState) {
		blues = character as Blues ?? throw new NullReferenceException();
		character.stopMoving();
        var centerPos = character.getCenterPos();

        if (character.ownedByLocalPlayer && player == Global.level.mainPlayer) {
			new BigBangStrikeBackwall(character.pos, character);
		}
	}

    public override void onExit(CharState newState) {
        base.onExit(newState);
        //if (particle1 != null) particle1.destroySelf();
        //if (particle2 != null) particle2.destroySelf();
    }


	public override void update() {
		base.update();

        /*particle1Time++;
        particle2Time++;

        if (particle1Time >= 10) {
            particle1Time = 0;
            particle1 = new Anim(character.getCenterPos().addRand(10, 7), "big_bang_strike_trail",
                character.xDir, player.getNextActorNetId(true), true, true);
            particle1.useGravity = true;
            particle1.gravityModifier = -0.5f;
        }

        if (particle2Time >= 3) {
            particle2Time = 0;
            particle2 = new Anim(character.getCenterPos().addRand(7, 10), "big_bang_strike_trail2",
                character.xDir, player.getNextActorNetId(true), true, true);
            particle2.useGravity = true;
            particle2.gravityModifier = -0.8f;
        }*/

		if (stateFrames >= 120) {
			character.changeState(new BigBangStrikeState(), true);
		}
	}
}


public class BigBangStrikeState : CharState {

	bool fired;
	public BigBangStrikeState() : base("chargeshot") {

	}


	public override void update() {
		base.update();

		if (!fired && character.frameIndex >= 3) {
			new BigBangStrikeProj(
				character.getShootPos(), character.getShootXDir(),
				player, player.getNextActorNetId(), true
			);
			fired = true;
			character.playSound("buster3", sendRpc: true);
		}
		if (stateFrames >= 60) {
			character.changeState(new OverheatStunned(), true);
		}
	}
}


public class BigBangStrikeBackwall : Effect {
	public Character rootChar;
    public int effectFrames;

	public BigBangStrikeBackwall(Point pos, Character character) : base(pos) {
		rootChar = character;
	}

	public override void update() {
		base.update();

        effectFrames++;
		if (effectFrames > 180) {
			destroySelf();
		}
	}

	public override void render(float offsetX, float offsetY) {
		float transparecy = 100;
		if (effectTime < 0.2) {
			transparecy = effectTime * 500f;
		}
		if (effectTime > 2.6) {
			transparecy = 100f - ((effectTime - 2.6f) * 500f);
		}

		DrawWrappers.DrawRect(
			Global.level.camX, Global.level.camY,
			Global.level.camX + 1000, Global.level.camY + 1000,
			true, new Color(0, 0, 0, (byte)System.MathF.Round(transparecy)), 1, ZIndex.Backwall
		);
	}
}