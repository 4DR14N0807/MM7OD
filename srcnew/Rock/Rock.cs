using System;
using System.Collections.Generic;
using System.Linq;


namespace MMXOnline;

public class Rock : Character {

	public float lemonTime;
	public int lemons;
	public float weaponCooldown;
	public List<JunkShieldProj?> junkShieldProjs = new();
	public LoopingSound? junkShieldSound;
	public ScorchWheelSpawn? sWellSpawn;
	public ScorchWheelProj? sWell;
	public UnderwaterScorchWheelProj? sWellU;
	public UnderwaterScorchWheelProj? underwaterScorchWheel;
	public Projectile? sWheel;
	public SARocketPunchProj? saRocketPunchProj;
	public bool armless;
	public ChargeEffect noiseCrushEffect;
	public bool hasChargedNoiseCrush = false;
	public float noiseCrushAnimTime;
	public LoopingSound? chargedNoiseCrushSound;
	public bool usedDoubleJump;
	public bool boughtSuperAdaptorOnce;
	public float arrowSlashCooldown;
	public float legBreakerCooldown;
	public float timeSinceLastShoot;
	public bool isSlideColliding;
	public Rush? rush;
	public RushWeapon rushWeapon;
	public int rushWeaponIndex;
	public int RushSearchCost = 5;
	public bool hasSuperAdaptor;
	public const int SuperAdaptorCost = 75;

	// AI Stuff.
	public float aiWeaponSwitchCooldown = 120;

	public Rock(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer,
		bool isWarpIn = true
	) : base(
		player, x, y, xDir, isVisible, netId, ownedByLocalPlayer, isWarpIn, false, false
	) {
		charId = CharIds.Rock;
		weapons = RockLoadoutSetup.getLoadout(player);

		spriteToCollider["sa_activate_air"] = null;
		spriteToCollider["sa_activate"] = null;
		spriteToCollider["sa_activate_end"] = null;
		spriteToCollider["sa_activate_end_air"] = null;

		charge1Time = 40;
		charge2Time = 80;
		var rl = player.loadout.rockLoadout.rushLoadout;
		rushWeaponIndex = rl;
	
		/* rushWeapon = rl switch {
			0 => new RushCoilWeapon(),
			1 => new RushJetWeapon(),
			_ => new RushSearchWeapon(),
		}; */
		rushWeapon = new RushWeapon();
		player.weapons.Add(rushWeapon);

		noiseCrushEffect = new ChargeEffect();
		noiseCrushEffect.character = this;
	}

	public override void update() {
		base.update();

		if (!ownedByLocalPlayer) return;

		Helpers.decrementFrames(ref lemonTime);
		Helpers.decrementFrames(ref arrowSlashCooldown);
		Helpers.decrementFrames(ref legBreakerCooldown);
		Helpers.decrementFrames(ref weaponCooldown);
		armless = saRocketPunchProj != null;

		timeSinceLastShoot++;
		if (timeSinceLastShoot >= 30) lemons = 0;

		if (currentWeapon?.ammo >= currentWeapon?.maxAmmo) {
			weaponHealAmount = 0;
		}

		if (player.weapon is not NoiseCrush) hasChargedNoiseCrush = false;

		if (hasChargedNoiseCrush) {
			if (chargedNoiseCrushSound == null) {	
				if (!isCharging()) chargedNoiseCrushSound = new LoopingSound("charge_start", "charge_loop", this);
			} else chargedNoiseCrushSound.play();
		} else if (chargedNoiseCrushSound != null) {
			chargedNoiseCrushSound.stop();
			chargedNoiseCrushSound = null!;
		}

		//Junk Shield soundloop.
		if (junkShieldProjs.Count > 0) {
			if (junkShieldSound == null) {
				junkShieldSound = new LoopingSound("charge_start", "charge_loop", this);
			} else junkShieldSound.play();

		} else if (junkShieldSound != null) {
			junkShieldSound.stop();
			junkShieldSound = null!;
		}

		// For the shooting animation.
		if (isCharging()) {
			if (charState.attackCtrl && !sprite.name.EndsWith("_shoot") && charState is not LadderClimb) {
				changeSpriteFromName(charState.shootSprite, false);
			}
		} else if (shootAnimTime > 0 && !armless) {
			shootAnimTime -= Global.spf;
			if (shootAnimTime <= 0) {
				shootAnimTime = 0;
				if (sprite.name.EndsWith("_shoot")) {
					changeSpriteFromName(charState.defaultSprite, false);
				}
			}
		}
		player.changeWeaponControls();
		
		// Shoot logic.
		chargeLogic(shoot);

		quickAdaptorUpgrade();
	}

	public override bool normalCtrl() {
		bool slidePressed = player.dashPressed(out string slideControl);
		bool jumpPressed = player.input.isPressed(Control.Jump, player);

		if (slidePressed && canSlide() && charState is not Slide) {
			changeState(new Slide(slideControl), true);
			return true;
		}

		if (jumpPressed && !grounded && hasSuperAdaptor && !usedDoubleJump && flag == null) {
			changeState(new RockDoubleJump(), true);
			usedDoubleJump = true;
			return true;
		}

		if (grounded) usedDoubleJump = false;

		return base.normalCtrl();
	}

	public override bool attackCtrl() {
		bool shootPressed = player.input.isPressed(Control.Shoot, player);
		bool specialPressed = player.input.isPressed(Control.Special1, player);
		bool downHeld = player.input.isHeld(Control.Down, player);
		bool slidePressed = player.dashPressed(out string slideControl);
		bool arrowSlashInput = player.input.checkHadoken(player, xDir, Control.Shoot);

		if (specialPressed && canCallRush(0)) {
			rushWeapon.shoot(this, 0);
			return true;
		}

		if (hasSuperAdaptor) {
			if (slidePressed && downHeld && legBreakerCooldown <= 0 && canSlide()) {
				changeState(new LegBreakerState(slideControl), true);
				legBreakerCooldown = 90f;
				return true;
			}

			if (specialPressed && arrowSlashCooldown <= 0 && charState is not LadderClimb) {
				changeState(new SAArrowSlashState(), true);
				arrowSlashCooldown = 90f;
				return true;
			}
		}

		if (!isCharging()) {
			if (shootPressed) {
				if (weaponCooldown <= 0) {
					shoot(0);
					return true;
				}
			}
		}
		return base.attackCtrl();
	}

	public void shoot(int chargeLevel) {
		if (currentWeapon?.canShoot(chargeLevel, player) == false) return;
		if (!canShoot()) return;
		if (!charState.attackCtrl && !charState.invincible || charState is Slide) {
			changeToIdleOrFall();
		}
		// Shoot anim and vars.
		float oldShootAnimTime = shootAnimTime;
		
		if (currentWeapon?.hasCustomAnim == false) setShootAnim();

		int chargedNS = hasChargedNoiseCrush ? 1 : 0;
		
		weaponCooldown = currentWeapon?.fireRate ?? 0;
		currentWeapon?.shoot(this, chargeLevel, chargedNS);
		currentWeapon?.addAmmo(-currentWeapon?.getAmmoUsage(chargeLevel) ?? 0, player);
		if (oldShootAnimTime <= 0.25f && currentWeapon?.hasCustomAnim == false) {
			shootAnimTime = 0.25f;
		}
		stopCharge();
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
	
		changeSprite(shootSprite, false);
		
		if (shootSprite == getSprite("shoot")) {
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

	public void quickAdaptorUpgrade() {
		if (!player.input.isHeld(Control.Special2, player)) {
			hyperProgress = 0;
			return;
		}
		if (hasSuperAdaptor) {
			hyperProgress = 0;
			return;
		}
		if (charState is CallDownRush) {
			hyperProgress = 0;
			return;
		}
		if (player.health <= 0) {
			hyperProgress = 0;
			return;
		}
		if (!(charState is WarpIn) && canGoSuperAdaptor()) {

			if (!boughtSuperAdaptorOnce) {
				player.currency -= SuperAdaptorCost;
				boughtSuperAdaptorOnce = true;
			}
			changeState(new CallDownRush(), true);

			return;
		}
		if (hyperProgress < 1) {
			return;
		}
		hyperProgress = 0;
	}

	public override void render(float x, float y) {
		base.render(x, y);

		if (hasChargedNoiseCrush) {
			drawChargedNoiseCrush(x, y);
		}
	}

	public void drawChargedNoiseCrush(float x, float y) {
		addRenderEffect(RenderEffectType.NCrushCharge, 0.05f, 0.1f); 
		noiseCrushEffect.character = this;
		noiseCrushEffect.update(2, 2);
		noiseCrushEffect.render(getCenterPos());
	}

	public override bool canMove() {
		if (charState is CallDownRush) return false;

		return base.canMove();
	}

	public override bool canJump() {
		if (isSlideColliding && charState is Slide) return false;

		return base.canJump();
	}

	public override bool canDash() {
		return false;
	}

	public bool canSlide() {
		if (!grounded) return false;
		if (flag != null) return false;
		if (charState is CallDownRush) return false;
		if (charState is SAArrowSlashState) return false;
		if (charState is LegBreakerState) return false;
		if (isBurnState) return false;
		if (rootTime > 0) return false;
		return true;
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

	public override string getSprite(string spriteName) {
		return "rock_" + spriteName;
	}

	public override bool canShoot() {
		if (sWell != null) return false;
		if (sWellSpawn != null) return false;
		if (sWellU != null) return false;
		if (charState is Slide)
			return (player.weapon is RockBuster || player.weapon is WildCoil) && getChargeLevel() == 2;
		if (charState is CallDownRush) return false;
		if (charState is SAArrowSlashState) return false;
		if (isInvulnerableAttack()) return false;
		if (saRocketPunchProj != null) return false;

		return base.canShoot() && weaponCooldown <= 0;
	}

	public override bool canClimbLadder() {
		if (armless) return false;

		return base.canClimbLadder();
	}

	public bool canCallRush(int type) {
		if (isInvulnerableAttack() ||
			hasSuperAdaptor ||
			(type == 2 && player.currency < RushSearchCost) ||
			flag != null) return false;
		
		return true;
	}


	public override bool canChangeWeapons() {
		return base.canChangeWeapons();
	}
	public override bool canCharge() {
		if (flag != null) return false;
		if (player.weapons.Count == 0) return false;
		if (isWarpIn()) return false;
		if (invulnTime > 0) return false;
		if (junkShieldProjs.Count > 0) return false;
		if (sWell != null) return false;

		return base.canCharge();
	}

	public override bool chargeButtonHeld() {
		return player.input.isHeld(Control.Shoot, player);
	}
	
	public override List<ShaderWrapper> getShaders() {
		List<ShaderWrapper> baseShaders = base.getShaders();
		List<ShaderWrapper> shaders = new();
		ShaderWrapper? palette = null;

		int index = currentWeapon?.index ?? 0;
		if (index > (int)RockWeaponIds.WildCoil) index = (int)RockWeaponIds.MegaBuster;
		palette = player.rockPaletteShader;

		palette?.SetUniform("palette", index);
		palette?.SetUniform("paletteTexture", Global.textures["rock_palette_texture"]);

		if (palette != null) {
			shaders.Add(palette);
		}
		if (shaders.Count == 0) {
			return baseShaders;
		}

		shaders.AddRange(baseShaders);
		return shaders;
	}

	public override bool canAddAmmo() {
		if (player.weapon == null) { return false; }
		bool hasEmptyAmmo = false;
		foreach (Weapon weapon in player.weapons) {
			if (weapon.canHealAmmo && weapon.ammo < weapon.maxAmmo) {
				hasEmptyAmmo = true;
				break;
			}
		}
		return hasEmptyAmmo;
	}

	public override int getMaxChargeLevel() {
		return 2;
	}

	public bool canRideRushJet() {
		return charState is Fall && Global.level.checkTerrainCollisionOnce(this, 0, -20) == null;
	}

	public static List<Weapon> getAllRushWeapons() {
		return new List<Weapon>() {
			new RushCoilWeapon(),
			new RushJetWeapon(),
			new RushSearchWeapon()
		};
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);

		var wall = other.gameObject as Wall;
		var rush = other.gameObject as Rush;
		var isGHit = other.isGroundHit();
		bool isRushCoil = rush != null && rush.rushState is RushIdle or RushSleep && rush.type == 0;
		bool isRushJet = rush != null && rush.rushState is RushJetState;

		if (charState is RockDoubleJump && wall != null) {
			vel = new Point(RockDoubleJump.jumpSpeedX * xDir, RockDoubleJump.jumpSpeedY);
		}

		/* if (isRushCoil && isGHit && charState is Fall) {		
			rush?.changeState(new RushCoil());
			vel.y = getJumpPower() * -1.5f;
			changeState(new Jump(), true);
			rushWeapon.addAmmo(-4, player);
		} */

		/* if (isRushJet && isGHit && canRideRushJet()) {
			changeState(new RushJetRide(), true);
			grounded = true;
		} */
	}

	public override void onWeaponChange(Weapon oldWeapon, Weapon newWeapon) {
		base.onWeaponChange(oldWeapon, newWeapon);
		
		if (getChargeLevel() >= 2) {
			weaponCooldown = 0;
		} else {
			if (oldWeapon.switchCooldownFrames != null && weaponCooldown > 0) {
				weaponCooldown = Math.Max(weaponCooldown, oldWeapon.switchCooldownFrames.Value);
			} 
		}
	}

	public override Projectile? getProjFromHitbox(Collider hitbox, Point centerPoint) {
		int meleeId = getHitboxMeleeId(hitbox);
		if (meleeId == -1) {
			return null;
		}
		Projectile? proj = getMeleeProjById(meleeId, centerPoint);
		if (proj != null) {
			proj.meleeId = meleeId;
			proj.owningActor = this;
			return proj;
		}
		return null;
	}

	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"rock_slashclaw" or
			"rock_slashclaw_air" or
			"rock_ladder_slashclaw" => MeleeIds.SlashClaw,

			"rock_shoot_swell" or 
			"rock_ladder_shoot_swell" => MeleeIds.UnderWaterScorchWheel,

			"rock_sa_legbreaker" => MeleeIds.LegBreaker,
			_ => MeleeIds.None
		});
	}

	public Projectile? getMeleeProjById(int id, Point? pos = null, bool addToLevel = true) {
		Point projPos = pos ?? new Point(0, 0);
		Projectile? proj = id switch {
			(int)MeleeIds.SlashClaw => new GenericMeleeProj(
				new SlashClawWeapon(player), projPos, ProjIds.SlashClaw, player
			),

			(int)MeleeIds.UnderWaterScorchWheel => new GenericMeleeProj(
				new ScorchWheel(), projPos, ProjIds.ScorchWheelUnderwater,
				player, 2, 0, 0.5f, addToLevel: addToLevel
			),

			(int)MeleeIds.LegBreaker => new GenericMeleeProj(
				new LegBreaker(player), projPos, ProjIds.LegBreaker, player, 2, Global.halfFlinch, 0.5f,
				addToLevel: addToLevel
			),

			_ => null

		};
		return proj;

	}

	public enum MeleeIds {
		None = -1,
		SlashClaw,
		UnderWaterScorchWheel,
		LegBreaker,
	}

	public override void chargeGfx() {
		if (ownedByLocalPlayer) {
			chargeEffect.stop();
		}
		if (hasChargedNoiseCrush) return;

		if (isCharging()) {
			chargeSound.play();
			int level = getChargeLevel();
			var renderGfx = RenderEffectType.ChargeBlue;
			renderGfx = level switch {
				1 => RenderEffectType.ChargeBlue,
				2 => RenderEffectType.ChargeGreen,
				_ => RenderEffectType.None,
			};
			addRenderEffect(renderGfx, 2, 6);
			chargeEffect.character = this;
			chargeEffect.update(level, 1);
		}
	}

	public virtual Collider getSlidingCollider() {
		var rect = new Rect(0, 0, 36, 12);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public virtual float getSlideSpeed() {
		if (flag != null) {
			return getRunSpeed();
		}
		float dashSpeed = 3.5f * 60;
		return dashSpeed * getRunDebuffs();
	}

	public void removeBusterProjs() {
		sWell = null;
	}
	public override void destroySelf(
		string spriteName = "", string fadeSound = "",
		bool disableRpc = false, bool doRpcEvenIfNotOwned = false,
		bool favorDefenderProjDestroy = false
	) {
		base.destroySelf(spriteName, fadeSound, disableRpc, doRpcEvenIfNotOwned, favorDefenderProjDestroy);
		//if (rush != null) rush.destroySelf();
	}

	public override void aiAttack(Actor target) {
		if (AI.trainingBehavior != 0) {
			return;
		}
		if (player.weapon == null) {
			return;
		}
		Helpers.decrementFrames(ref aiWeaponSwitchCooldown);
		if (aiWeaponSwitchCooldown == 0) {
			player.weaponRight();
			aiWeaponSwitchCooldown = 120;
		}
		if (!isFacing(target)) {
			if (canCharge() && shootAnimTime == 0) {
				increaseCharge();
			}
			return;
		}
		if (canShoot() && player.weapon.shootCooldown == 0 && player.weapon.canShoot(0, player)) {
			shoot(0);
			stopCharge();
		} else if (canCharge() && shootAnimTime == 0) {
			increaseCharge();
		}
	}

	public bool canGoSuperAdaptor() {
		return (
			charState is not Die && charState is not CallDownRush &&
			!hasSuperAdaptor && player.currency >= SuperAdaptorCost &&
			rush == null
		);
	}

	public void setSuperAdaptor(bool addOrRemove) {
		if (addOrRemove) {
			hasSuperAdaptor = true;
			player.removeWeaponsButBuster();
			player.addSARocketPunch();
		} else {
			player.removeSARocketPunch();
			hasSuperAdaptor = false;
		}
	}
	
	public override (float, float) getGlobalColliderSize() {
		if (sprite.name == "rock_slide") {
			return (34, 12);
		}
		return (24, 30);
	}

	public override Point getCenterPos() {
		return pos.addxy(0, -21);
	}

	public override void onExitState(CharState oldState, CharState newState) {		 
		if (newState.shootSprite != null && sprite.name != getSprite(newState.shootSprite) && shootAnimTime > 0) {
			changeSpriteFromName(newState.shootSprite, false);
		}
		
	}

	public override List<byte> getCustomActorNetData() {
		// Get base arguments.
		List<byte> customData = base.getCustomActorNetData() ?? new();

		// Per-character data.
		int weaponIndex = currentWeapon?.index ?? 255;
		byte ammo = (byte)MathF.Ceiling(currentWeapon?.ammo ?? 0);
		customData.Add((byte)weaponIndex);
		customData.Add(ammo);

		customData.Add(Helpers.boolArrayToByte([
			hasChargedNoiseCrush,
			hasSuperAdaptor,
			armless
		]));

		return customData;
	}

	public override void updateCustomActorNetData(byte[] data) {
		// Update base arguments.
		base.updateCustomActorNetData(data);
		data = data[data[0]..];

		// Per-character data.
		Weapon? targetWeapon = weapons.Find(w => w.index == data[0]);
		if (targetWeapon != null) {
			weaponSlot = weapons.IndexOf(targetWeapon);
			targetWeapon.ammo = data[1];
		}

		bool[] boolData = Helpers.byteToBoolArray(data[2]);
		hasChargedNoiseCrush = boolData[0];
		hasSuperAdaptor = boolData[1];
		armless = boolData[2];
	}
}

