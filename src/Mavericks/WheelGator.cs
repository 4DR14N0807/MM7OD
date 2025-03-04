﻿using System;
using System.Collections.Generic;

namespace MMXOnline;

public class WheelGator : Maverick {
	public WheelGatorDrillSpinWeapon GatorSpinWeapon = new WheelGatorDrillSpinWeapon();
	public WheelGatorEatWeapon GatorEatWeapon = new WheelGatorEatWeapon();
	public WheelGatorFallWeapon GatorFallWeapon = new WheelGatorFallWeapon();
	public WheelGatorGrabWeapon GatorGrabWeapon = new WheelGatorGrabWeapon();

	public static Weapon getWeapon() { return new Weapon(WeaponIds.WheelGGeneric, 142); }
	public static Weapon getUpBiteWeapon(Player player) { return new Weapon(WeaponIds.WheelGGeneric, 142, new Damager(player, 4, Global.defFlinch, 0.25f)); }
	public Weapon upBiteWeapon;

	public float damageEaten;
	//public ShaderWrapper eatenShader;

	public WheelGator(
		Player player, Point pos, Point destPos, int xDir, ushort? netId, bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		player, pos, destPos, xDir, netId, ownedByLocalPlayer
	) {
		stateCooldowns.Add(typeof(WheelGShootState), new MaverickStateCooldown(false, false, 1.25f));
		stateCooldowns.Add(typeof(WheelGSpinState), new MaverickStateCooldown(false, false, 2f));

		weapon = getWeapon();
		upBiteWeapon = getUpBiteWeapon(player);

		awardWeaponId = WeaponIds.SpinWheel;
		weakWeaponId = WeaponIds.StrikeChain;
		weakMaverickWeaponId = WeaponIds.WireSponge;

		//eatenShader = Helpers.cloneShaderSafe("wheelgEaten");
		//player.gatorArmorShader?.SetUniform("paletteTexture", Global.textures["paletteWheelGator"]);

		netActorCreateId = NetActorCreateId.WheelGator;
		netOwner = player;
		if (sendRpc) {
			createActorRpc(player.id);
		}

		armorClass = ArmorClass.Heavy;
		canStomp = true;
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;

		if (aiBehavior == MaverickAIBehavior.Control) {
			if (state is MIdle or MRun or MLand) {
				if (input.isPressed(Control.Shoot, player)) {
					if (input.isHeld(Control.Up, player)) {
						changeState(new WheelGUpBiteState());
					} else {
						if (damageEaten > 0) {
							changeState(new WheelGSpitState(damageEaten));
							damageEaten = 0;
						} else {
							changeState(new WheelGBiteState());
						}
					}
				} else if (input.isPressed(Control.Special1, player)) {
					changeState(new WheelGShootState());
				} else if (input.isPressed(Control.Dash, player)) {
					changeState(new WheelGSpinState());
				}
			} else if (state is MJump || state is MFall) {
			}
		}
	}

	public void feedWheelGator(float damage) {
		damageEaten = damage;
		changeState(new WheelGEatState());
	}

	public override string getMaverickPrefix() {
		return "wheelg";
	}

	public override float getRunSpeed() {
		return 85;
	}
	/*
	public override List<ShaderWrapper> getShaders() {
		if (player.gatorArmorShader == null || Global.isOnFrameCycle(4)) return new List<ShaderWrapper>();

		if (damageEaten > 0) {
			return new List<ShaderWrapper>() { player.gatorArmorShader };
		}
		return new List<ShaderWrapper>();
	}
	*/
	// Melee IDs for attacks.
	public enum MeleeIds {
		None = -1,
		Drill,
		Eat,
		Fall,
		Grab,

	}

	// This can run on both owners and non-owners. So data used must be in sync.
	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"wheelg_drill_loop" => MeleeIds.Drill,
			"wheelg_eat_start" => MeleeIds.Eat,
			"wheelg_fall" => MeleeIds.Fall,
			"wheelg_grab_start" => MeleeIds.Grab,
			_ => MeleeIds.None
		});
	}

	// This can be called from a RPC, so make sure there is no character conditionals here.
	public override Projectile? getMeleeProjById(int id, Point pos, bool addToLevel = true) {
		return (MeleeIds)id switch {
			MeleeIds.Drill => new GenericMeleeProj(
				GatorSpinWeapon, pos, ProjIds.WheelGSpin, player,
				1, Global.defFlinch, 6, addToLevel: addToLevel
			),
			MeleeIds.Eat => new GenericMeleeProj(
				GatorEatWeapon, pos, ProjIds.WheelGEat, player,
				6, Global.defFlinch, addToLevel: addToLevel
			),
			MeleeIds.Fall => new GenericMeleeProj(
				GatorFallWeapon, pos, ProjIds.WheelGStomp, player,
				4, Global.defFlinch, 60, addToLevel: addToLevel
			),
			MeleeIds.Grab => new GenericMeleeProj(
				GatorGrabWeapon, pos, ProjIds.WheelGGrab, player,
				0, 0, 0, addToLevel: addToLevel
			),
			_ => null
		};
	}

	public override void updateProjFromHitbox(Projectile proj) {
		if (proj.projId == (int)ProjIds.WheelGStomp) {
			float damage = Helpers.clamp(MathF.Floor(deltaPos.y * 0.9f), 1, 4);
			proj.damager.damage = damage;
		}
	}

	public override MaverickState[] aiAttackStates() {
		var attacks = new MaverickState[]
		{
				new WheelGBiteState(),
				new WheelGShootState(),
				new WheelGSpinState(),
		};
		return attacks;
	}

	public override MaverickState getRandomAttackState() {
		var attacks = new MaverickState[]
		{
				new WheelGBiteState(),
				new WheelGShootState(),
				new WheelGUpBiteState(),
		};
		return attacks.GetRandomItem();
	}

	public override List<byte> getCustomActorNetData() {
		List<byte> customData = base.getCustomActorNetData();
		customData.Add((byte)MathF.Ceiling(damageEaten));

		return customData;
	}

	public override void updateCustomActorNetData(byte[] data) {
		base.updateCustomActorNetData(data);
		data = data[Maverick.CustomNetDataLength..];

		damageEaten = data[0];
	}
}

public class WheelGatorDrillSpinWeapon : Weapon {
	public WheelGatorDrillSpinWeapon() {
		index = (int)WeaponIds.WheelGSpinWeapon;
		killFeedIndex = 142;
	}
}

public class WheelGatorEatWeapon : Weapon {
	public WheelGatorEatWeapon() {
		index = (int)WeaponIds.WheelGEatWeapon;
		killFeedIndex = 142;
	}
}
public class WheelGatorFallWeapon : Weapon {
	public WheelGatorFallWeapon() {
		index = (int)WeaponIds.WheelGFallWeapon;
		killFeedIndex = 142;
	}
}

public class WheelGatorGrabWeapon : Weapon {
	public WheelGatorGrabWeapon() {
		index = (int)WeaponIds.WheelGGrabWeapon;
		killFeedIndex = 142;
	}
}

public class WheelGSpinWheelProj : Projectile {
	float lastHitTime;
	const float hitCooldown = 0.2f;
	public WheelGSpinWheelProj(
		Weapon weapon, Point pos, int xDir, Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 250, 1, player, "wheelg_proj_wheel",
		Global.defFlinch, hitCooldown, netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.WheelGSpinWheel;
		maxTime = 2f;

		destroyOnHit = false;
		vel = new Point(xDir * 200, -200);
		useGravity = true;
		collider.wallOnly = true;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void update() {
		base.update();

		vel.x = xDir * 250;
		if (lastHitTime > 0) vel.x = xDir * 4;
		Helpers.decrementTime(ref lastHitTime);
	}

	int bounces;
	public override void onHitWall(CollideData other) {
		base.onHitWall(other);

		bounces++;

		var normal = other.hitData.normal ?? new Point(0, -1);
		if (normal.isSideways()) {
			vel.x *= -1f;
			xDir *= -1;
			incPos(new Point(5 * MathF.Sign(vel.x), 0));
		} else if (bounces < 2) {
			vel.y *= -0.5f;
			if (vel.y < -300) vel.y = -300;
			incPos(new Point(0, 5 * MathF.Sign(vel.y)));
		}
	}

	public override void onHitDamagable(IDamagable damagable) {
		if (damagable is CrackedWall) {
			damager.hitCooldownSeconds = hitCooldown;
			return;
		}

		lastHitTime = hitCooldown;

		var chr = damagable as Character;
		if (chr != null && chr.ownedByLocalPlayer && !chr.isSlowImmune()) {
			chr.vel = Point.lerp(chr.vel, Point.zero, Global.spf * 10);
			chr.slowdownTime = 0.25f;
		}

		base.onHitDamagable(damagable);
	}
}

public class WheelGShootState : MaverickState {
	int state;
	bool shotOnce;
	public WheelGShootState() : base("wheelthrow_start") {
	}

	public override void update() {
		base.update();

		if (state == 0) {
			if (maverick.isAnimOver()) {
				maverick.changeSpriteFromName("wheelthrow_loop1", true);
				state = 1;
			}
		} else if (state == 1) {
			Point? shootPos = maverick.getFirstPOI();
			if (!shotOnce && shootPos != null) {
				shotOnce = true;
				//maverick.playSound("wheelgSpinWheel", sendRpc: true);
				new WheelGSpinWheelProj((maverick as WheelGator).weapon, shootPos.Value, maverick.xDir, player, player.getNextActorNetId(), rpc: true);
			}

			if (maverick.isAnimOver()) {
				maverick.changeSpriteFromName("wheelthrow_loop2", true);
				shotOnce = false;
				state = 2;
			}
		} else if (state == 2) {
			Point? shootPos = maverick.getFirstPOI();
			if (!shotOnce && shootPos != null) {
				shotOnce = true;
				//maverick.playSound("wheelgSpinWheel", sendRpc: true);
				new WheelGSpinWheelProj((maverick as WheelGator).weapon, shootPos.Value, maverick.xDir, player, player.getNextActorNetId(), rpc: true);
			}

			if (maverick.isAnimOver()) {
				state = 3;
			}
		}

		if (maverick.isAnimOver()) {
			maverick.changeToIdleOrFall();;
		}
	}
}

public class WheelGBiteState : MaverickState {
	int state;
	public WheelGBiteState() : base("eat_start") {
	}

	public override void update() {
		base.update();

		if (state == 0) {
			if (input.isHeld(Control.Shoot, player) && maverick.frameIndex == 4) {
				state = 1;
			}
		} else if (state == 1) {
			if (input.isHeld(Control.Shoot, player)) {
				maverick.frameIndex = 4;
				maverick.frameSpeed = 0;
				maverick.turnToInput(input, player);
			} else {
				state = 2;
				maverick.frameSpeed = 1;
			}
		}

		if (maverick.frameIndex == 6 && !once) {
			//maverick.playSound("wheelgBite", sendRpc: true);
			once = true;
		}

		if (maverick.isAnimOver()) {
			maverick.changeToIdleOrFall();
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.frameSpeed = 1;
	}
}

public class WheelGEatState : MaverickState {
	float soundTime;
	public WheelGEatState() : base("eat_loop") {
	}

	public override void update() {
		base.update();

		Helpers.decrementTime(ref soundTime);
		if (soundTime == 0) {
			//maverick.playSound("wheelgBite", sendRpc: true);
			soundTime = 0.26f;
		}

		if (maverick.loopCount >= 4) {
			maverick.changeToIdleOrFall();
		}
	}
}

public class WheelGSpitProj : Projectile {
	public WheelGSpitProj(
		Weapon weapon, Point pos, int xDir, Point unitVel, Player player, ushort netProjId, bool rpc = false
	) : base(
		weapon, pos, xDir, 0, 2, player, "wheelg_proj_spit", 0, 0.01f, netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.WheelGSpit;
		maxDistance = 150;
		vel = unitVel.times(400);

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
		// ToDo: Make local.
		canBeLocal = false;
	}
}

public class WheelGSpitState : MaverickState {
	bool shotOnce;
	float damageEaten;
	public WheelGSpitState(float damageEaten) : base("eat_spit") {
		this.damageEaten = damageEaten;
	}

	public override void update() {
		base.update();

		Point? shootPos = maverick.getFirstPOI();
		if (!shotOnce && shootPos != null) {
			shotOnce = true;
			//maverick.playSound("wheelgSpit", sendRpc: true);

			Point moveDir = new Point(maverick.xDir, 0);
			var targets = Global.level.getTargets(shootPos.Value, maverick.player.alliance, true);
			foreach (var target in targets) {
				Point dirTo = shootPos.Value.directionToNorm(target.getCenterPos());
				if (dirTo.isSideways() && MathF.Sign(dirTo.x) == maverick.xDir) {
					moveDir = dirTo;
					break;
				}
			}

			new WheelGSpitProj((maverick as WheelGator).weapon, shootPos.Value, maverick.xDir, moveDir, player, player.getNextActorNetId(), rpc: true);
			damageEaten--;
		}

		if (maverick.isAnimOver()) {
			if (damageEaten > 0) {
				maverick.frameIndex = maverick.sprite.totalFrameNum - 2;
				shotOnce = false;
			} else {
				maverick.changeToIdleOrFall();
			}
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.frameSpeed = 1;
	}
}

public class WheelGSpinState : MaverickState {
	int state = 0;
	float soundTime;
	public WheelGSpinState() : base("drill_start", "jump_start") {
	}

	public override void update() {
		base.update();
		Helpers.decrementTime(ref soundTime);

		if (state == 0) {
			if (!inTransition()) {
				state = 1;
				maverick.vel.y = -maverick.getJumpPower();
			}
		} else if (state == 1) {
			maverick.stopOnCeilingHit();
			if (maverick.grounded) {
				maverick.changeSpriteFromName("drill_loop", true);
				maverick.useGravity = false;
				maverick.stopMoving();
				state = 2;
				stateTime = 0;
			}
		} else if (state == 2) {
			maverick.move(new Point(maverick.xDir * 250, 0));
			if (soundTime == 0) {
				soundTime = 0.247f;
				//maverick.playSound("wheelgSpin", sendRpc: true);
			}

			if (stateTime > 0.75f) {
				maverick.changeToIdleOrFall();
			}
		}
	}

	public override void onEnter(MaverickState oldState) {
		base.onEnter(oldState);
		maverick.stopMoving();
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		maverick.useGravity = true;
	}
}

public class WheelGUpBiteState : MaverickState {
	public Character victim;
	int state;
	int shootFramesHeld;
	bool shootReleased;
	public WheelGUpBiteState() : base("grab_start", "jump_start") {
	}

	public override void update() {
		base.update();

		if (input.isHeld(Control.Shoot, player) && !shootReleased) {
			shootFramesHeld++;
		} else {
			shootReleased = true;
		}

		if (state == 0) {
			if (!inTransition()) {
				state = 1;
				float jumpMod = 1 + Helpers.clamp01(shootFramesHeld / 24f);
				maverick.vel.y = -maverick.getJumpPower() * jumpMod * 0.875f;
			}
		} else if (state == 1) {
			maverick.stopOnCeilingHit();
			if (maverick.grounded) {
				landingCode();
			}
		}
	}

	public Character getVictim() {
		if (victim == null) return null;
		if (victim.sprite.name.EndsWith("_grabbed")) {
			return null;
		}
		return victim;
	}

	public override bool trySetGrabVictim(Character grabbed) {
		if (victim == null) {
			victim = grabbed;
			if (maverick.ownedByLocalPlayer) {
				(maverick as WheelGator).upBiteWeapon.applyDamage(victim, false, maverick, (int)ProjIds.WheelGUpBite, sendRpc: true);
				//maverick.playSound("wheelgBite", sendRpc: true);
			}
			return true;
		}
		return false;
	}

	public override void onExit(MaverickState newState) {
		base.onExit(newState);
		if (getVictim() != null) {
			victim?.releaseGrab(maverick);
		}
	}
}

public class WheelGGrabbed : GenericGrabbedState {
	public Character grabbedChar;
	public float timeNotGrabbed;
	string lastGrabberSpriteName;
	public const float maxGrabTime = 4;
	public WheelGGrabbed(WheelGator grabber) : base(grabber, maxGrabTime, "grabbed") {
	}

	public override void update() {
		string grabberSpriteName = grabber.sprite?.name ?? "";
		if (grabberSpriteName.EndsWith("_grab_start") == true) {
			if (lastGrabberSpriteName != grabberSpriteName) {
				if (!trySnapToGrabPoint(true)) {
					character.changeToIdleOrFall();
					return;
				}
			} else {
				character.incPos(grabber.deltaPos);
			}
		} else {
			timeNotGrabbed += Global.spf;
			if (timeNotGrabbed > 0.1f) {
				character.changeToIdleOrFall();
				return;
			}
		}
		lastGrabberSpriteName = grabberSpriteName;

		grabTime -= player.mashValue();
		if (grabTime <= 0) {
			character.changeToIdleOrFall();
		}
	}
}
