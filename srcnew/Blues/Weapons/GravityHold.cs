using System;
using System.Collections.Generic;
using SFML.Graphics;

namespace MMXOnline;

public class GravityHold : Weapon {

	public static GravityHold netWeapon = new();

	public GravityHold() : base() {
		displayName = "GRAVITY HOLD";
		descriptionV2 = "";
		defaultAmmoUse = 4;

		index = (int)RockWeaponIds.GravityHold;
		fireRateFrames = 60;
		hasCustomAnim = true;
	}

	public override float getAmmoUsage(int chargeLevel) {
		return defaultAmmoUse;
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Point shootPos = character.getCenterPos();
		int xDir = character.getShootXDir();
		Player player = character.player;
		Blues blues = character as Blues ?? throw new NullReferenceException();
		blues.gHoldOwnerYDir *= -1;
		int yDir = blues.gHoldOwnerYDir;
		//character.changeState(new GravityHoldState(), true);

		if (args[1] == 1) {
			if (character.charState is not LadderClimb) character.changeState(new BluesShootAlt(this), true);
			else character.changeState(new BluesShootAltLadder(this), true);
		} else if (args[1] == 2) {
			new GravityHoldProj(shootPos, xDir, player, player.getNextActorNetId(), true);
		}
	}
}


public class GravityHoldProj : Projectile {

	bool fired;
	bool effect;
	float r = 0;
	float maxR = 80;
	float midR;
	bool changeColor;

	public GravityHoldProj(
		Point pos, int xDir, Player player, 
		ushort? netProjId, bool rpc = false
	) : base 
	(
		GravityHold.netWeapon, pos, xDir, 0, 0,
		player, "empty", 0, 1, netProjId,
		player.ownedByLocalPlayer
	) {
		projId = (int)BluesProjIds.GravityHold;
		//maxTime = 0.1f;
		shouldShieldBlock = false;
		destroyOnHit = false;
		midR = maxR / 2;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new GravityHoldProj(
			args.pos, args.xDir, args.player, args.netId
		);
	}

	public override void update() {
		base.update();

		if (r < maxR) r += 4;
		else destroySelf();
		if (r >= midR) changeColor = true;
		
		if (!effect) {
			new GravityHoldEffect(pos, damager.owner.character, damager.owner.character.grounded);
			effect = true;
		}

		foreach (var gameObject in Global.level.getGameObjectArray()) {
			if (gameObject is Actor actor && !fired &&
				actor.ownedByLocalPlayer &&
				gameObject is Character chr && chr != null &&
				chr.canBeDamaged(damager.owner.alliance, damager.owner.id, null)
			) {
				if (chr == null) return;
				if (chr.player.alliance == damager.owner.alliance) continue;
				if (chr.isCCImmune()) continue;
				if (chr.grounded) return;
				if (chr.gHoldOwner != null && chr.gHoldOwner != damager.owner) return;
				//if (actor.gHoldOwner != damager.owner) continue;

				if (chr != null && chr.pos.distanceTo(pos) <= 80) {
					chr.gHoldOwner = damager.owner;

					if (!chr.gHolded) {
						chr.gHoldStart();
					} else {
						chr.gHoldEnd(false);
					}
				}
				fired = true;
			}
		}
	}

	public override void render(float x, float y) {
		base.render(x,y);
		var colors = new List<Color>() {
			new Color(241, 9, 18, 128),
			new Color(200, 18, 130, 128)
		};

		DrawWrappers.DrawCircle(pos.x, pos.y, r, false, !changeColor ? colors[0] : colors[1],
		8, ZIndex.Foreground, outlineColor: !changeColor ? colors[0] : colors[1]);

		DrawWrappers.DrawCircle(pos.x, pos.y, maxR - r, false, !changeColor ? colors[1] : colors[0],
		8, ZIndex.Foreground, outlineColor: !changeColor ? colors[1] : colors[0]);
	}
}


public class GravityHoldEffect : Effect {

	Character rootChar;
	int effectFrames;
	Rect rect;
	bool fired;
	Anim? part;
	Anim? rock;
	bool grounded;

	public GravityHoldEffect(Point pos, Character character, bool grounded) : base(pos) {
		rootChar = character;
		rootChar.shakeCamera(true);
		this.grounded = grounded;
	}

	public override void update() {
		base.update();

		if (rock != null && rock.time >= 0.5f) rock?.destroySelf();

		if (!fired && grounded) {
			for (int i = 0; i < 8; i++) {
				float ang = Helpers.randomRange(-128, 0);
				rock = new Anim(rootChar.pos.addRand(80,0), "gravity_hold_rocks", 
					1 ,rootChar.player.getNextActorNetId(), 
					false, true) {vel = Point.createFromByteAngle(ang) * Helpers.randomRange(60, 180)};
				
				rock.frameSpeed = 0;
				rock.frameIndex = Helpers.randomRange(0, rock.sprite.frames.Count - 1);
				rock.useGravity = true;
			}
			 
			fired = true;
		}

		if (effectFrames % 8 == 0) {
			for (int i = 0; i < 4; i++) {
				part = new Anim(rootChar.pos.addRand(80, 8), 
				"gravity_hold_charge_part", 1, rootChar.player.getNextActorNetId(), false, true);

				part.vel.y = Helpers.randomRange(-300, -180);
			}

			Point newPos = rootChar.pos.addxy(0, -160);
			for (int i = 0; i < 4; i++) {
				part = new Anim(newPos.addRand(80, 8), 
				"gravity_hold_charge_part", 1, rootChar.player.getNextActorNetId(), false, true);

				part.vel.y = Helpers.randomRange(180, 300);
			}
		}

		effectFrames++;

		if (effectFrames >= 32) {
			part?.destroySelf();
			destroySelf();
		} 
	}

	public override void render(float x, float y) {
		base.render(x, y);
		//DrawWrappers.DrawCircle(pos.x, pos.y, 80, true, 
		// Color(255,255,255,255), 1, ZIndex.Backwall);
	}
}


public class GravityHoldState : CharState {

	Blues blues = null!;
	bool fired;

	public GravityHoldState() : base("strikeattack") {
		normalCtrl = false;
		attackCtrl = false;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		blues = character as Blues ?? throw new NullReferenceException();
		blues.gHoldOwnerYDir *= -1;
	}

	public override void update() {
		base.update();

		if (!fired && character.isAnimOver()) {
			new GravityHoldProj(character.getCenterPos(), character.getShootXDir(), 
				player, player.getNextActorNetId(), true);
			fired = true;
		}

		if (character.isAnimOver()) character.changeToIdleOrFall();
	}
}
