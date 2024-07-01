using System;
using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;

public class Blues : Character {
	public float lemonCooldown;
	public float[] unchargedLemonCooldown = new float[3];
	public float coreMaxAmmo = 28;
	public float coreAmmo;
	public float coreAmmoMaxCooldown = 60;
	public float coreAmmoDamageCooldown = 120;
	public float coreAmmoDecreaseCooldown;
	public bool isShieldActive = true;
	public bool overheating;
	public float overheatEffectTime;
	public decimal shieldHP = 20;
	public int shieldMaxHP = 20;
	public float healShieldHPCooldown = 15;
	public decimal shieldDamageDebt;
	public bool starCrashActive;

	// Special weapon stuff
	public Weapon specialWeapon;
	public StarCrashProj? starCrash;
	public HardKnuckleProj? hardKnuckleProj;

	// AI variables.
	public float aiSpecialUseTimer = 0;

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

		specialWeapon = protomanLoadout switch {
			0 => new NeedleCannon(),
			1 => new HardKnuckle(),
			2 => new SearchSnake(),
			3 => new SparkShock(),
			4 => new PowerStone(),
			5 => new WaterWave(),
			6 => new GyroAttack(),
			7 => new StarCrash(),
			_ => new PowerStone(),
		};
	}

	public override bool canAddAmmo() {
		return false;//(coreAmmo > 0);
	}

	public override float getRunSpeed() {
		float runSpeed = Physics.WalkSpeed;
		if (overheating) {
			runSpeed *= 0.5f;
			if (isShieldActive) {
				runSpeed *= 0.9375f;
			}
		}
		else if (isShieldActive) {
			runSpeed *= 0.75f;
		}
		return runSpeed * getRunDebuffs();
	}

	public float getShieldDashSpeed() {
		float runSpeed = 3.25f * 60;
		if (overheating) {
			runSpeed *= 0.5f;
		}
		else if (isShieldActive) {
			runSpeed = 2.75f * 60;
		}
		return runSpeed * getRunDebuffs();
	}

	public float getSlideSpeed() {
		float runSpeed = 3f * 60;
		if (overheating) {
			runSpeed *= 0.5f;
		}
		return runSpeed * getRunDebuffs();
	}

	public override float getJumpPower() {
		float jumpSpeed = Physics.JumpSpeed;
		if (overheating) {
			jumpSpeed *= 0.75f;
		}
		else if (isShieldActive) {
			jumpSpeed *= 0.85f;
		}
		return jumpSpeed * getJumpModifier();
	}

	public override bool canTurn() {
		if (charState is BluesSpreadShoot or HardKnuckleShoot) {
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
		if (overheating || charState is ProtoStrike) {
			return false;
		}
		return base.canCharge();
	}

	public bool canShieldDash() {
		return (
			grounded &&
			charState is not ShieldDash &&
			!overheating && rootTime <= 0
		);
	}

	public bool canSlide() {
		return (
			grounded && vel.y >= 0 &&
			charState is not BluesSlide and not ShieldDash &&
			!overheating && rootTime <= 0
		);
	}

	public bool canUseShield() {
		if (!charState.normalCtrl || charState is Slide) {
			return false;
		}
		return true;
	}

	public bool canShootSpecial() {
		if (isCharging() || overheating || specialWeapon.shootCooldown > 0 ||
		 !specialWeapon.canShoot(0, this) || invulnTime > 0) {
			return false;
		}
		return true;
	}

	public bool canUseBigBangStrike() {
		return grounded && overheating;
	}

	public void destroyStarCrash() {
		if (starCrash != null) {
			starCrash.destroySelf();
		}
		starCrash = null;
		gravityModifier = 1;
		starCrashActive = false;

		if (specialWeapon is StarCrash) {
			specialWeapon.shootCooldown = specialWeapon.fireRateFrames;
		}
	}

	public override string getSprite(string spriteName) {
		return "blues_" + spriteName;
	}

	public override void changeSprite(string spriteName, bool resetFrame) {
		if (isShieldActive && spriteName == getSprite("idle") && getChargeLevel() >= 2) {
			spriteName = getSprite("idle_chargeshield");
		} else if (isShieldActive && Global.sprites.ContainsKey(spriteName + "_shield")) {
			spriteName += "_shield";
		}
		List<Trail>? trails = sprite?.lastFiveTrailDraws;
		base.changeSprite(spriteName, resetFrame);
		if (trails != null && sprite != null) {
			sprite.lastFiveTrailDraws = trails;
		}
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
		proj.owningActor = this;

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

	public override void update() {
		base.update();
		// For non-local players.
		if (overheating) {
			addRenderEffect(RenderEffectType.ChargeOrange, 0.033333f, 0.1f);
		}
		if (!ownedByLocalPlayer) return;

		// Cooldowns.
		Helpers.decrementFrames(ref lemonCooldown);
		Helpers.decrementFrames(ref healShieldHPCooldown);
		specialWeapon.update();
		for (int i = 0; i < unchargedLemonCooldown.Length; i++) {
			Helpers.decrementFrames(ref unchargedLemonCooldown[i]);
		}

		// Shield HP.
		if (healShieldHPCooldown <= 0 && shieldHP < shieldMaxHP) {
			playSound("heal");
			shieldHP++;
			healShieldHPCooldown = 6;
			if (shieldHP >= shieldMaxHP) {
				shieldHP = shieldMaxHP;
			}
		}
		if (coreAmmo >= coreMaxAmmo && !overheating) {
			overheating = true;
			setHurt(-xDir, 12, false);
			playSound("danger_wrap_explosion", sendRpc: true);
			stopCharge();
		}
		if (isCharging() && chargeTime <= charge3Time + 10) {
			if (coreAmmoDecreaseCooldown < coreAmmoMaxCooldown) {
				coreAmmoDecreaseCooldown = coreAmmoMaxCooldown;
			}
		} else {
			Helpers.decrementFrames(ref coreAmmoDecreaseCooldown);
		}
		if (coreAmmoDecreaseCooldown <= 0) {
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
		// For the shooting animation.
		if (shootAnimTime > 0) {
			shootAnimTime -= Global.spf;
			if (shootAnimTime <= 0) {
				shootAnimTime = 0;
				if (sprite.name.EndsWith("_shoot") || sprite.name.EndsWith("_shoot_shield")) {
					changeSpriteFromName(charState.defaultSprite, false);
					if (charState is WallSlide) {
						frameIndex = sprite.frames.Count - 1;
					}
				}
			}
		}
		// Shoot logic.
		chargeLogic(shoot);
		if (isShieldActive && getChargeLevel() >= 2 && sprite.name == getSprite("idle_shield")) {
			changeSpriteFromName("idle_chargeshield", true);
		}

		// Overheating effects-
		if (overheating) {
			overheatEffectTime += Global.speedMul;
			if (overheatEffectTime >= 3) {
				overheatEffectTime = 0;
				Point burnPos = pos.addxy(xDir * 2, -15);

				Anim tempAnim = new Anim(burnPos.addRand(14, 15), "dust", 1, null, true, host: this);
				tempAnim.vel.y = -120;
				tempAnim.addRenderEffect(RenderEffectType.ChargeOrange, 0.033333f, 2);
			}
		}
	}

	public override void onFlinchOrStun(CharState newState) {
		coreAmmoDecreaseCooldown = coreAmmoDamageCooldown;
		if (newState is Hurt) addCoreAmmo(3);
		base.onFlinchOrStun(newState);
	}

	public override bool normalCtrl() {
		// For keeping track of shield change.
		bool lastShieldMode = isShieldActive;
		// Shield switch.
		if (shieldHP > 0 && grounded && vel.y >= 0 && shootAnimTime <= 0 && canUseShield()) {
			if (Options.main.protoShieldHold) {
				isShieldActive = player.input.isWeaponLeftOrRightHeld(player);
			} else {
				if (player.input.isWeaponLeftOrRightPressed(player)) {
					isShieldActive = !isShieldActive;
				}
			}
		}
		// Change sprite is shield mode changed.
		if (lastShieldMode != isShieldActive) {
			if (shootTime == 0 && charState is Idle idleState) {
				idleState.transitionSprite = getSprite("idle_swap");
				if (isShieldActive) {
					idleState.transitionSprite += "_shield";
				}
				idleState.sprite = idleState.transitionSprite;
				changeSprite(idleState.sprite, true);
			}
			else if (!isShieldActive || shieldHP <= 0) {
				isShieldActive = false;
				if (sprite.name.EndsWith("_shield")) {
					changeSprite(sprite.name[..^7], false);
				}
				if (sprite.name == getSprite("idle_chargeshield")) {
					changeSpriteFromName("idle", true);
				}
			} else {
				isShieldActive = true;
				if (!sprite.name.EndsWith("_shield")) {
					changeSprite(sprite.name + "_shield", false);
				}
			}
		}
		if (player.input.isPressed(Control.Jump, player) &&
			player.input.isHeld(Control.Down, player) &&
			canSlide()
		) {
			changeState(new BluesSlide(), true);
			return true;
		}
		if (player.input.isPressed(Control.Dash, player) && canShieldDash()) {
			addCoreAmmo(2);
			changeState(new ShieldDash(), true);
			return true;
		}
		return base.normalCtrl();
	}

	public override bool attackCtrl() {
		bool shootPressed = player.input.isPressed(Control.Shoot, player);
		bool specialPressed = player.input.isPressed(Control.Special1, player);
		if (!overheating && specialWeapon is NeedleCannon) {
			specialPressed = player.input.isHeld(Control.Special1, player);
		}
		bool downHeld = player.input.isHeld(Control.Down, player);

		if (specialPressed) {
			if (canShootSpecial()) {
				shootSpecial(0);
				return true;
			} else if (canUseBigBangStrike()) {
				changeState(new BigBangStrikeStart(), true);
				return true;
			}
		}

		if (shootPressed && downHeld && !overheating && !grounded) {
			changeState(new BluesSpreadShoot(), true);
			return true;
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
		int lemonNum = -1;
		if (chargeLevel < 2) {
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
			var lemon = new ProtoBusterProj(
				shootPos, xDir, player, player.getNextActorNetId(), rpc: true
			);
			playSound("buster", sendRpc: true);
			//resetCoreCooldown(45);
			lemonCooldown = 12;
			unchargedLemonCooldown[lemonNum] = 50;
			if (oldShootAnimTime <= 0.25f) {
				shootAnimTime = 0.25f;
			}
		} else if (chargeLevel == 1) {
			new ProtoBusterLv2Proj(
				shootPos, xDir, player, player.getNextActorNetId(), true
			);
			addCoreAmmo(getChargeShotAmmoUse(2));
			playSound("buster2", sendRpc: true);
			lemonCooldown = 12;
		} else if (chargeLevel == 2) {
			new ProtoBusterLv3Proj(
				shootPos, xDir, player, player.getNextActorNetId(), true
			);
			addCoreAmmo(getChargeShotAmmoUse(3));
			playSound("buster3", sendRpc: true);
			lemonCooldown = 12;
		} else {
			if (player.input.isHeld(Control.Up, player)) {
				addCoreAmmo(4);
				changeState(new ProtoStrike(), true);
			} else {
				new ProtoBusterLv4Proj(
					shootPos, xDir, player, player.getNextActorNetId(), true
				);
				addCoreAmmo(getChargeShotAmmoUse(3));
				playSound("buster3", sendRpc: true);
				lemonCooldown = 12;
			}
		}
	}

	public void shootSpecial(int chargeLevel) {
		if (specialWeapon == null) {
			return;
		}
		if (!charState.attackCtrl && !charState.invincible) {
			changeToIdleOrFall();
		}
		// Shoot anim and vars.
		if (!specialWeapon.hasCustomAnim) {
			setShootAnim();
		}
		Point shootPos = getShootPos();
		int xDir = getShootXDir();

		specialWeapon.shootCooldown = specialWeapon.fireRateFrames;
		specialWeapon.shoot(this, chargeLevel);
		addCoreAmmo(specialWeapon.getAmmoUsage(chargeLevel));
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
		coreAmmo += amount;
		if (coreAmmo > coreMaxAmmo) { coreAmmo = coreMaxAmmo; }
		if (coreAmmo < 0) { coreAmmo = 0; }
		if (resetCooldown) {
			resetCoreCooldown();
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
				2 => RenderEffectType.ChargePink,
				3 => RenderEffectType.ChargeGreen,
			};
			addRenderEffect(renderGfx, 0.033333f, 0.1f);
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

	public bool isShieldFront() {
		if (!ownedByLocalPlayer) {
			return isShieldActive;
		}
		return (
			(isShieldActive && charState is not BluesSlide) &&
			shieldHP > 0 &&
			shootAnimTime == 0 &&
			charState is not Hurt { stateFrames: not 0 }
		);
	}

	public override void applyDamage(float fDamage, Player? attacker, Actor? actor, int? weaponIndex, int? projId) {
		if (!ownedByLocalPlayer) return;
		decimal damage = decimal.Parse(fDamage.ToString());
		// Disable shield on any damage.
		if (damage > 0) {
			healShieldHPCooldown = 180;
		}
		// Do shield checks only if damage exists and a actor too.
		if (actor == null || attacker == null) {
			base.applyDamage(fDamage, attacker, actor, weaponIndex, projId);
			return;
		}
		// Tracker variables.
		decimal ogShieldHP = shieldHP;
		float oldHealth = player.health;
		bool fullyBlocked = false;
		bool shieldBlocked = false;
		// Shield front block check.
		if (isShieldFront() && Damager.hitFromFront(this, actor, attacker, projId ?? -1)) {
			shieldBlocked = true;
			// 1 damage scenario.
			// Reduce damage only 50% of the time.
			if (damage < 2) {
				shieldDamageDebt += damage / 2m;
				damage = 0;
				if (shieldDamageDebt >= 1) {
					shieldDamageDebt--;
					shieldHP--;
				} else {
					fullyBlocked = true;
				}
			}
			// High HP scenario.
			else if (shieldHP + 1 >= damage) {
				shieldHP -= damage - 1;
				if (shieldHP <= 0) {
					shieldHP = 0;
				}
				damage = 0;
			}
			// Low HP scenario.
			else {
				damage -= shieldHP + 1;
				shieldHP = 0;
				shieldBlocked = false;
			}
			if (shieldHP <= 0) {
				isShieldActive = false;
				if (sprite.name.EndsWith("_shield")) {
					changeSprite(sprite.name[..^7], false);
				}
				if (sprite.name == getSprite("idle_chargeshield")) {
					changeSpriteFromName("idle", true);
				}
			}
		}
		// Back shield block check.
		else if (!isShieldFront() && Damager.hitFromBehind(this, actor, attacker, projId ?? -1)) {
			shieldBlocked = true;
			if (damage < 2) {
				shieldDamageDebt += damage / 2m;
				damage = 0;
				if (shieldDamageDebt >= 1) {
					shieldDamageDebt--;
					damage = 1;
				} else {
					fullyBlocked = true;
				}
			} else {
				damage--;
			}
		}
		if (damage > 0) {
			base.applyDamage(float.Parse(damage.ToString()), attacker, actor, weaponIndex, projId);
			addRenderEffect(RenderEffectType.Hit, 0.05f, 0.1f);
			playSound("hit", sendRpc: true);
		} else {
			playSound("ding", sendRpc: true);
		}
		if (fullyBlocked) {
			addDamageText("0", (int)FontType.Blue);
			RPC.addDamageText.sendRpc(attacker.id, netId, 0);
			return;
		}
		if (oldHealth > player.health) {
			int fontColor = (int)FontType.Red;
			if (shieldBlocked) {
				fontColor = (int)FontType.Blue;
			}
			string damageText = (oldHealth - player.health).ToString();
			addDamageText(damageText, fontColor);
			RPC.addDamageText.sendRpc(attacker.id, netId, float.Parse(damageText));
			//addCoreAmmo(MathInt.Ceiling(oldHealth - player.health), false);
			if (!overheating) {
				coreAmmoDecreaseCooldown = coreAmmoDamageCooldown;
			}
		}
		if (ogShieldHP > shieldHP) {
			string damageText = (ogShieldHP - shieldHP).ToString();
			addDamageText(damageText, (int)FontType.Blue);
			RPC.addDamageText.sendRpc(attacker.id, netId, float.Parse(damageText));
		}
	}

	public override void aiAttack(Actor target) {
		if (AI.trainingBehavior != 0) {
			return;
		}
		Helpers.decrementFrames(ref aiSpecialUseTimer);
		if (!isFacing(target)) {
			if (charState.normalCtrl && grounded) {
				isShieldActive = false;
			}
			if (canCharge() && shootAnimTime == 0) {
				increaseCharge();
			}
			return;
		}
		if (!charState.attackCtrl) {
			return;
		}
		if (charState.normalCtrl && grounded) {
			isShieldActive = true;
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
}
