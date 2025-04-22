using System;
using System.Collections.Generic;
using SFML.Graphics;

namespace MMXOnline;

public class GravityHold : Weapon {
	public static GravityHold netWeapon = new();

	public GravityHold() : base() {
		displayName = "GRAVITY HOLD";
		descriptionV2 = "";
		defaultAmmoUse = 5;
		descriptionV2 = "manipulates airborne enemies gravity\nslamming them into the ground";

		index = (int)BluesWeaponIds.GravityHold;
		fireRate = 55;
		hasCustomAnim = true;
	}

	public override float getAmmoUsage(int chargeLevel) {
		return defaultAmmoUse;
	}

	public override void shoot(Character character, params int[] args) {
		if (character.charState is not LadderClimb lc) {
			character.changeState(new BluesGravityHold(), true);
		} else {
			character.changeState(new BluesShootAltLadder(this, lc.ladder), true);
		}
	}
}


public class GravityHoldProj : Projectile {
	bool effect;
	float r = 0;
	float maxR = 80;
	float midR;
	bool changeColor;

	public GravityHoldProj(
		Actor owner, Point pos, int xDir, ushort? netProjId, 
		bool rpc = false, Player? altPlayer = null
	) : base
	(
		pos, xDir, owner, "empty", netProjId, altPlayer
	) {
		projId = (int)BluesProjIds.GravityHold;
		//maxTime = 0.1f;
		shouldShieldBlock = false;
		destroyOnHit = false;
		midR = maxR / 2;
		netcodeOverride = NetcodeModel.FavorDefender;

		damager.hitCooldown = 45;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netProjId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new GravityHoldProj(
			args.owner, args.pos, args.xDir, args.netId, altPlayer: args.player
		);
	}

	public override void update() {
		base.update();
		if (owner.character != null) {
			changePos(owner.character.getCenterPos());
		}
		if (r < maxR) {
			r += 4;
		} else {
			destroySelf();
		}
		if (r >= midR) {
			changeColor = true;
		}
		if (!effect && ownedByLocalPlayer && damager.owner.character != null) {
			new GravityHoldEffect(pos, damager.owner.character, damager.owner.character.grounded);
			effect = true;
		}
		if (changeColor) {
			foreach (Actor actor in getCloseActors(160)) {
				if (actor.ownedByLocalPlayer && actor is Character chara &&
					chara.getCenterPos().distanceTo(pos) <= maxR + 20 &&
					chara.canBeDamaged(damager.owner.alliance, damager.owner.id, projId) &&
					chara.projectileCooldown.GetValueOrDefault(projId + "_" + owner.id) <= 0
				) {
					if (!chara.grounded && !chara.isPushImmune()) {
						chara.gHoldOwner = damager.owner;
						chara.gHolded = true;
						gHoldModifier = 1;
						chara.charState.stoppedJump = true;
						chara.vel.y = 800;
					} else {
						damager.applyDamage(
							chara, false, GravityHold.netWeapon, this,
							projId, 1, Global.defFlinch
						);
					}
					chara.projectileCooldown[projId + "_" + owner.id] = 1 * 60;
				}
			}
		}
	}

	public override void render(float x, float y) {
		base.render(x, y);
		float transparencyMultiplier = 1;
		if (r + 16 >= maxR) {
			transparencyMultiplier = (maxR - r) / 16;
		}

		Color[] colors = [
			new Color(241, 9, 18, (byte)MathF.Ceiling(140 * transparencyMultiplier)),
			new Color(200, 18, 130, (byte)MathF.Ceiling(140* transparencyMultiplier)),
			new Color(241, 9, 18, (byte)MathF.Ceiling(64 * transparencyMultiplier)),
			new Color(200, 18, 130, (byte)MathF.Ceiling(64 * transparencyMultiplier))
		];

		if (r > 40) {
			float innerSize = maxR - r;
			if (innerSize <= 0) {
				innerSize = 1;
			} 
			DrawWrappers.DrawCircle(
				pos.x, pos.y, innerSize, true, new Color(0, 0, 0, 0),
				r - (maxR - r), ZIndex.Backwall, outlineColor: colors[3]
			);
		}

		float redSize = r;
		float purpleSize = maxR - r;

		if (changeColor) {
			(redSize, purpleSize) = (purpleSize, redSize);
		}

		DrawWrappers.DrawCircle(
			pos.x, pos.y, redSize, true,
			colors[2],
			0, ZIndex.Backwall
		);

		DrawWrappers.DrawCircle(
			pos.x, pos.y, redSize, true,
			Color.Transparent,
			8, ZIndex.Foreground,
			outlineColor: colors[0]
		);

		DrawWrappers.DrawCircle(
			pos.x, pos.y, purpleSize, true,
			Color.Transparent,
			8, changeColor ? ZIndex.Foreground : ZIndex.Backwall,
			outlineColor: changeColor ? colors[1] : colors[3]
		);
	}

	public override void onStart() {
		base.onStart();
		playSound("gHoldCrash");
	}
}

public class GravityHoldEffect : Effect {
	Character rootChar;
	int effectFrames;
	bool fired;
	bool grounded;

	public GravityHoldEffect(Point pos, Character character, bool grounded) : base(pos) {
		rootChar = character;
		rootChar.shakeCamera(true);
		this.grounded = grounded;
		Global.level.addEffect(this);
	}

	public override void update() {
		base.update();

		if (!fired && grounded) {
			for (int i = 0; i < 8; i++) {
				float ang = Helpers.randomRange(-128, 0);
				Anim rock = new Anim(
					rootChar.pos.addRand(80, 0),
					"gravity_hold_rocks",
					1, rootChar.player.getNextActorNetId(), false, true
				);
				rock.vel.x = Helpers.randomRange(-100, 100);
				rock.vel.y = -225;
				rock.frameSpeed = 0;
				rock.frameIndex = Helpers.randomRange(0, rock.sprite.totalFrameNum - 1);
				rock.useGravity = true;
				rock.ttl = 0.5f;
			}
			fired = true;
		}

		if (effectFrames % 8 == 0) {
			for (int i = 0; i < 4; i++) {
				Anim part = new Anim(
					rootChar.pos.addRand(80, 0),
					"gravity_hold_charge_part",
					1, null, false
				);
				part.ttl = 30 / 60f;
				part.vel.y = Helpers.randomRange(-300, -180);
			}
		}
		if (effectFrames >= 32) {
			destroySelf();
			return;
		}
		effectFrames++;
	}

	public override void render(float x, float y) {
		base.render(x, y);
		//DrawWrappers.DrawCircle(pos.x, pos.y, 80, true, 
		// Color(255,255,255,255), 1, ZIndex.Backwall);
	}
}

public class BluesGravityHold : CharState {
	Blues blues = null!;
	bool fired;

	public BluesGravityHold() : base("shoot2") {
		normalCtrl = false;
		airMove = false;
		useGravity = false;
		landSprite = "shoot2";
		airSprite = "shoot2_air";
	}

	public override void update() {
		base.update();

		if (!fired && character.frameIndex >= 2) {
			new GravityHoldProj(
				blues, blues.getCenterPos(), blues.xDir,
				player.getNextActorNetId(), true
			);
			fired = true;
		}

		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		blues = character as Blues ?? throw new NullReferenceException();
		blues.stopMovingWeak();
		if (!blues.grounded || blues.vel.y < 0) {
			blues.changeSpriteFromName(airSprite, true);
		}
		blues.shieldCustomState = blues.isShieldActive;
	}

	public override void onExit(CharState newState) {
		blues.shieldCustomState = null;
		blues.inCustomShootAnim = false;
		base.onExit(newState);
	}
}
