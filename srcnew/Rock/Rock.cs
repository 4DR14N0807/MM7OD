using System;
using System.Collections.Generic;
using System.Linq;


namespace MMXOnline;

public class Rock : Character {
	public RockLoadout loadout;
	public float weaponCooldown;
	public List<Actor> junkShieldProjs = new();
	public LoopingSound? junkShieldSound;
	public ScorchWheelSpawn? sWellSpawn;
	public ScorchWheelProj? sWell;
	public UnderwaterScorchWheelSpawn? sWellU;
	public UnderwaterScorchWheelSpawn? underwaterScorchWheel;
	public Projectile? sWheel;
	public SARocketPunchProj? saRocketPunchProj;
	public bool armless;
	public ChargeEffect noiseCrushEffect;
	public bool hasChargedNoiseCrush = false;
	public float noiseCrushAnimTime;
	public LoopingSound? chargedNoiseCrushSound;
	public bool usedDoubleJump;
	public bool boughtSuperAdaptorOnce;
	public float timeSinceLastShoot;
	public bool isSlideColliding;
	public Rush? rush;
	public RushWeapon rushWeapon;
	public bool rushWeaponSpecial;
	public int rushWeaponIndex;
	public const int RushSearchCost = 5;
	public bool hasSuperAdaptor;
	public const int SuperAdaptorCost = 75;

	// AI Stuff.
	public float aiWeaponSwitchCooldown = 120;

	public Rock(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer,
		bool isWarpIn = true, RockLoadout? loadout = null
	) : base(
		player, x, y, xDir, isVisible, netId, ownedByLocalPlayer, isWarpIn
	) {
		charId = CharIds.Rock;
		maxHealth = (decimal)player.getMaxHealth(charId);
		health = (decimal)player.getMaxHealth(charId);
		if (loadout == null) {
			loadout = new RockLoadout {
				weapon1 = player.loadout.rockLoadout.weapon1,
				weapon2 = player.loadout.rockLoadout.weapon2,
				weapon3 = player.loadout.rockLoadout.weapon3,	
			};
			
		}
		
		this.loadout = loadout;
		weapons = getLoadout();

		charge1Time = 40;
		charge2Time = 105;

		rushWeapon = new RushWeapon();
		rushWeaponSpecial = !ownedByLocalPlayer || Options.main.rushSpecial || player.isAI;
		if (!rushWeaponSpecial) {
			weapons.Add(rushWeapon);
		}

		noiseCrushEffect = new ChargeEffect();
		noiseCrushEffect.character = this;

		addAttackCooldown(
			(int)AttackIds.LegBreaker, new AttackCooldown((int)RockWeaponSlotIds.LegBreaker, "hud_weapon_icon", 90)
		);
		addAttackCooldown(
			(int)AttackIds.ArrowSlash, new AttackCooldown((int)RockWeaponSlotIds.ArrowSlash, "hud_weapon_icon", 90)
		);

		if (isWarpIn && ownedByLocalPlayer) {
			health = 0;
		}
	}

	public override void update() {
		base.update();

		if (!ownedByLocalPlayer) return;

		Helpers.decrementFrames(ref weaponCooldown);
		armless = saRocketPunchProj != null;
		if (rushWeaponSpecial) {
			rushWeapon.update();
			rushWeapon.charLinkedUpdate(this, true);
		}
		
		if (currentWeapon?.ammo >= currentWeapon?.maxAmmo) {
			weaponHealAmount = 0;
		}

		//if (currentWeapon is not NoiseCrush) hasChargedNoiseCrush = false;

		if (hasChargedNoiseCrush) {
			if (chargedNoiseCrushSound == null) {
				if (!isCharging()) {
					chargedNoiseCrushSound = new LoopingSound("charge_start", "charge_loop", this);
				}
			} else {
				chargedNoiseCrushSound.play();
			}
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
		if (isCharging() && currentWeapon is RockBuster { superAdaptor: false } && charState is not LadderClimb) {
			if (charState.attackCtrl && !sprite.name.EndsWith("_shoot")) {
				if (shootAnimTime < 2) {
					shootAnimTime = 2;
				}
				changeSpriteFromName(charState.shootSprite, false);
				if (charState is Idle) frameIndex = 3;
			}
		} else if (shootAnimTime > 0 && !armless) {
			shootAnimTime -= Global.speedMul;
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
		bool slidePressed = player.input.isPressed(Control.Dash, player);
		if (!slidePressed && Options.main.downJumpSlide) {
			slidePressed = (
				player.input.isPressed(Control.Jump, player) &&
				player.input.isHeld(Control.Down, player)
			);
		}
		bool jumpPressed = player.input.isPressed(Control.Jump, player);

		if (slidePressed && canSlide() && charState is not Slide) {
			changeState(new Slide(Control.Dash), true);
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

		if (specialPressed && canCallRush(0) && rushWeaponSpecial && !isSlideColliding) {
			rushWeapon.shootRock(this, 0);
			return true;
		}

		if (hasSuperAdaptor) {
			if (slidePressed && downHeld && isCooldownOver((int)AttackIds.LegBreaker) && canSlide()) {
				changeState(new LegBreakerState(slideControl), true);
				return true;
			}

			if (specialPressed && isCooldownOver((int)AttackIds.ArrowSlash) && charState is not LadderClimb && !isSlideColliding) {
				changeState(new SAArrowSlashState(), true);
				return true;
			}
		}

		if (!isCharging() && !isSlideColliding) {
			if (shootPressed && weaponCooldown <= 0 && currentWeapon?.shootCooldown <= 0) {
				shoot(0);
				return true;
			}
		}
		return base.attackCtrl();
	}

	public void shoot(int chargeLevel) {
		if (!ownedByLocalPlayer) return;
		if (currentWeapon == null) { return; }
		if (currentWeapon.canShoot(chargeLevel, player) == false) return;
		if (!canShoot()) return;
		if (!charState.attackCtrl && !charState.invincible || charState is Slide) {
			changeToIdleOrFall();
		}
		// Shoot anim and vars.
		float oldShootAnimTime = shootAnimTime;
		if (currentWeapon.hasCustomAnim == false) {
			setShootAnim();
		}
		int chargedNS = hasChargedNoiseCrush ? 1 : 0;
		currentWeapon.shootRock(this, chargeLevel, chargedNS);
		currentWeapon.shootCooldown = currentWeapon.fireRate;
		weaponCooldown = currentWeapon.fireRate;
		if (currentWeapon.switchCooldown < weaponCooldown) {
			weaponCooldown = currentWeapon.switchCooldown;
		}
		currentWeapon.addAmmo(-currentWeapon.getAmmoUsage(chargeLevel), player);
		if (oldShootAnimTime <= 20 && !currentWeapon.hasCustomAnim) {
			shootAnimTime = 20;
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
		shootAnimTime = 18;
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
		if (charState is CallDownRush or WarpIdle) {
			hyperProgress = 0;
			return;
		}
		if (player.health <= 0) {
			hyperProgress = 0;
			return;
		}
		if (charState is not WarpIn && canGoSuperAdaptor()) {

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
		addRenderEffect(RenderEffectType.NCrushCharge, 3, 5);
		noiseCrushEffect.character = this;
		noiseCrushEffect.update(2, 2);
		noiseCrushEffect.render(getCenterPos());
	}

	public bool isUsingRushJet() {
		if (!grounded) {
			return false;
		}
		CollideData? collideData = Global.level.checkTerrainCollisionOnce(this, 0, 2, checkPlatforms: true);
		return (
			collideData?.gameObject is Rush rj &&
			rj.rushState is RushJetState &&
			pos.y <= rj.pos.y - 4
		);
	}

	public override bool canMove() {
		if (charState is CallDownRush) return false;

		return base.canMove() && !isUsingRushJet();
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
		if (rootTime > 0) return false;
		if (bigBubble != null) return false;
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
		if (isSlideColliding) return false;
		if (sWell != null) return false;
		if (sWellSpawn != null) return false;
		if (sWellU != null) return false;
		if (charState is Slide)
			return (currentWeapon is RockBuster || currentWeapon is WildCoil) && getChargeLevel() == 2;
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
			//(type == 2 && player.currency < RushSearchCost) ||
			flag != null
		) {
			return false;
		}
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

	public override bool isInvulnerable(bool ignoreRideArmorHide = false, bool factorHyperMode = false) {
		if (charState is CallDownRush) return true;
 		
		return base.isInvulnerable(ignoreRideArmorHide, factorHyperMode);
	}

	// Loadout Stuff
	
	public List<Weapon> getLoadout() {	
		// 1v1/Training loadout.
		if (Global.level.isTraining() && !Global.level.server.useLoadout || Global.level.is1v1()) {
			return getAllWeapons();
		}
		
		// Regular Loadout.
		return getWeaponsFromLoadout(loadout);
	}

	public static List<Weapon> getAllWeapons() {
		return new List<Weapon>()
		{
				new RockBuster(false),
				new FreezeCracker(),
				new ThunderBolt(),
				new JunkShield(),
				new ScorchWheel(),
				new SlashClawWeapon(),
				new NoiseCrush(),
				new DangerWrap(),
				new WildCoil(),
				//new RushWeapon(),
		};
	}

	public static Weapon getWeaponById(int id) {
		return id switch {
			0 => new RockBuster(false),
			1 => new FreezeCracker(),
			2 => new ThunderBolt(),
			3 => new JunkShield(),
			4 => new ScorchWheel(),
			5 => new SlashClawWeapon(),
			6 => new NoiseCrush(),
			7 => new DangerWrap(),
			8 => new WildCoil(),
			_ => new RockBuster(false)
		};
	}
	
	public List<Weapon> getWeaponsFromLoadout(RockLoadout loadout) {
		return [
			getWeaponById(loadout.weapon1),
			getWeaponById(loadout.weapon2),
			getWeaponById(loadout.weapon3)
		];
	}

	public static List<Weapon> getRandomWeapons(Player player) {
		return [
			getWeaponById(player.loadout.rockLoadout.weapon1),
			getWeaponById(player.loadout.rockLoadout.weapon2),
			getWeaponById(player.loadout.rockLoadout.weapon3),
		];
	}

	public override List<ShaderWrapper> getShaders() {
		List<ShaderWrapper> shaders = new();
		List<ShaderWrapper> baseShaders = base.getShaders();
		ShaderWrapper? palette = null;

		int index = currentWeapon?.index ?? 0;
		if (index > (int)RockWeaponIds.WildCoil) index = (int)RockWeaponIds.MegaBuster;
		palette = player.rockPaletteShader;

		palette?.SetUniform("palette", index);
		palette?.SetUniform("paletteTexture", Global.textures["rock_palette_texture"]);

		if (palette != null) {
			shaders.Add(palette);
		}
		shaders.AddRange(baseShaders);

		return shaders;
	}

	public override bool canAddAmmo() {
		if (currentWeapon == null) { return false; }
		return getRefillTargetWeapon() != null;
	}

	public override void addPercentAmmo(float amount) {
		Weapon? targetWeapon = getRefillTargetWeapon();
		if (targetWeapon == null) {
			return;
		}
		if (ownedByLocalPlayer && targetWeapon != currentWeapon && targetWeapon is not RushWeapon) {
			playSound("heal");
		}
		targetWeapon.addAmmoPercentHeal(amount);
	}

	public Weapon? getRefillTargetWeapon() {
		if (currentWeapon?.canHealAmmo == true && currentWeapon.ammo < currentWeapon.maxAmmo) {
			return currentWeapon;
		}
		if (rushWeapon.ammo < rushWeapon.maxAmmo) {
			return rushWeapon;
		}
		Weapon? targetWeapon = null;
		float targetAmmo = Int32.MaxValue;

		foreach (Weapon weapon in weapons) {
			if (!weapon.canHealAmmo) {
				continue;
			}
			if (weapon != currentWeapon &&
				weapon.ammo < weapon.maxAmmo &&
				weapon.ammo < targetAmmo
			) {
				targetWeapon = weapon;
				targetAmmo = targetWeapon.ammo;
			}
		}
		return targetWeapon;
	}

	public override int getMaxChargeLevel() {
		return 2;
	}

	public bool canRideRushJet() {
		var collideData = Global.level.checkTerrainCollisionOnce(this, 0, -20);
		var collideDatas = Global.level.checkTerrainCollisionOnce(this, 0, 1, checkPlatforms: true);
		Rush? rj = collideDatas?.gameObject as Rush;

		/* foreach(var cd in collideDatas) {
			if (cd.gameObject is Rush r) rj = r;
		} */

		return collideData == null && rj != null/*  && rj.rushState is RushJetState */;
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
		if (!ownedByLocalPlayer) return;

		var wall = other.gameObject as Wall;
		var rush = other.gameObject as Rush;
		bool isGHit = Global.level.checkTerrainCollisionOnce(this, 0, 1, checkPlatforms: true) != null;
		bool isRushCoil = (
			rush != null && rush == this.rush &&
			rush.rushState is RushIdle or RushSleep &&
			rush.type == 0
		);
		bool isRushJet = rush != null && rush == this.rush && rush.rushState is RushJetState;

		if (isGHit && isLanding() && isRushJet && rush?.rushState is RushJetState rjs && !rjs.once) {
			rjs.once = true;
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
			proj.ownerActor = this;
			updateProjFromHitbox(proj);
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

	public override Projectile? getMeleeProjById(int id, Point projPos, bool addToLevel = true) {
		Projectile? proj = id switch {
			(int)MeleeIds.SlashClaw => new SlashClawMelee(
				projPos, player, addToLevel: addToLevel
			),

			(int)MeleeIds.UnderWaterScorchWheel => new GenericMeleeProj(
				new ScorchWheel(), projPos, ProjIds.ScorchWheelUnderwater,
				player, 1, 0, 0.5f * 60, addToLevel: addToLevel
			),

			(int)MeleeIds.LegBreaker => new GenericMeleeProj(
				new LegBreaker(player), projPos, ProjIds.LegBreaker, player, 2, Global.halfFlinch, 0.5f * 60,
				addToLevel: addToLevel
			) { projId = (int)RockProjIds.LegBreaker },

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

	public enum AttackIds {
		ArrowSlash,
		LegBreaker,
	}

	public override void chargeGfx() {
		if (ownedByLocalPlayer) {
			chargeEffect.stop();
		}
		//if (hasChargedNoiseCrush) return;

		if (isCharging()) {
			chargeSound.play();
			int level = getChargeLevel();
			var renderGfx = RenderEffectType.ChargeBlue;
			renderGfx = level switch {
				1 => RenderEffectType.ChargeBlue,
				2 => RenderEffectType.ChargeGreen,
				_ => RenderEffectType.None,
			};
			addRenderEffect(renderGfx, 3, 5);
			chargeEffect.character = this;
			chargeEffect.update(level, 0);
		}
	}

	public virtual float getSlideSpeed() {
		return 3.5f * getRunDebuffs();
	}

	public void removeLastingProjs() {
		sWellSpawn?.destroySelf();
		sWell?.destroySelf();
		sWellU?.destroySelf();
		foreach (Weapon w in weapons) {
			if (w is DangerWrap dw) {
				foreach (Projectile mine in dw.dangerMines) {
					if (mine is DangerWrapLandProj lProj) {
						lProj.health = 0;
						lProj.destroySelf();
					}
					else {
						mine.destroySelf();
					}
				}
			}
		}
	}

	public override void destroySelf(
		string spriteName = "", string fadeSound = "",
		bool disableRpc = false, bool doRpcEvenIfNotOwned = false,
		bool favorDefenderProjDestroy = false
	) {
		removeLastingProjs();

		base.destroySelf(spriteName, fadeSound, disableRpc, doRpcEvenIfNotOwned, favorDefenderProjDestroy);
	}

	public override void aiAttack(Actor? target) {
		if (target == null) {
			return;
		}
		if (AI.trainingBehavior != 0) {
			return;
		}
		if (currentWeapon == null) {
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
		if (canShoot() && weaponCooldown == 0 &&
			currentWeapon.shootCooldown == 0 &&
			currentWeapon.canShoot(0, player)
		) {
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
			rush == null && charState.normalCtrl
		);
	}

	public void setSuperAdaptor(bool addOrRemove) {
		if (addOrRemove) {
			heal(player, (float)(maxHealth - health));
			hasSuperAdaptor = true;
			weapons.Add(new RockBuster(true));
		} else {
			hasSuperAdaptor = false;
			foreach (Weapon wep in weapons) {
				if (wep is RockBuster { superAdaptor: true }) weapons.Remove(wep);
			}
		}
	}

	public override (float, float) getGlobalColliderSize() {
		if (sprite.name == "rock_slide" || sprite.name == "rock_sa_legbreaker") {
			return (34, 14);
		}
		return (24, 36);
	}

	public override (float, float) getTerrainColliderSize() {
		if (sprite.name == "rock_slide" || sprite.name == "rock_sa_legbreaker") {
			return (34, 12);
		}
		return (24, 30);
	}

	public override Point getCenterPos() {
		float yCollider = getGlobalColliderSize().Item2 / 2;
		return pos.addxy(0, -yCollider);
	}

	public override void onExitState(CharState oldState, CharState newState) {
		if (newState.shootSprite != null &&
			sprite.name != getSprite(newState.shootSprite) && shootAnimTime > 0
		) {
			changeSpriteFromName(newState.shootSprite, false);
		}
	}

	public override List<byte> getCustomActorNetData() {
		// Get base arguments.
		List<byte> customData = base.getCustomActorNetData();

		// Per-character data.
		int weaponIndex = currentWeapon?.index ?? 255;
		byte ammo = (byte)MathF.Ceiling(currentWeapon?.ammo ?? 0);
		customData.Add((byte)weaponIndex);
		customData.Add(ammo);
		customData.Add((byte)getChargeLevel());

		customData.Add(Helpers.boolArrayToByte([
			hasChargedNoiseCrush,
			hasSuperAdaptor,
			armless
		]));

		return customData;
	}

	public override void updateCustomActorNetData(byte[] data) {
		// Update base arguments.
		if (data[0] > 1) { base.updateCustomActorNetData(data); }
		data = data[data[0]..];

		// Per-character data.
		Weapon? targetWeapon = weapons.Find(w => w.index == data[0]);
		if (targetWeapon != null) {
			weaponSlot = weapons.IndexOf(targetWeapon);
			targetWeapon.ammo = data[1];
		}

		int netChargeLevel = data[2];
		if (netChargeLevel == 0) {
			stopCharge();
		}

		bool[] boolData = Helpers.byteToBoolArray(data[3]);
		hasChargedNoiseCrush = boolData[0];
		hasSuperAdaptor = boolData[1];
		armless = boolData[2];
	}
}

