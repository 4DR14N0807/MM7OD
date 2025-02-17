﻿using System;
using System.Collections.Generic;
using System.Linq;
using static MMXOnline.GameMode;
using SFML.Graphics;

namespace MMXOnline;

public class Blues : Character {
	// Lemons.
	public float lemonCooldown;
	public float[] unchargedLemonCooldown = new float[3];

	// Mode variables.
	public bool isShieldActive = true;
	public bool isBreakMan;
	public const int reviveCost = 75;

	// Core heat system.
	public float coreMaxAmmo = 28;
	public float coreAmmo;
	public float coreAmmoMaxCooldown = 60;
	public float coreAmmoDamageCooldown = 60;
	public float coreAmmoDecreaseCooldown;
	public bool overheating;
	public float overheatEffectTime;
	public bool starCrashOverheat;

	// Break Man stuff.
	public bool overdrive;
	public float overdriveAmmo = 20;
	public float overdriveAmmoDecreaseCooldown;
	public float overdriveAmmoMaxCooldown = 15;
	public float redStrikeCooldown = 0;

	// Shield vars.
	public decimal shieldHP = 20;
	public int shieldMaxHP = 20;
	public float healShieldHPCooldown = 15;
	public decimal shieldDamageSavings;
	public decimal shieldDamageDebt;
	public bool? shieldCustomState = null;
	public bool customDamageDisplayOn;
	public bool fastShieldHeal;

	// Tanks
	public bool isUsingLTank;
	public float lTankHealShieldAmount;
	public float lTankHealShieldCooldown;
	public float lTankCoreHealAmount;
	public float lTankCoreHealTime;

	// Special weapon stuff
	public Weapon specialWeapon;
	public int specialWeaponIndex;
	public bool starCrashActive;
	public StarCrashProj? starCrash;
	public HardKnuckleProj? hardKnuckleProj;
	public bool inCustomShootAnim;

	// Gravity Hold stuff
	public int gHoldOwnerYDir = 1;

	// AI variables.
	public float aiSpecialUseTimer = 0;
	public bool aiActivateShieldOnLand;

	// Netcode stuff.
	public int netChargeLevel;
	public decimal lastDamageNum;

	// Creation code.
	public Blues(
		Player player, float x, float y, int xDir, bool isVisible,
		ushort? netId, bool ownedByLocalPlayer, bool isWarpIn = true
	) : base(
		player, x, y, xDir, isVisible, netId, ownedByLocalPlayer, isWarpIn, false, false
	) {
		charId = CharIds.Blues;
		int protomanLoadout = player.loadout.bluesLoadout.specialWeapon;
		charge1Time = 40;
		charge2Time = 105;
		charge3Time = 170;

		specialWeaponIndex = protomanLoadout;
		specialWeapon = protomanLoadout switch {
			0 => new NeedleCannon(),
			1 => new HardKnuckle(),
			2 => new SearchSnake(),
			3 => new SparkShock(),
			4 => new GravityHold(),
			5 => new PowerStone(),
			6 => new GyroAttack(),
			7 => new StarCrash(),
			_ => new PowerStone(),
		};
		shieldMaxHP = (int)Player.getModifiedHealth(shieldMaxHP);
		shieldHP = shieldMaxHP;

		if (isWarpIn && ownedByLocalPlayer) {
			shieldHP = 0;
			healShieldHPCooldown = 39 + MathF.Ceiling(42 * Player.getHealthModifier());
			fastShieldHeal = true;
		}
	}

	public override bool canAddAmmo() {
		return (coreAmmo > 0 && !overdrive);
	}

	public override float getRunSpeed() {
		bool shieldEquipped = isShieldEquipped();
		float runSpeed = Physics.WalkSpeed;
		if (overdrive) {
			if (shieldEquipped) {
				runSpeed = 0.825f * 60;
			} else {
				runSpeed = 1.2f * 60;
			}
		}
		else if (overheating) {
			runSpeed = 0.75f * 60;
			if (!shieldEquipped) {
				runSpeed = 1.35f * 60;
			}
		}
		else if (shieldEquipped) {
			runSpeed = 1.125f * 60;
		}
		return runSpeed * getRunDebuffs();
	}

	public override float getDashSpeed() {
		bool shieldEquipped = isShieldEquipped();
		float dashSpeed = 3f * 60;
		if (overdrive) {
			dashSpeed = 2.75f * 60;
			if (shieldEquipped) {
				dashSpeed = 2.25f * 60;
			}
		}
		else if (shieldEquipped) {
			dashSpeed = 2.5f * 60;
		}
		return dashSpeed * getRunDebuffs();
	}

	public float getShieldDashSpeed() {
		bool shieldEquipped = isShieldEquipped();
		float dashSpeed = 3.25f * 60;
		if (overdrive) {
			dashSpeed = 3f * 60;
			if (shieldEquipped) {
				dashSpeed = 2.5f * 60;
			}
		}
		else if (shieldEquipped) {
			dashSpeed = 2.75f * 60;
		}
		return dashSpeed * getRunDebuffs();
	}

	public float getSlideSpeed() {
		float slideSpeed = 3 * 60;
		if (overheating) {
			slideSpeed = 1.5f * 60;
		}
		return slideSpeed * getRunDebuffs();
	}

	public override float getJumpPower() {
		if (flag != null) {
			return base.getJumpPower();
		}
		bool shieldEquipped = isShieldEquipped();
		float jumpSpeed = Physics.JumpSpeed;
		if (overheating) {
			if (shieldEquipped) {
				jumpSpeed = 5 * 60;
			} else {
				jumpSpeed = 5.25f * 60;
			}
		}
		else if (overdrive) {
			if (shieldEquipped) {
				jumpSpeed = 4.875f * 60;
			} else {
				jumpSpeed = 5.125f * 60;
			}
		}
		else if (shieldEquipped) {
			jumpSpeed = 5.25f * 60;
		}
		return jumpSpeed * getJumpModifier();
	}

	public override bool canAirJump() {
		if (isBreakMan && overdrive) {
			return (dashedInAir == 0);
		}
		return false;
	}

	public override bool canTurn() {
		if (charState is BluesSpreadShoot) {
			return false;
		}
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
		if (overheating) {
			return false;
		}
		if (charState.attackCtrl || charState.normalCtrl ||
			charState is ShieldDash or BluesSlide or Hurt or GenericStun or Burning
		) {
			return base.canCharge();
		}
		return false;
	}

	public bool canShieldDash() {
		return (
			flag == null &&
			(grounded || isBreakMan && dashedInAir == 0) &&
			charState is not ShieldDash &&
			!overheating && rootTime <= 0
		);
	}

	public bool canSlide() {
		return (
			flag == null &&
			grounded && vel.y >= 0 &&
			charState is not BluesSlide and not ShieldDash &&
			!overdrive && rootTime <= 0
		);
	}


	public override bool canJump() {
		if (charState is ShieldDash) {
			return false;
		}
		return base.canJump();
	}

	public bool canUseShield() {
		if (shootAnimTime > 0 && !sprite.name.EndsWith("_shield") ||
			!charState.normalCtrl || charState is Slide ||
			charState is BigBangStrikeState ||
			charState is BigBangStrikeStart
		) {
			return false;
		}
		return true;
	}

	public bool canShootSpecial() {
		if (flag != null ||
			isCharging() ||
			overheating ||
			overdrive ||
			specialWeapon.shootCooldown > 0 ||
			!specialWeapon.canShoot(0, this) ||
			invulnTime > 0
		) {
			return false;
		}
		return true;
	}

	public bool canUseBigBangStrike() {
		return grounded && overheating;
	}
	
	public bool canUseDropSwap() {
		return (
			isBreakMan && !overheating &&
			rootTime <= 0 && !isDWrapped
		);
	}

	public void delinkStarCrash() {
		starCrash = null;
		gravityModifier = 1;
		starCrashActive = false;

		if (specialWeapon is StarCrash) {
			specialWeapon.shootCooldown = specialWeapon.fireRate;
		}
	}

	public override string getSprite(string spriteName) {
		return "blues_" + spriteName;
	}

	public override void changeSprite(string spriteName, bool resetFrame) {
		if (!ownedByLocalPlayer) {
			base.changeSprite(spriteName, resetFrame);
			return;
		}
		bool shieldEquipped = isShieldEquipped();
		if (shieldEquipped && spriteName == getSprite("idle_shield") && getChargeLevel() >= 2) {
			spriteName = getSprite("idle_charge_shield");
		}
		else if (shieldEquipped && Global.sprites.ContainsKey(spriteName + "_shield")) {
			spriteName += "_shield";
		}
		List<Trail>? trails = sprite.lastFiveTrailDraws;
		base.changeSprite(spriteName, resetFrame);
		if (trails != null) {
			sprite.lastFiveTrailDraws = trails;
		}
		if (isBreakMan && sprite.animData.textureName == "blues_default") {
			sprite.overrideTexture = Sprite.breakManBitmap;
		}
	}

	public void changeSpriteEX(string spriteName, bool resetFrame) {
		if (!ownedByLocalPlayer) {
			base.changeSprite(spriteName, resetFrame);
			return;
		}
		List<Trail>? trails = sprite.lastFiveTrailDraws;
		base.changeSprite(spriteName, resetFrame);
		if (trails != null) {
			sprite.lastFiveTrailDraws = trails;
		}
		if (isBreakMan && sprite.animData.textureName == "blues_default") {
			sprite.overrideTexture = Sprite.breakManBitmap;
		}
	}

	public override bool changeState(CharState newState, bool forceChange = false) {
		bool? oldScs = shieldCustomState;
		shieldCustomState = null;
		bool changedState = base.changeState(newState, forceChange);
		if (!changedState) {
			shieldCustomState = oldScs;
			return false;
		}
		if (!newState.attackCtrl || !newState.normalCtrl) {
			shootAnimTime = 0;
		}
		return true;
	}

	public override Projectile? getProjFromHitbox(Collider hitbox, Point centerPoint) {
		int meleeId = getHitboxMeleeId(hitbox);
		if (meleeId == -1) {
			return null;
		}
		Projectile? proj = getMeleeProjById(meleeId, centerPoint);
		if (proj == null) {
			return null;
		}
		// Assing data variables.
		proj.meleeId = meleeId;
		proj.ownerActor = this;

		return proj;
	}

	public enum MeleeIds {
		None = -1,
		ShieldBlock,
		ProtoStrike,
	}

	public override bool chargeButtonHeld() {
		return player.input.isHeld(Control.Shoot, player);
	}

	public override void preUpdate() {
		base.preUpdate();
		// Cooldowns.
		Helpers.decrementFrames(ref lemonCooldown);
		Helpers.decrementFrames(ref redStrikeCooldown);
		Helpers.decrementFrames(ref lTankCoreHealTime);
		Helpers.decrementFrames(ref lTankHealShieldCooldown);
		if (lTankHealShieldAmount <= 0) {
			Helpers.decrementFrames(ref healShieldHPCooldown);
		}
		for (int i = 0; i < unchargedLemonCooldown.Length; i++) {
			Helpers.decrementFrames(ref unchargedLemonCooldown[i]);
		}
		// Core ammo regen.
		if (!overdrive && !isCharging() ||
			overdrive && chargeTime <= charge1Time/2f || 
			chargeTime > charge3Time + (overdrive ? 20 : 10)) {
			Helpers.decrementFrames(ref coreAmmoDecreaseCooldown);
			Helpers.decrementFrames(ref overdriveAmmoDecreaseCooldown);
		}
	}

	public override void update() {
		base.update();

		// Hypermode music.
		if (false && !Global.level.isHyper1v1()) {
			if (isBreakMan) { 
				if (musicSource == null) {
					addMusicSource("breakman", getCenterPos(), true);
				}
			} else {
				destroyMusicSource();
			}
		}

		// Netcode update ends here.
		if (!ownedByLocalPlayer) {
			overheatGfx();
			return;
		}

		// Special weapon stuff.
		specialWeapon.update();

		// Revive stuff.
		if (player.canReviveBlues() && player.input.isPressed(Control.Special2, player)) {
			changeState(new BluesRevive(), true);
			player.currency -= reviveCost;
		}

		// Shield HP.
		if (healShieldHPCooldown <= 0 && shieldHP < shieldMaxHP) {
			playSound("heal");
			shieldHP++;
			healShieldHPCooldown = 6;
			if (fastShieldHeal) {
				healShieldHPCooldown = 3; 
			}
			if (shieldHP >= shieldMaxHP) {
				shieldHP = shieldMaxHP;
				fastShieldHeal = false;
			}
		}

		if (coreAmmo >= coreMaxAmmo && !overheating && !overdrive && !inCustomShootAnim) {
			if (isBreakMan) {
				overdrive = true;
				overdriveAmmo = 20;
			} else {
				overheating = true;
				coreAmmoDecreaseCooldown = 60;
			}
			starCrash?.destroySelf();
			delinkStarCrash();
			setHurt(-xDir, Global.halfFlinch, false);
			playSound("danger_wrap_explosion", sendRpc: true);
			stopCharge();
			starCrashOverheat = false;
		}
		if (isCharging() && chargeTime <= charge3Time + (overdrive ? 20 : 10) && !overheating) {
			if (coreAmmoDecreaseCooldown < coreAmmoMaxCooldown) {
				coreAmmoDecreaseCooldown = coreAmmoMaxCooldown;
			}
			if (overdriveAmmoDecreaseCooldown < overdriveAmmoMaxCooldown) {
				overdriveAmmoDecreaseCooldown = overdriveAmmoMaxCooldown;
			}
		}
		if (coreAmmoDecreaseCooldown <= 0 && !overdrive && charState is not BluesRevive && !starCrashOverheat) {
			coreAmmo--;
			if (coreAmmo <= 0) {
				overheating = false;
				coreAmmo = 0;
			}
			coreAmmoDecreaseCooldown = 15;
			if (overheating) {
				coreAmmoDecreaseCooldown = 12;
			}
		}

		bool overdriveLimit = false;
		if (overdriveAmmoDecreaseCooldown <= 0 && overdrive && charState is not BluesRevive) {
			overdriveAmmo--;
			if (overdriveAmmo <= 0) {
				overdriveAmmo = 0;
				overdriveLimit = true;
			}
			overdriveAmmoDecreaseCooldown = 10;
		}
		if (overdrive && (overdriveLimit || overdriveAmmo >= coreMaxAmmo)) {
			overdrive = false;
			overheating = true;
			coreAmmoDecreaseCooldown = 60;
			overdriveAmmoDecreaseCooldown = 10;
			setHurt(0, Global.defFlinch, false);
			xPushVel = -xDir * 4 * 60;
			playSound("danger_wrap_explosion", sendRpc: true);
			stopCharge();
		}

		// L-Tank check.
		if (isUsingLTank && lTankCoreHealAmount <= 0 &&
			lTankHealShieldAmount <= 0 && eTankHealAmount <= 0
		) {
			isUsingLTank = false;
		}

		// L-Tank Core cooling.
		if (lTankCoreHealAmount > 0 && lTankCoreHealTime <= 0) {
			lTankCoreHealAmount--;
			lTankCoreHealTime = 3;
			if (!overdrive) {
				coreAmmo--;
				if (coreAmmo <= 0) {
					coreAmmo = 0;
					lTankCoreHealAmount = 0;
					lTankCoreHealTime = 0;
				}
				playSound("heal");
			}
		}

		// L-Tank Shield HP Heal.
		if (lTankHealShieldAmount > 0 && lTankHealShieldCooldown <= 0) {
			shieldHP++;
			lTankHealShieldAmount--;
			lTankHealShieldCooldown = 8;
			playSound("heal");
			if (shieldHP >= shieldMaxHP) {
				shieldHP = shieldMaxHP;
				lTankHealShieldAmount = 0;
				lTankHealShieldCooldown = 0;
			}
		}

		// For the shooting animation.
		if (shootAnimTime > 0 || charState is LadderClimb) {
			shootAnimTime -= Global.spf;
			if (shootAnimTime <= 0) {
				shootAnimTime = 0;
				if (sprite.name.EndsWith("_shoot") || sprite.name.EndsWith("_shoot_shield")) {
					changeSpriteFromName(charState.defaultSprite, false);
					if (charState is WallSlide) {
						frameIndex = sprite.totalFrameNum - 1;
					}
				}
			}
		}

		// Shoot logic.
		chargeLogic(shoot);

		// Charge animations.
		int requiredCharge = (isBreakMan ? 1 : 2);
		if (getChargeLevel() >= requiredCharge) {
			if (sprite.name == getSprite("idle_shield")) {
				changeSpriteFromName("idle_charge_shield", true);
			}
		}

		// Overheat stuff.
		overheatGfx();
	}

	public void overheatGfx() {
		if (overheating || overdrive) {
			overheatEffectTime += Global.speedMul;
			if (overheatEffectTime >= 3) {
				overheatEffectTime = 0;
				Point burnPos = pos.addxy(xDir * 2, -15);
				string sprite = "dust";
				if (overdrive) {
					sprite = "charge_part_1";
				}

				Anim tempAnim = new Anim(burnPos.addRand(14, 15), sprite, 1, null, true, host: this);
				tempAnim.vel.y = -120;
				if (overheating) {
					tempAnim.addRenderEffect(RenderEffectType.ChargeOrange, 3, 120, 5);
				} else {
					RenderEffectType smokeEffect = getChargeLevel() switch {
						1 => RenderEffectType.ChargeBlue,
						2 => RenderEffectType.ChargePurple,
						3 => RenderEffectType.ChargeGreen,
						_ => RenderEffectType.ChargeYellow,
					};
					tempAnim.addRenderEffect(smokeEffect, 3, 120, 5);
				}
			}
		}
		if (overdrive && getChargeLevel() <= 0) {
			addRenderEffect(RenderEffectType.ChargeYellow, 3, 5);
		}
		else if (overheating) {
			addRenderEffect(RenderEffectType.ChargeOrange, 3, 5);
		}
	}

	public override int getChargeLevel() {
		if (!ownedByLocalPlayer) {
			return netChargeLevel;
		}
		return base.getChargeLevel();
	}

	public override void onFlinchOrStun(CharState newState) {
		if (!overdrive && !overheating) {
			coreAmmoDecreaseCooldown = coreAmmoDamageCooldown;
		}
		base.onFlinchOrStun(newState);
	}

	public override bool normalCtrl() {
		//For getting the slide and shield dash input
		bool slide = slideInput();
		bool shieldDash = shieldDashInput();

		// For keeping track of shield change.
		bool lastShieldMode = isShieldActive;
		// Shield switch.
		if (!player.isAI && shieldHP > 0 && shootAnimTime <= 0 && canUseShield()) {
			if (Options.main.protoShieldHold) {
				isShieldActive = player.input.isWeaponLeftOrRightHeld(player);
			}
			else if (player.input.isWeaponLeftOrRightPressed(player)) {
				isShieldActive = !isShieldActive;
			}
			if (lastShieldMode != isShieldActive) {
				if (isShieldActive) {
					isDashing = false;
					if (vel.y < 0) {
						vel.y *= 0.625f;
					}
				}
				if (!grounded && lastShieldMode != isShieldActive &&
					canUseDropSwap() &&
					player.input.isHeld(Control.Down, player)
				) {
					if (vel.y < 6 * 60) {
						vel.y = 6 * 60;
					}
					isShieldActive = true;
					changeState(new BluesShieldSwapAir());
					return true;
				}
			}
		}
		// Change sprite is shield mode changed.
		if (lastShieldMode != isShieldActive) {
			if (shootAnimTime == 0 && charState is Idle idleState) {
				idleState.transitionSprite = getSprite("idle_swap");
				if (isShieldActive) {
					idleState.transitionSprite += "_shield";
				}
				idleState.sprite = idleState.transitionSprite;
				changeSprite(idleState.sprite, true);
			}
			else if (!isShieldActive || (shieldHP <= 0 && charState is not BigBangStrikeStart)) {
				isShieldActive = false;
				if (sprite.name == getSprite("idle_charge_shield")) {
					changeSpriteFromName("idle", true);
				}
				else if (sprite.name.EndsWith("_shield")) {
					changeSprite(sprite.name[..^7], false);
				}
			} else {
				isShieldActive = true;
				if (!sprite.name.EndsWith("_shield")) {
					changeSprite(sprite.name + "_shield", false);
				}
			}
		}
		if (slide && canSlide())  {
			changeState(new BluesSlide(), true);
			return true;
		}
		if (shieldDash && canShieldDash()) {
			addCoreAmmo(2);
			changeState(new ShieldDash(), true);
			return true;
		}
		return base.normalCtrl();
	}

	public override bool attackCtrl() {
		bool shootPressed = player.input.isPressed(Control.Shoot, player);
		bool specialPressed = player.input.isPressed(Control.Special1, player);
		if (!overheating && !overdrive && specialWeapon is NeedleCannon) {
			specialPressed = player.input.isHeld(Control.Special1, player);
		}
		bool downHeld = player.input.isHeld(Control.Down, player);

		if (specialPressed) {
			if (canShootSpecial()) {
				shootSpecial(0);
				return true;
			} else if (overdrive && redStrikeCooldown == 0) {
				changeState(new RedStrike(), true);
				return true;
			} else if (canUseBigBangStrike()) {
				bool bufferedShield = isShieldActive;
				isShieldActive = false;
				changeState(new BigBangStrikeStart(), true);
				isShieldActive = bufferedShield;
				return true;
			}
		}

		if (shootPressed && downHeld && !grounded) {
			changeState(new BluesSpreadShoot(), true);
			return true;
		}

		if (!isCharging()) {
			if (shootPressed) {
				if (lemonCooldown <= 0) {
					shoot(0);
					return true;
				}
			}
		}
		return base.attackCtrl();
	}

	public void shoot(int chargeLevel) {
		if (!ownedByLocalPlayer) return;

		int lemonNum = -1;
		int type = overdrive ? 1 : 0;

		if (chargeLevel == 0) {
			for (int i = 0; i < unchargedLemonCooldown.Length; i++) {
				if (unchargedLemonCooldown[i] <= 0) {
					lemonNum = i;
					break;
				}
			}
			if (lemonNum == -1) {
				return;
			}
		}
		// Cancel non-invincible states.
		if (!charState.attackCtrl && !charState.invincible || charState is BluesSlide) {
			changeToIdleOrFall();
		}
		// Shoot anim and vars.
		float oldShootAnimTime = shootAnimTime;
		setShootAnim();
		Point shootPos = getShootPos();
		int xDir = getShootXDir();

		if (chargeLevel <= 0) {
			if (type == 0) {
				new ProtoBusterProj(
					this, shootPos, xDir, player.getNextActorNetId(), rpc: true
				);
			} else {
				new ProtoBusterOverdriveProj(
					this, shootPos, xDir, player.getNextActorNetId(), rpc: true
				);
				playSound("buster2", sendRpc: true);
				addCoreAmmo(0.75f);
			}
			playSound("buster", sendRpc: true);
			lemonCooldown = 8;
			unchargedLemonCooldown[lemonNum] = 50;
			if (oldShootAnimTime <= 0.25f) {
				shootAnimTime = 0.25f;
			}
		} else if (chargeLevel == 1) {
			new ProtoBusterLv2Proj(
				this, type, shootPos, xDir, player.getNextActorNetId(), true
			);
			addCoreAmmo(getChargeShotAmmoUse(1));
			if (type == 0) {
				playSound("buster2", sendRpc: true);
			} else {
				playSound("buster3", sendRpc: true);
			}
			lemonCooldown = 12;
		} else if (chargeLevel == 2) {
			new ProtoBusterLv3Proj(
				this, type, shootPos, xDir, player.getNextActorNetId(), true
			);
			addCoreAmmo(getChargeShotAmmoUse(2));
			playSound("buster3", sendRpc: true);
			lemonCooldown = 12;
		} else {
			if (player.input.isHeld(Control.Up, player)) {
				addCoreAmmo(overdrive ? 6 : 4);
				changeState(new ProtoStrike(), true);
			} else {
				if (type == 1) {
					var proj = new ProtoBusterLv4Proj(
						this, type, shootPos.addxy(-12 * xDir, 0), xDir, player.getNextActorNetId(), true
					);
					proj.frameIndex = 2;
					proj.maxTime += 4 / 60f;
				}
				new ProtoBusterLv4Proj(
					this, type, shootPos, xDir, player.getNextActorNetId(), true
				);
				if (type == 1) {
					var proj = new ProtoBusterLv4Proj(
						this, type, shootPos.addxy(12 * xDir, 0), xDir, player.getNextActorNetId(), true
					);
					proj.frameIndex = 1;
					proj.maxTime -= 4 / 60f;
				}
				addCoreAmmo(getChargeShotAmmoUse(3));
				playSound("buster3", sendRpc: true);
				lemonCooldown = 12;
			}
		}
	}

	public void shootSpecial(int chargeLevel) {
		if (!ownedByLocalPlayer) return;

		int extraArg = 0;
		if (specialWeapon == null) {
			return;
		}
		// Cancel non-invincible states.
		if (!charState.attackCtrl && !charState.invincible || charState is BluesSlide) {
			changeToIdleOrFall();
		}
		// Shoot anim and vars.
		if (!specialWeapon.hasCustomAnim) {
			setShootAnim();
		} else {
			inCustomShootAnim = true;
			extraArg = 1;
		}

		Point shootPos = getShootPos();
		int xDir = getShootXDir();

		specialWeapon.shootCooldown = specialWeapon.fireRate;
		specialWeapon.shoot(this, chargeLevel, extraArg);
		addCoreAmmo(specialWeapon.getAmmoUsage(chargeLevel));
		if (specialWeapon is StarCrash && coreAmmo >= coreMaxAmmo) starCrashOverheat = true;
	}

	public void setShootAnim() {
		string shootSprite = getSprite(charState.shootSprite);
		if (!Global.sprites.ContainsKey(shootSprite)) {
			if (grounded) {
				shootSprite = getSprite("shoot");
			} else {
				shootSprite = getSprite("jump_shoot");
			}
		}
		if (shootAnimTime == 0) {
			shootAnimTime = 0.3f;
			changeSprite(shootSprite, false);
		}
		if (shootSprite == getSprite("shoot") || shootSprite == getSprite("shoot_shield")) {
			frameIndex = 0;
			frameTime = 0;
			animTime = 0;
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

	public void addCoreAmmo(float amount, bool resetCooldown = true, bool forceAdd = false) {
		if (!forceAdd && overheating && amount >= 0) {
			return;
		}
		if (overdrive) {
			addOvedriveAmmo(amount, resetCooldown, forceAdd);
			return;
		}
		coreAmmo += amount;
		if (coreAmmo > coreMaxAmmo) { coreAmmo = coreMaxAmmo; }
		if (coreAmmo < 0) { coreAmmo = 0; }
		if (resetCooldown) {
			resetCoreCooldown();
		}
	}

	public void healCore(float amount) {
		lTankCoreHealAmount = amount;
	}

	public void healShield(float amount) {
		lTankHealShieldAmount = amount;
	}

	public void stopLTankHeal(bool stopShield = true) {
		if (stopShield) {
			lTankHealShieldAmount = 0;
			lTankHealShieldCooldown = 0;
		}
	}
	
	public void drawLTankHealingInner() {
		if (eTankHealAmount <= 0 && lTankHealShieldAmount <= 0 && lTankCoreHealAmount <= 0) return;
		Point topLeft = new Point(pos.x - 8, pos.y - 15 + currentLabelY);

		Global.sprites["menu_ltank"].draw(1, topLeft.x, topLeft.y, 1, 1, null, 1, 1, 1, ZIndex.HUD);
		
		deductLabelY(labelSubtankOffY);
	}

	public void addOvedriveAmmo(float amount, bool resetCooldown = true, bool forceAdd = false) {
		if (!overdrive) {
			return;
		}
		overdriveAmmo += amount;
		if (overdriveAmmo > coreMaxAmmo) { overdriveAmmo = coreMaxAmmo; }
		if (overdriveAmmo < 0) { overdriveAmmo = 0; }
		if (resetCooldown) {
			overdriveAmmoDecreaseCooldown = overdriveAmmoMaxCooldown;
		}
	}

	public override void chargeGfx() {
		if (ownedByLocalPlayer) {
			chargeEffect.stop();
		}
		if (isCharging()) {
			chargeSound.play();
			int level = getChargeLevel();
			var renderGfx = RenderEffectType.ChargeBlue;
			renderGfx = level switch {
				1 => RenderEffectType.ChargeBlue,
				2 => RenderEffectType.ChargePurple,
				3 => RenderEffectType.ChargeGreen,
				_ => RenderEffectType.ChargeBlue,
			};
			addRenderEffect(renderGfx, 3, 5);
			chargeEffect.update(getChargeLevel(), 1);
		}
	}

	public void resetCoreCooldown(float? time = null, bool force = false) {
		if (!force && overheating) {
			return;
		}
		if (time == null) {
			time = coreAmmoMaxCooldown;
		}
		if (coreAmmoDecreaseCooldown < time) {
			coreAmmoDecreaseCooldown = time.Value;
		}
	}

	public int getChargeShotCorePendingAmmo() {
		int ammoUse = 0;
		float[] chargeAmmoTimes = [
			charge1Time / 2,
			charge1Time,
			(charge2Time - charge1Time) / 2 + charge1Time,
			charge2Time,
			(charge3Time - charge2Time) / 2 + charge2Time,
			charge3Time,
		];
		for (int i = 0; i < chargeAmmoTimes.Length; i++) {
			if (chargeTime >= chargeAmmoTimes[i]) {
				ammoUse++;
			} else {
				break;
			}
		}
		return ammoUse;
	}

	public int getChargeShotAmmoUse(int chargeLevel) {
		return chargeLevel switch {
			0 => 0,
			1 => 2,
			2 => 4,
			3 => 6,
			_ => 6
		};
	}

	public bool shieldDashInput() {
		if (Options.main.altSlideInput && !overdrive) {
			return (
				player.input.isPressed(Control.Dash, player) &&
				!player.input.isHeld(Control.Down, player)
			);
		}
		return player.input.isPressed(Control.Dash, player);
	}

	public bool slideInput() {
		if (Options.main.altSlideInput) {
			if (overheating) {
				return player.input.isPressed(Control.Dash, player);
			}
			return (
				player.input.isPressed(Control.Dash, player) &&
				player.input.isHeld(Control.Down, player)
			);
		}
		return (
			player.input.isPressed(Control.Jump, player) &&
			player.input.isHeld(Control.Down, player)
		);
	}

	public bool isShieldFront() {
		if (!ownedByLocalPlayer) {
			return isShieldActive;
		}
		bool canShieldBeActive = false;
		if (shieldCustomState != null) {
			canShieldBeActive = shieldCustomState.Value;
		} else {
			canShieldBeActive = (
				charState.attackCtrl ||
				charState.normalCtrl ||
				charState is BigBangStrikeStart ||
				charState is Hurt { stateFrames: < 2 } ||
				charState is GenericStun { stateFrames: < 2 }
			);
			if (shootAnimTime > 0 || charState is LadderClimb) {
				canShieldBeActive = false;
			}
		}
		return (
			isShieldActive &&
			canShieldBeActive &&
			(shieldHP > 0 || charState is BigBangStrikeStart)
		);
	}
	
	public bool isShieldEquipped() {
		if (!ownedByLocalPlayer) {
			return isShieldActive;
		}
		return (shieldCustomState ?? isShieldActive);
	}

	public override void applyDamage(
		float fDamage, Player? attacker, Actor? actor,
		int? weaponIndex, int? projId
	) {
		if (!ownedByLocalPlayer || fDamage <= 0) {
			return;
		}
		decimal damage = decimal.Parse(fDamage.ToString());
		decimal originalDamage = damage;
		// Do shield checks only if damage exists and a actor too.
		if (damage < 0 || actor == null || attacker == null || player.health <= 0) {
			if (charState is not Hurt { stateFrames: 0 } && player.health > 0) {
				playSound("hit", sendRpc: true);
			}
			base.applyDamage(fDamage, attacker, actor, weaponIndex, projId);
			return;
		}
		// Tracker variables.
		decimal ogShieldHP = shieldHP;
		float oldHealth = player.health;
		bool shieldDamaged = false;
		bool bodyDamaged = false;
		bool backShieldDamaged = false;
		bool shieldPierced = false;
		bool bodyPierced = false;
		int damageReduction = 1;
		bool shieldFront = isShieldFront();
		bool shieldHitFront = (shieldFront && Damager.hitFromFront(this, actor, attacker, projId ?? -1));
		bool shieldHitBack = (
			!shieldFront && Damager.hitFromBehind(this, actor, attacker, projId ?? -1)
			&& charState is not OverheatShutdown and not OverheatShutdownStart and not Recover
		);
		if (projId == (int)BassProjIds.RemoteMineExplosion) {
			if (shieldFront) {
				shieldHitFront = true;
			}
		}
		// Disable shield on any non DOT damage.
		if (shieldHitFront && damage > 0 && !Damager.isDot(projId)) {
			healShieldHPCooldown = 180;
		}
		// Things that apply to both shield variants.
		if (shieldHitBack || shieldHitFront) {
			// In case we did only fractional damage to the shield.
			if (damage % 1 != 0) {
				decimal oldDamage = damage;
				damage = Math.Floor(damage);
				if (shieldHitFront) {
					shieldDamageDebt += oldDamage - damage;
				} else {
					damageDebt += oldDamage - damage;
				}
			}
			while (shieldHitFront && shieldDamageDebt >= 1 && originalDamage > 0) {
				shieldDamageDebt -= 1;
				damage += 1;
			}
			while (shieldHitBack && damageDebt >= 1 && originalDamage > 0) {
				damageDebt -= 1;
				damage += 1;
			}
			// Armor pierce.
			// Remove damage reduction.
			if (Damager.isArmorPiercing(projId)) {
				damageReduction = 0;
				if (shieldHitFront) {
					shieldPierced = true;
				}
				if (shieldHitBack) {
					bodyPierced = true;
				}
			}
		}
		// Shield front block check.
		if (shieldHitFront && damage > 0) {
			shieldDamaged = true;
			stopLTankHeal();
			// 1-2 damage scenario.
			if (damageReduction > 0 && damage <= 2) {
				if (damage <= 1) {
					shieldDamageSavings += damage * (1m/3m);
				} else {
					shieldDamageSavings += damage * 0.25m;
				}
				if (damage < 0) {
					damage = 0;
				}
				if (shieldDamageSavings >= 1) {
					shieldDamageSavings -= damageReduction;
					if (shieldDamageSavings <= 0) { shieldDamageSavings = 0; }
					if (damage >= 2) {
						shieldHP -= damage - 1;
					}
				} else {
					shieldHP -= damage;
				}
				damage = 0;
			}
			// High HP scenario.
			else if (shieldHP + damageReduction >= damage) {
				shieldHP -= damage - damageReduction;
				damage = 0;
			}
			// Low HP scenario.
			else {
				damage -= shieldHP + damageReduction;
				shieldHP = 0;
				shieldDamaged = false;
			}
			if (shieldHP <= 0 && charState is not BigBangStrikeStart) {
				shieldHP = 0;
				shieldDamageDebt = 0;
				isShieldActive = false;
				if (sprite.name.EndsWith("_shield")) {
					changeSprite(sprite.name[..^7], false);
				}
			}
		}
		// Back shield block check.
		else if (shieldHitBack && !bodyPierced && damage > 0) {
			backShieldDamaged = true;
			bodyDamaged = true;
			if (damage <= 1) {
				shieldDamageSavings += damage * 0.5m;
				if (shieldDamageSavings >= 1) {
					damage = 0;
					shieldDamageSavings--;
					if (shieldDamageSavings <= 0) { shieldDamageSavings = 0; }
				}
			} else {
				damage--;
			}
		}
		if (damage > 0) { 
			bodyDamaged = true;
			customDamageDisplayOn = true;
			base.applyDamage(float.Parse(damage.ToString()), attacker, actor, weaponIndex, projId);
			customDamageDisplayOn = false;
			addRenderEffect(RenderEffectType.Hit, 3, 5);
			damage = lastDamageNum;
			if (charState is not Hurt { stateFrames: 0 }) {
				playSound("hit", sendRpc: true);
			}
		} else {
			if (charState is not Hurt { stateFrames: 0 }) {
				playSound("ding", sendRpc: true);
			}
			if (shieldHitBack && !bodyPierced) {
				backShieldDamaged = true;
			}
			if ((originalDamage > 0 || Damager.alwaysAssist(projId)) && attacker != null && weaponIndex != null) {
				damageHistory.Add(new DamageEvent(attacker, weaponIndex.Value, projId, false, Global.time));
			}
		}
		if (originalDamage == 0 && damage == 0) {
			return;
		}
		if (bodyDamaged || shieldHitBack) {
			int fontColor = (int)FontType.RedSmall;
			if (bodyPierced) {
				fontColor = (int)FontType.YellowSmall;
			} else if (backShieldDamaged) {
				fontColor = (int)FontType.OrangeSmall;
			}
			float damageText = float.Parse(damage.ToString());
			addDamageText(damageText, fontColor);
			RPC.addDamageText.sendRpc(attacker.id, netId, damageText, fontColor);
			resetCoreCooldown(coreAmmoDamageCooldown);
		}
		if (shieldDamaged || shieldHitFront) {
			int fontColor = (int)FontType.BlueSmall;
			if (shieldPierced) {
				fontColor = (int)FontType.PurpleSmall;
			}
			float damageText = float.Parse((ogShieldHP - shieldHP).ToString());
			addDamageText(damageText, fontColor);
			RPC.addDamageText.sendRpc(attacker.id, netId, damageText, fontColor);
		}
	}

	public override void aiAttack(Actor? target) {
		if (target == null) {
			return;
		}
		if (grounded) {
			if (shieldHP >= 1 && (shieldHP >= shieldMaxHP || aiActivateShieldOnLand)) {
				isShieldActive = true;
			}
			aiActivateShieldOnLand = false;
		}
		if (AI.trainingBehavior != 0) {
			return;
		}
		Helpers.decrementFrames(ref aiSpecialUseTimer);
		if (!isFacing(target)) {
			if (canCharge() && shootAnimTime == 0) {
				increaseCharge();
			}
			return;
		}
		if (!charState.attackCtrl) {
			return;
		}
		if (aiSpecialUseTimer == 0 &&
			specialWeapon is not StarCrash && canShootSpecial() &&
			coreMaxAmmo - coreAmmo > specialWeapon.getAmmoUsage(0) * 2
		) {
			aiSpecialUseTimer = 60;
			shootSpecial(0);
			return;
		}
		if (canShoot() && lemonCooldown == 0) {
			shoot(getChargeLevel());
			return;
		}
	}

	public override void onFlagPickup(Flag flag) {
		base.onFlagPickup(flag);
		stopCharge();
		if (starCrash != null) {
			delinkStarCrash();
		}
	}

	public override List<ShaderWrapper> getShaders() {
		List<ShaderWrapper> shaders = base.getShaders();

		if (player.bluesScarfShader != null && !overdrive && !overheating
			&& (!isBreakMan || !Options.main.fastShaders)
		) {
			ShaderWrapper palette = player.bluesScarfShader;
			palette.SetUniform("palette", specialWeaponIndex + 1);
			palette.SetUniform("paletteTexture", Global.textures["blues_palette_texture"]);
			shaders.Add(palette);
		}
		if (player.breakManShader != null && isBreakMan) {
			ShaderWrapper palette = player.breakManShader;
			palette.SetUniform("palette", 1);
			palette.SetUniform("paletteTexture", Global.textures["blues_hyperpalette"]);
			shaders.Add(palette);
		}
		return shaders;
	}

	public override Collider? getGlobalCollider() {
		(float xSize, float ySize) = getGlobalColliderSize();
		float xOffset = 0;
		if (isShieldFront()) {
			xSize += 8;
			xOffset = 4;
		}
		return new Collider(
			new Rect(0, 0, xSize, ySize).getPoints(),
			false, this, false, false, HitboxFlag.Hurtbox,
			new Point(xOffset, 0)
		);
	}

	public override (float, float) getGlobalColliderSize() {
		return sprite.name switch{ 
			"blues_slide" => (34, 22),
			"blues_dash" or "blues_dash_shield" => (34, 30),
			_ => (24, 30)
		};
	}

	public override (float, float) getTerrainColliderSize() {
		return sprite.name switch{ 
			"blues_slide" => (34, 12),
			"blues_dash" or "blues_dash_shield" => (34, 30),
			_ => (24, 30)
		};
	}

	public override void render(float x, float y) {
		base.render(x,y);

		float pAmmo = getChargeShotCorePendingAmmo();
		if (player.isMainPlayer && (coreAmmo > 0 || pAmmo > 0) && Options.main.coreHeatDisplay >= 1) {
			float corePct = Helpers.clamp01((coreMaxAmmo - coreAmmo) / coreMaxAmmo);
			corePct = -corePct + 1;
			
			float pendAmmo = coreAmmo + pAmmo;
			if (pendAmmo > coreMaxAmmo) {
				pendAmmo = MathInt.Floor(coreMaxAmmo - coreAmmo);
			}
			float pendPct = Helpers.clamp01((coreMaxAmmo - pendAmmo) / coreMaxAmmo);
			pendPct = -pendPct + 1;
			
			float sy = -27;
			float sx = 20;
			if (xDir == -1) sx = 90 - 20;
			drawCoreHeat(corePct, pendPct, sx, sy);
		}
	}

	public void drawCoreHeat(float corePct, float pendPct, float sx, float sy) {
		float coreBarInnerWidth = 30;
		Color color = Color.Red;
		Color color2 = Color.Yellow;
		float width = Helpers.clampMax(MathF.Ceiling(coreBarInnerWidth * corePct), coreBarInnerWidth);
		float width2 = Helpers.clampMax(MathF.Ceiling(coreBarInnerWidth * pendPct), coreBarInnerWidth);

		DrawWrappers.DrawRect(pos.x - 47 + sx, pos.y - 16 + sy, pos.x - 42 + sx, pos.y + 16 + sy, true, Color.Black, 0, ZIndex.HUD - 1, outlineColor: Color.White);
		DrawWrappers.DrawRect(pos.x - 46 + sx, pos.y + 15 - width2 + sy, pos.x - 43 + sx, pos.y + 15 + sy, true, color2, 0, ZIndex.HUD - 1);
		DrawWrappers.DrawRect(pos.x - 46 + sx, pos.y + 15 - width + sy, pos.x - 43 + sx, pos.y + 15 + sy, true, color, 0, ZIndex.HUD - 1);
	}

	public override void renderHUD(Point offset, GameMode.HUDHealthPosition position) {
		base.renderHUD(offset, position);
	}

	public override void renderLifebar(Point offset, GameMode.HUDHealthPosition position) {
		offset = offset.addxy(0, 17);
		base.renderLifebar(offset, position);

		Point hudHealthPosition = GameMode.getHUDHealthPosition(position, true);
		float baseX = hudHealthPosition.x + offset.x;
		float baseY = hudHealthPosition.y + offset.y;

		renderShieldBar(offset, position);
	}

	public override (string, int) getBaseHpSprite() {
		return ("hud_health_base", 1);
	}

	public override (string, int) getTopHpSprite() {
		return ("hud_health_top", 1);
	}

	public void renderShieldBar(Point offset, GameMode.HUDHealthPosition position) {
		decimal damageSavings = 0;
		if (shieldHP > 0) {
			damageSavings = MathInt.Floor(this.shieldDamageSavings);
		}
		Point hudHealthPosition = GameMode.getHUDHealthPosition(position, true);
		float baseX = hudHealthPosition.x + offset.x;
		float baseY = hudHealthPosition.y + offset.y;
		decimal modifier = (decimal)Player.getHealthModifier();
		decimal maxHP = maxHealth / modifier;
		baseY += (float)(-21 - maxHP * 2);
		decimal maxShield = shieldMaxHP / modifier;
		decimal curShield = Math.Floor(shieldHP) / modifier;
		decimal ceilShield = Math.Ceiling(shieldHP / modifier);
		decimal floatShield = health / modifier;
		float fhpAlpha = (float)(floatShield - curShield);
		decimal savings = curShield + Math.Ceiling((Math.Floor(damageSavings) / modifier));

		for (int i = 0; i < Math.Ceiling(maxShield); i++) {
			// Draw HP
			if (i < curShield) {
				Global.sprites["hud_weapon_full_blues"].drawToHUD(3, baseX, baseY);
			}
			else if (i < savings) {
				Global.sprites["hud_weapon_full_blues"].drawToHUD(2, baseX, baseY);
			}
			else {
				Global.sprites["hud_health_empty"].drawToHUD(0, baseX, baseY);
				if (i < ceilShield) {
					Global.sprites["hud_weapon_full_blues"].drawToHUD(3, baseX, baseY, fhpAlpha);
				}
			}
			baseY -= 2;
		}
		Global.sprites["hud_health_top"].drawToHUD(0, baseX, baseY);
	}

	public override void renderAmmo(Point offset, GameMode.HUDHealthPosition position, Weapon? weaponOverride = null) {
		Point hudHealthPosition = GameMode.getHUDHealthPosition(position, false);
		float baseX = hudHealthPosition.x + offset.x;
		float baseY = hudHealthPosition.y + offset.y + 17;

		int coreAmmoColor = 0;
		int overdriveColor = 1;
		if ((overheating || overdrive) && Global.frameCount % 6 >= 3) {
			coreAmmoColor = 2;
		}
		if (Options.main.coreHeatDisplay == 1) return;

		GameMode.renderAmmo(
			baseX, baseY, -2, coreAmmoColor, MathF.Ceiling(coreAmmo),
			maxAmmo: coreMaxAmmo, barSprite: "hud_weapon_full_blues"
		);
		if (overdrive) {
			int yPos = MathInt.Ceiling(baseY - 16);
			overdriveColor = 1;
			if (Global.frameCount % 6 >= 3) {
				overdriveColor = 3;
			}
			float alpha = 1;
			if (Global.frameCount % 4 >= 2) {
				alpha = 0.25f;
			}
			for (var i = 0; i < overdriveAmmo; i++) {
				if (alpha < 1 && i > coreAmmo - 1) {
					Global.sprites["hud_weapon_full_blues"].drawToHUD(coreAmmoColor, baseX, yPos);
				}
				Global.sprites["hud_weapon_full_blues"].drawToHUD(overdriveColor, baseX, yPos, alpha);
				yPos -= 2;
			}
		}
		if (!overheating && ownedByLocalPlayer) {
			int baseAmmo = MathInt.Floor(coreAmmo);
			int baseColor = 2;
			int filledColor = 0;
			if (overdrive) {
				baseAmmo = MathInt.Floor(overdriveAmmo);
				baseColor = 3;
				filledColor = 1;
			}
			int yPos = MathInt.Ceiling(baseY - 16 - MathF.Ceiling(baseAmmo) * 2);

			int ammoAmmount = getChargeShotCorePendingAmmo();
			int actualUse = getChargeShotAmmoUse(getChargeLevel());
			if (ammoAmmount + baseAmmo > coreMaxAmmo) {
				ammoAmmount = MathInt.Floor(coreMaxAmmo - baseAmmo);
			}
			for (var i = 0; i < ammoAmmount; i++) {
				int color = baseColor;
				if (i < actualUse) {
					color = filledColor;
				}
				if (overdrive && i > coreAmmo - 1) {
					Global.sprites["hud_weapon_full_blues"].drawToHUD(coreAmmoColor, baseX, yPos);
				}
				Global.sprites["hud_weapon_full_blues"].drawToHUD(color, baseX, yPos, 0.75f);
				yPos -= 2;
			}
		}
	}

	public override void renderBuffs(Point offset, GameMode.HUDHealthPosition position) {
		offset.y += 17;
		base.renderBuffs(offset, position);
		int drawDir = 1;
		if (position == GameMode.HUDHealthPosition.Right) {
			drawDir = -1;
		}
		Point drawPos = GameMode.getHUDBuffPosition(position) + offset;

		if (redStrikeCooldown > 0) {
			drawBuff(drawPos, redStrikeCooldown / 240, "hud_blues_weapon_icon", 3);
			secondBarOffset += 18 * drawDir;
			drawPos.x += 18 * drawDir;
		}
	}

	public override List<byte> getCustomActorNetData() {
		// Get base arguments.
		List<byte> customData = base.getCustomActorNetData() ?? new();

		// Per-character data.
		int weaponIndex = specialWeaponIndex;
		customData.Add((byte)MathInt.Floor(coreAmmo));
		customData.Add((byte)MathInt.Ceiling(shieldHP));
		customData.Add((byte)getChargeLevel());
		bool[] flags = [
			isShieldFront(),
			overheating,
			isBreakMan,
			overdrive
		];
		customData.Add(Helpers.boolArrayToByte(flags));

		return customData;
	}

	public override void updateCustomActorNetData(byte[] data) {
		// Update base arguments.
		base.updateCustomActorNetData(data);
		data = data[data[0]..];

		// Per-character data.
		coreAmmo = data[0];
		shieldHP = data[1];
		netChargeLevel = data[2];
		if (netChargeLevel == 0) {
			stopCharge();
		}

		bool[] flags = Helpers.byteToBoolArray(data[3]);
		isShieldActive = flags[0];
		overheating = flags[1];
		isBreakMan = flags[2];
		overdrive = flags[3];
	}
}
