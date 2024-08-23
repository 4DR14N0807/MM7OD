using System;
using System.Collections.Generic;
using System.Linq;


namespace MMXOnline;

public class Rock : Character {

	public float lemonTime;
	public int lemons;
	public float weaponCooldown;
	public JunkShieldProj? junkShield;
	public ScorchWheelSpawn? sWellSpawn;
	public ScorchWheelProj? sWell;
	public UnderwaterScorchWheelProj? sWellU;
	public UnderwaterScorchWheelProj? underwaterScorchWheel;
	public Projectile? sWheel;
	public SARocketPunchProj? saRocketPunchProj;
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

		spriteToCollider["sa_activate_air"] = null;
		spriteToCollider["sa_activate"] = null;
		spriteToCollider["sa_activate_end"] = null;
		spriteToCollider["sa_activate_end_air"] = null;

		charge1Time = 40;
		charge2Time = 80;
		var rl = player.loadout.rockLoadout.rushLoadout;
		rushWeaponIndex = rl;
	
		rushWeapon = rl switch {
			0 => new RushCoilWeapon(),
			1 => new RushJetWeapon(),
			_ => new RushSearchWeapon(),
		};

		player.weapons.Add(rushWeapon);
	}

	public override void update() {
		base.update();

		if (!ownedByLocalPlayer) return;

		Helpers.decrementFrames(ref lemonTime);
		Helpers.decrementFrames(ref arrowSlashCooldown);
		Helpers.decrementFrames(ref legBreakerCooldown);
		Helpers.decrementFrames(ref weaponCooldown);

		timeSinceLastShoot++;
		if (timeSinceLastShoot >= 30) lemons = 0;

		if (player.weapon.ammo >= player.weapon.maxAmmo) {
			weaponHealAmount = 0;
		}

		if (player.weapon is not NoiseCrush) hasChargedNoiseCrush = false;

		if (hasChargedNoiseCrush) {
			if (chargedNoiseCrushSound == null) {
				chargedNoiseCrushSound = new LoopingSound("charge_start", "charge_loop", this);
			} else chargedNoiseCrushSound.play();
		} else if (chargedNoiseCrushSound != null) {
			chargedNoiseCrushSound.stop();
			chargedNoiseCrushSound = null!;
		}

		// For the shooting animation.
		if (shootAnimTime > 0) {
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

		if (specialPressed && canCallRush()) {
			rushWeapon.shoot(this, 0);
			return true;
		}

		if (hasSuperAdaptor) {
			if (slidePressed && downHeld && legBreakerCooldown <= 0 && canSlide()) {
				changeState(new LegBreakerState(slideControl), true);
				legBreakerCooldown = 90f;
				return true;
			}

			if (arrowSlashInput && arrowSlashCooldown <= 0 && charState is not LadderClimb) {
				changeState(new SAArrowSlashState(), true);
				arrowSlashCooldown = 90f;
				return true;
			}
		}

		/*if (shootPressed && canShoot()) {
			
		}*/

		if (!isCharging()) {
			if (shootPressed) {
				lastShootPressed = Global.frameCount;
			}
			int framesSinceLastShootPressed = Global.frameCount - lastShootPressed;
			if (shootPressed || framesSinceLastShootPressed < 6) {
				if (weaponCooldown <= 0) {
					shoot(0);
					return true;
				}
			}
		}
		return base.attackCtrl();
	}

	public void shoot(int chargeLevel) {
		if (!player.weapon.canShoot(chargeLevel, player)) return;
		if (!canShoot()) return;
		if (!charState.attackCtrl && !charState.invincible || charState is Slide) {
			changeToIdleOrFall();
		}
		// Shoot anim and vars.
		float oldShootAnimTime = shootAnimTime;
		
		setShootAnim();

		int chargedNS = hasChargedNoiseCrush ? 1 : 0;
		
		weaponCooldown = player.weapon.fireRateFrames;
		player.weapon.shoot(this, chargeLevel, chargedNS);
		player.weapon.addAmmo(-player.weapon.getAmmoUsage(chargeLevel), player);
		if (oldShootAnimTime <= 0.25f) {
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

	public void superAdaptorControls() {
		if (!grounded) {

			if (!usedDoubleJump && canJump() && flag == null && player.input.isPressed(Control.Jump, player) && charState is not LadderClimb) {
				changeState(new RockDoubleJump(), true);
				usedDoubleJump = true;
			}

		} else {
			usedDoubleJump = false;
			bool arrowSlashInput = player.input.checkHadoken(player, xDir, Control.Shoot);

			if (arrowSlashInput && arrowSlashCooldown <= 0 && charState is not LadderClimb) {
				changeState(new SAArrowSlashState(), true);
				arrowSlashCooldown = 90f / 60f;
			}

			bool legBreakerInput = player.input.isHeld(Control.Down, player);

			if (legBreakerInput && legBreakerCooldown <= 0 && canSlide() && player.dashPressed(out string slideControl)) {
				changeState(new LegBreakerState(slideControl), true);
				legBreakerCooldown = 90f / 60f;
			}
		}
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
			//hyperProgress += Global.spf;

			if (!boughtSuperAdaptorOnce) {
				player.currency -= SuperAdaptorCost;
				boughtSuperAdaptorOnce = true;
			}
			player.character.changeState(new CallDownRush(), true);

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
		addRenderEffect(RenderEffectType.ChargePink, 0.05f, 0.1f);
		noiseCrushAnimTime += Global.spf * 20;
		if (noiseCrushAnimTime >= 12) noiseCrushAnimTime = 0;
		float posMove = -4 * noiseCrushAnimTime;
		float sx = pos.x + x;
		float sy = pos.y + posMove;

		var sprite1 = Global.sprites["noise_crush_charge_part"];
		//float distFromCenter = Helpers.randomRange(-16, 16);
		float posOffset = noiseCrushAnimTime * 50;
		int hyperChargeAnimFrame = MathInt.Floor((noiseCrushAnimTime / 12) * sprite1.frames.Length);
		for (int i = 0; i < 8; i++) {
			sprite1.draw(hyperChargeAnimFrame, sx + Helpers.randomRange(-16,16), sy, 1,1, null, 1,1,1, zIndex + 1);
		}
		/*sprite1.draw(hyperChargeAnimFrame, sx + Helpers.randomRange(-16, 16), sy, 1, 1, null, 1, 1, 1, zIndex + 1);
		sprite1.draw(hyperChargeAnimFrame, sx + Helpers.randomRange(-16, 16), sy, 1, 1, null, 1, 1, 1, zIndex + 1);
		sprite1.draw(hyperChargeAnimFrame, sx + Helpers.randomRange(-16, 16), sy, 1, 1, null, 1, 1, 1, zIndex + 1);
		sprite1.draw(hyperChargeAnimFrame, sx + Helpers.randomRange(-16, 16), sy, 1, 1, null, 1, 1, 1, zIndex + 1);
		sprite1.draw(hyperChargeAnimFrame, sx + Helpers.randomRange(-16, 16), sy, 1, 1, null, 1, 1, 1, zIndex + 1);
		sprite1.draw(hyperChargeAnimFrame, sx + Helpers.randomRange(-16, 16), sy, 1, 1, null, 1, 1, 1, zIndex + 1);
		sprite1.draw(hyperChargeAnimFrame, sx + Helpers.randomRange(-16, 16), sy, 1, 1, null, 1, 1, 1, zIndex + 1);
		sprite1.draw(hyperChargeAnimFrame, sx + Helpers.randomRange(-16, 16), sy, 1, 1, null, 1, 1, 1, zIndex + 1);*/
	}

	public override bool canMove() {
		if (charState is CallDownRush) return false;

		return base.canMove();
	}

	public override bool canJump() {
		if (isSlideColliding) return false;

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
		if (charState is Burning) return false;
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

	public bool canCallRush() {
		if (isInvulnerableAttack() ||
			hasSuperAdaptor ||
			(rushWeapon is RushSearchWeapon && player.currency < RushSearchCost) ||
			flag != null) return false;
		
		return true;
	}


	public override bool canChangeWeapons() {
		return base.canChangeWeapons();
	}
	public override bool canCharge() {
		Weapon weapon = player.weapon;
		if (flag != null) return false;
		//if (isInvisibleBS.getValue()) return false;
		if (player.weapons.Count == 0) return false;
		if (isWarpIn()) return false;
		if (player.weapons.Count == 0) return false;
		if (invulnTime > 0) return false;
		if (junkShield != null) return false;
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

		int index = player.weapon.index;
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

	public override int maxChargeLevel() {
		return 2;
	}

	public bool canRideRushJet() {
		return charState is Fall && Global.level.checkCollisionActor(this, 0, -20) == null;
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
		var isGHit = other.hitData?.normal != null && other.hitData.normal.Value.isGroundNormal();
		bool isRushCoil = rush != null && rush.rushState is RushIdle or RushSleep && rush.type == 0;
		bool isRushJet = rush != null && rush.rushState is RushJetState;

		if (charState is RockDoubleJump && wall != null) {
			vel = new Point(RockDoubleJump.jumpSpeedX * xDir, RockDoubleJump.jumpSpeedY);
		}

		if (isRushCoil && isGHit && charState is Fall) {		
			rush?.changeState(new RushCoil());
			vel.y = getJumpPower() * -1.5f;
			changeState(new Jump(), true);
			rushWeapon.addAmmo(-1, player);
		}

		if (isRushJet && isGHit && canRideRushJet()) {
			changeState(new RushJetRide(), true);
			grounded = true;
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
				new SlashClawWeapon(player), projPos, ProjIds.SlashClaw, player, 3, 0, 0.25f,
				addToLevel: addToLevel

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
		if (isCharging()) {
			chargeSound.play();
			int level = getChargeLevel();
			var renderGfx = RenderEffectType.ChargeBlue;
			renderGfx = level switch {
				1 => RenderEffectType.ChargeBlue,
				2 => RenderEffectType.ChargeGreen,
				_ => RenderEffectType.ChargeBlue,
			};
			addRenderEffect(renderGfx, 0.033333f, 0.1f);
			chargeEffect.character = this;
			chargeEffect.update(getChargeLevel(), 1);
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

	public override List<byte> getCustomActorNetData() {
		// Get base arguments.
		List<byte> customData = base.getCustomActorNetData() ?? new();

		// Per-character data.
		int weaponIndex = player.weapon.index;
		if (weaponIndex == (int)WeaponIds.HyperBuster) {
			weaponIndex = player.weapons[player.hyperChargeSlot].index;
		}
		customData.Add((byte)weaponIndex);
		customData.Add((byte)MathF.Ceiling(player.weapon?.ammo ?? 0));

		customData.Add(Helpers.boolArrayToByte([
			hasChargedNoiseCrush,
			hasSuperAdaptor,
		]));

		return customData;
	}

	public override void updateCustomActorNetData(byte[] data) {
		// Update base arguments.
		base.updateCustomActorNetData(data);
		data = data[data[0]..];

		// Per-character data.
		player.changeWeaponFromWi(data[0]);
		if (player.weapon != null) {
			player.weapon.ammo = data[1];
		}

		bool[] boolData = Helpers.byteToBoolArray(data[2]);
		hasChargedNoiseCrush = boolData[0];
		hasSuperAdaptor = boolData[1];
	}
}

