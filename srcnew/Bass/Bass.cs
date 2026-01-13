using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SFML.Graphics;

namespace MMXOnline;

public class Bass : Character {
	// Weapons.
	public BassLoadout loadout;
	public float weaponCooldown;
	public float tBladeDashCooldown;
	public SuperBassRP? sbRocketPunch;
	public bool armless;

	// Modes.
	public bool isSuperBass;
	public const int TrebleBoostCost = 75;
	public int phase;
	public int[] evilEnergy = new int[] {0,0,0};
	public const int MaxEvilEnergy = 16;
	public float flyTime;
	public const float MaxFlyTime = 240;
	public bool canRefillFly;

	// AI Stuff.
	public float aiWeaponSwitchCooldown = 120;

	public Bass(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer,
		bool isWarpIn = true, BassLoadout? loadout = null
	) : base(
		player, x, y, xDir, isVisible, netId, ownedByLocalPlayer, isWarpIn
	) {
		charId = CharIds.Bass;
		if (loadout == null) {
			loadout = new BassLoadout {
				weapon1 = player.loadout.bassLoadout.weapon1,
				weapon2 = player.loadout.bassLoadout.weapon2,
				weapon3 = player.loadout.bassLoadout.weapon3,
			};
		}
		this.loadout = loadout;
		weapons = getLoadout();

		maxHealth = (decimal)player.getMaxHealth(charId);
		maxHealth -= player.evilEnergyHP;
		health = maxHealth;

		charge1Time = 40;
		charge2Time = 105;
		charge3Time = 180;

		addAttackCooldown(
			(int)AttackIds.Kick, new AttackCooldown((int)BassWeaponIds.BassKick, "hud_weapon_icon_bass", 75)
		);
		addAttackCooldown(
			(int)AttackIds.SweepingLaser, new AttackCooldown((int)BassWeaponIds.SweepingLaser, "hud_weapon_icon_bass", 90)
		);
		addAttackCooldown(
			(int)AttackIds.DarkComet, new AttackCooldown((int)BassWeaponIds.DarkComet, "hud_weapon_icon_bass", 90)
		);

		if (isWarpIn && ownedByLocalPlayer) {
			health = 0;
		}
	}

	public override bool canAddAmmo() {
		if (player.weapon == null) { return false; }
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
			return player.weapon;
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

	public bool canGoSuperBass() {
		return (
			charState is not Die && charState.normalCtrl &&
			!isSuperBass && player.currency >= TrebleBoostCost &&
			player.evilEnergyStacks <= 0 && player.pendingEvilEnergyStacks <= 0
		);
	}

	public void setSuperBass() {
		if (isSuperBass) {
			return;
		}
		isSuperBass = true;
		maxHealth += 2;
		heal(player, (float)(maxHealth - health));
		phase = 0;
		player.changeWeaponSlot(0);
		weapons.Clear();
		weapons.Add(new SBassBuster());
		weapons.Add(new SBassRP());
	}

	public void nextPhase(int level) {
		evilEnergy[phase] = MaxEvilEnergy;
		phase = level;
		player.pendingEvilEnergyStacks = level;
	}

	public override void update() {
		base.update();
	
		//Hypermode Music.
		if (Global.level.enabledBreakmanMusic()) {
			if (isSuperBass) {
				if (musicSource == null) {
					addMusicSource("basstheme", getCenterPos(), true);
				}
			} else {
				destroyMusicSource();
			}
		}

		//if (showNumberTime > 0) drawCardNumber(lastCardNumber);

		//Non-local players end here.
		if (!ownedByLocalPlayer) return;


		Helpers.decrementFrames(ref weaponCooldown);
		Helpers.decrementFrames(ref tBladeDashCooldown);
		if (refillFly()) {
			Helpers.decrementFrames(ref flyTime);
		}
		armless = sbRocketPunch != null;
		if (flyTime > MaxFlyTime) {
			flyTime = MaxFlyTime;
		}
		for (int i = 0; i < evilEnergy.Length; i++) {
			if (evilEnergy[i] > MaxEvilEnergy) evilEnergy[i] = MaxEvilEnergy;
		}
		player.changeWeaponControls();

		// For the shooting animation.
		if ((shootAnimTime > 0) || charState is LadderClimb) {
			Helpers.decrementFrames(ref shootAnimTime);
			if (shootAnimTime <= 0 || string.IsNullOrEmpty(charState.shootSprite)) {
				shootAnimTime = 0;
				if (sprite.name.EndsWith("_shoot")) {
					changeSpriteFromName(charState.defaultSprite, false);
				}
			}
		}

		if (isSuperBass) {
			chargeLogic(shoot);
		}
		quickHyperUpgrade(); 
	}

	public override void chargeGfx() {
		if (ownedByLocalPlayer) {
			chargeEffect.stop();
		}

		if (isCharging()) {
			chargeSound.play();
			int level = getChargeLevel();
			RenderEffectType renderGfx = RenderEffectType.ChargeBlue;
			renderGfx = level switch {
				1 => RenderEffectType.ChargeBlue,
				2 => RenderEffectType.ChargeGreenBass2,
				3 => RenderEffectType.ChargePurpleBass,
				_ => RenderEffectType.None,
			};
			addRenderEffect(renderGfx, 3, 5);
			chargeEffect.character = this;
			chargeEffect.update(level, 0);
		}
	}

	public override (string, int) getBaseHpSprite() {
		return ("hud_health_base", 2);
	}

	public override void renderHUD(Point offset, GameMode.HUDHealthPosition position) {
		offset = offset.addxy(0, 0);
		base.renderHUD(offset, position);

	}

	public override void renderLifebar(Point offset, GameMode.HUDHealthPosition position) {
		decimal damageSavings = 0;
		if (health > 0 && health < maxHealth) {
			damageSavings = MathInt.Floor(this.damageSavings);
		}

		Point hudHealthPosition = GameMode.getHUDHealthPosition(position, true);
		float baseX = hudHealthPosition.x + offset.x;
		float baseY = hudHealthPosition.y + offset.y;

		(string healthBaseSprite, int baseSpriteIndex) = getBaseHpSprite();
		Global.sprites[healthBaseSprite].drawToHUD(baseSpriteIndex, baseX, baseY);
		baseY -= 16;
		decimal modifier = (decimal)Player.getHealthModifier();
		decimal maxHP = Math.Ceiling((maxHealth + player.evilEnergyHP) / modifier);
		decimal curHP = Math.Floor(health / modifier);
		decimal ceilCurHP = Math.Ceiling(health / modifier);
		decimal floatCurHP = health / modifier;
		float fhpAlpha = (float)(floatCurHP - curHP);
		decimal savings = curHP + (damageSavings / modifier);

		for (var i = 0; i < Math.Ceiling(maxHP); i++) {
			// Draw HP
			if (i < curHP) {
				Global.sprites["hud_health_full"].drawToHUD(0, baseX, baseY);
			}
			else if (i < savings) {
				Global.sprites["hud_weapon_full_blues"].drawToHUD(2, baseX, baseY);
			}
			else if (i >= Math.Ceiling(maxHP) - player.evilEnergyHP) {
				Global.sprites["hud_energy_full"].drawToHUD(2, baseX, baseY);
			}
			else {
				Global.sprites["hud_health_empty"].drawToHUD(0, baseX, baseY);
				if (i < ceilCurHP) {
					Global.sprites["hud_health_full"].drawToHUD(0, baseX, baseY, fhpAlpha);
				}
			}
			baseY -= 2;
		}
		(string healthTopSprite, int baseTopIndex) = getTopHpSprite();
		Global.sprites[healthTopSprite].drawToHUD(baseTopIndex, baseX, baseY);
	}

	public override void renderAmmo(
		Point offset, GameMode.HUDHealthPosition position, Weapon? weaponOverride = null
	) {
		if (isSuperBass) {
			renderSuperAmmo(offset, position);
			return;
		}
		base.renderAmmo(offset, position, weaponOverride);
	}

	public void renderSuperAmmo(Point offset, GameMode.HUDHealthPosition position) {
		Point energyBarPos = GameMode.getHUDHealthPosition(position, false).add(offset);

		Global.sprites["hud_energy_base"].drawToHUD(phase, energyBarPos.x, energyBarPos.y);
		if (Global.frameCount % 6 >= 3 && charState is EnergyCharge or EnergyIncrease) {
			Global.sprites["hud_energy_eyes"].drawToHUD(phase, energyBarPos.x, energyBarPos.y);
		}
		energyBarPos.y -= 16;
		Point energyStartPos = energyBarPos;
		int[][] index = [
			[0,1,2],
			[3,4,5],
			[6,7,8]
		];

		for (int i = 0; i < phase + 1; i++) {
			int amount =  evilEnergy[i];
			energyBarPos = energyStartPos;
			for (int j = 0; j < MaxEvilEnergy; j++) {
				int k = i == 1 && evilEnergy[i] >= MaxEvilEnergy ? 2 : i;
				if (j < amount) {
					Global.sprites["hud_energy_full"].drawToHUD(
						index[phase][k], energyBarPos.x, energyBarPos.y
					);
				}
				else if (i == 0){
					Global.sprites["hud_energy_empty"].drawToHUD(phase, energyBarPos.x, energyBarPos.y);
				}
				energyBarPos.y -= 2;
			}
		}
	
		Global.sprites["hud_energy_top"].drawToHUD(phase, energyBarPos.x, energyBarPos.y);

		if (player.isMainPlayer && (charState is BassFly || flyTime > 0)) {
			decimal maxLength = 14;
			decimal length = (maxLength * (decimal)(MaxFlyTime - flyTime)) / (decimal)MaxFlyTime;
			int color = 4;
			if (length <= maxLength * (1 / 2)) color = 3;
			drawBarVHUD(length, maxLength, color, pos.addxy(-20 * xDir, -27));
		}
	}

	public void quickHyperUpgrade() {
		if (isSuperBass || !alive || !canGoSuperBass() ||
			charState.immortal || charState is SuperBassStart or WarpIdle ||
			!player.input.isHeld(Control.Special2, player)
		) {
			hyperProgress = 0;
			return;
		}
		if (hyperProgress < 1 || !charState.normalCtrl) {
			hyperProgress += Global.spf;
			return;
		}
		player.currency -= TrebleBoostCost;
		changeState(new SuperBassStart(), true);
		hyperProgress = 0;
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

	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"bass_tblade" => MeleeIds.TenguBlade,
			"bass_tblade_dash" => MeleeIds.TenguBladeDash,
			"sbass_kick" => MeleeIds.Kick,
			"sbass_soniccrusher" => MeleeIds.SonicCrusher,
			_ => MeleeIds.None
		});
	}

	public override Projectile? getMeleeProjById(int id, Point projPos, bool addToLevel = true) {
		return id switch {
			(int)MeleeIds.TenguBlade => new TenguBladeProjMelee(
				projPos, player, addToLevel: addToLevel
			),
			(int)MeleeIds.TenguBladeDash => new TenguBladeMelee(
				projPos, player, addToLevel: addToLevel
			),
			(int)MeleeIds.Kick => new GenericMeleeProj(
				new Weapon(), projPos, ProjIds.BassKick, player, 2, Global.halfFlinch, 0.75f * 60,
				addToLevel: addToLevel
			),
			(int)MeleeIds.SonicCrusher => new GenericMeleeProj(
				new Weapon(), projPos, ProjIds.SonicCrusher, player, 2, Global.halfFlinch, 1 * 60,
				addToLevel: addToLevel
			),
			_ => null
		};
	}

	public enum MeleeIds {
		None = -1,
		TenguBlade,
		TenguBladeDash,
		Kick,
		SonicCrusher,
	}

	public enum AttackIds {
		Kick,
		DarkComet,
		SweepingLaser,
	}

	public bool canUseTBladeDash() {
		return player.weapon is TenguBlade tb && tb.ammo > 0 &&
		grounded && tBladeDashCooldown <= 0 && flag == null;
	}

	public override bool normalCtrl() {
		if (isSuperBass) {
			if (player.input.isPressed(Control.Jump, player) &&
				!grounded && !canAirJump() && flyTime < MaxFlyTime) {
				dashedInAir++;
				changeState(new BassFly(), false);
				return true;
			}
			if (
				player.input.isHeld(Control.Special2, player) &&
				charState is not EnergyCharge && phase < 2
			) {
				changeState(new EnergyCharge(), true);
				return true;
			}
		}
		return base.normalCtrl();
	}

	public override bool attackCtrl() {
		// Shoot controls.
		bool shootPressed;
		if (currentWeapon?.isStream == true) {
			shootPressed = player.input.isHeld(Control.Shoot, player);
		} else {
			shootPressed = player.input.isPressed(Control.Shoot, player);
		}

		if (player.input.isHeld(Control.Dash, player) && shootPressed && canUseTBladeDash()) {
			changeState(new TenguBladeDash(), true);
			return true;
		}

		if (shootPressed) {
			if (weaponCooldown <= 0 &&
				currentWeapon?.canShoot(0, this) == true &&
				currentWeapon?.shootCooldown <= 0 && canShoot()
			) {
				shoot(getChargeLevel());
				return true;
			}
		}

		if (isSuperBass && player.input.isPressed(Control.Special1, player)) {
			int yInput = player.input.getYDir(player);

			if (yInput == 1 && phase >= 1 && !grounded) {
				if (isCooldownOver((int)AttackIds.SweepingLaser)) {
					changeState(new SweepingLaserState(), true);
					return true;
				}
				return false;
			}
			if (yInput == -1 && phase >= 1 && !grounded) {
				if (isCooldownOver((int)AttackIds.DarkComet)) {
					changeState(new DarkCometState(), true);
					return true;
				}
				return false;
			}
			if (grounded && yInput == -1) {
				if (isCooldownOver((int)AttackIds.Kick)) {
					changeState(new BassKick(), true);
					return true;
				}
				return false;
			}
			if (flyTime < MaxFlyTime) {
				Point spd = Point.zero;
				if (charState is BassFly bfly) spd.x = bfly.getFlightMove().x;
				changeState(new SonicCrusher(spd.addxy(xPushVel + xIceVel, 0)), true);
				return true;
			}
			return false;
		}
		return base.attackCtrl();
	}

	public void shoot(int chargeLevel) {
		if (!ownedByLocalPlayer) return;
		if (currentWeapon == null) {
			return;
		}
		turnToInput(player.input, player);
		if (!currentWeapon.hasCustomAnim) {
			if (charState is LadderClimb lc) {
				changeState(new BassShootLadder(lc.ladder), true);
			}
			else if (charState is BassShootLadder bsl) {
				changeState(new BassShootLadder(bsl.ladder), true);
			}
			else if (charState is BassFly) {
				string shootSprite = getSprite(charState.shootSprite);
				changeSprite(shootSprite, false);
				frameIndex = 0;
				frameTime = 0;
				animTime = 0;
				shootAnimTime = 18;
			}
			else {
				changeState(new BassShoot(), true);
			}
		}
		currentWeapon.shoot(this, chargeLevel);
		currentWeapon.shootCooldown = currentWeapon.fireRate;
		weaponCooldown = currentWeapon.fireRate;
		if (currentWeapon.switchCooldown < weaponCooldown) {
			weaponCooldown = currentWeapon.switchCooldown;
		}
		currentWeapon.addAmmo(-currentWeapon.getAmmoUsageEX(0, this), player);
		stopCharge();
	}

	public int getShootYDir(bool allowDown = false, bool allowDiagonal = true) {
		int dir = player.input.getYDir(player);
		int multiplier = 2;
		if (allowDiagonal && player.input.getXDir(player) != 0) {
			multiplier = 1;
		}
		if (!allowDown && dir * multiplier == 2) return 1;

		return dir * multiplier;
	}

	public int getShootAngle(bool allowDown = false, bool allowDiagonal = true) {
		int baseAngle = 0;
		if (xDir == -1) {
			baseAngle = 128;
		}
		return getShootYDir(allowDown, allowDiagonal) * xDir * 32 + baseAngle;
	}

	public override void onWeaponChange(Weapon oldWeapon, Weapon newWeapon) {
		base.onWeaponChange(oldWeapon, newWeapon);
		if (charState is BassShoot && oldWeapon is WaveBurner) {
			playSound("waveburnerEnd", sendRpc: true);
		}
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
		return [
			new BassBuster(),
			new IceWall(),
			new CopyVision(),
			new SpreadDrill(),
			new WaveBurner(),
			new RemoteMine(),
			new LightningBolt(),
			new TenguBlade(),
			new MagicCard(),
		];
	}

	public static Weapon getWeaponById(int id) {
		return id switch {
			1 => new IceWall(),
			2 => new CopyVision(),
			3 => new SpreadDrill(),
			4 => new WaveBurner(),
			5 => new RemoteMine(),
			6 => new LightningBolt(),
			7 => new TenguBlade(),
			8 => new MagicCard(),
			_ => new BassBuster()
		};
	}

	public List<Weapon> getWeaponsFromLoadout(BassLoadout loadout) {
		return [
			getWeaponById(loadout.weapon1),
			getWeaponById(loadout.weapon2),
			getWeaponById(loadout.weapon3)
		];
	}

	public static List<Weapon> getRandomWeapons(Player player) {
		return [
			getWeaponById(player.loadout.bassLoadout.weapon1),
			getWeaponById(player.loadout.bassLoadout.weapon2),
			getWeaponById(player.loadout.bassLoadout.weapon3),
		];
	}

	public void drawCardNumber(int number) {
		Point center = getCenterPos();

		Global.sprites["magic_card_numbers"].draw(
			number, center.x, center.y - 16,
			1, 1, null, 1, 1, 1, zIndex + 10
		);
	}

	public override string getSprite(string spriteName) {
		string prefix = isSuperBass ? "sbass_" : "bass_";
		return prefix + spriteName;
	}

	public override (float, float) getGlobalColliderSize() {
		if (sprite.name == getSprite("dash") || sprite.name == getSprite("tblade_dash")) {
			return (24, 30);
		}
		if (sprite.name == getSprite("soniccrusher")) return (38, 20);

		float yExtra = isSuperBass ? 4 : 0;
		return (24, 36 + yExtra);
	}

	public override (float, float) getTerrainColliderSize() {
		if (sprite.name == getSprite("dash") || sprite.name == getSprite("tblade_dash")) {
			return (24, 24);
		}

		if (sprite.name == getSprite("soniccrusher")) return (24, 20);

		return (24, 30);
	}

	public override Point getCenterPos() {
		float yCollider = getGlobalColliderSize().Item2 / 2;
		return pos.addxy(0, -yCollider);
	}

	public override bool canCrouch() {
		return false;
	}

	public override bool canClimbLadder() {
		if (armless) return false;

		return base.canClimbLadder();
	}

	public override bool canMove() {
		if (shootAnimTime > 0 && grounded) {
			return false;
		}
		return base.canMove();
	}

	public override bool canShoot() {
		if (weaponCooldown > 0 ||
			sbRocketPunch != null
		) {
			return false;
		}
		return base.canShoot();
	}

	public override bool canCharge() {
		return base.canCharge() && isSuperBass && charState.attackCtrl;
	}

	public override bool chargeButtonHeld() {
		return player.input.isHeld(Control.Shoot, player);
	}

	public override int getMaxChargeLevel() {
		if (isSuperBass && phase >= 2) return 3;
		return 2;
	}
	public override float getDashSpeed() {
		return 3.45f * getRunDebuffs();
	}

	public override float getFallSpeed(bool checkUnderwater = true) {
		float modifier = 1;
		if (charState is EnergyCharge or EnergyIncrease) modifier = 0.05f;

		return base.getFallSpeed() * modifier;
	}

	public override bool canAirJump() {
		return dashedInAir == 0 && rootTime <= 0 && charState is not BassShootLadder && !isSuperBass;
	}

	public override bool canAirDash() {
		return false;
	}

	public override bool canWallClimb() {
		return false;
	}

	public override void changeToIdleFallorFly(string transitionSprite = "") {
		if (grounded) {
			if (charState is not Idle) changeState(new Idle(transitionSprite: transitionSprite));
		} else {
			if (charState?.wasFlying == true) changeState(new BassFly());
			else if (charState is not Fall) changeState(new Fall());
		}
	}

	public override void landingCode(bool useSound = true) {
		base.landingCode(useSound);
		canRefillFly = true;
	}

	public override bool isInvulnerable(bool ignoreRideArmorHide = false, bool factorHyperMode = false) {
		return base.isInvulnerable(ignoreRideArmorHide, factorHyperMode);
	}

	bool refillFly() { return grounded && canRefillFly; }

	public override void aiAttack(Actor? target) {
		if (target == null) {
			return;
		}
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
			return;
		}
		if (canShoot()) {
			shoot(getChargeLevel());
		}
	}

	public override List<ShaderWrapper> getShaders() {
		List<ShaderWrapper> shaders = new();
		List<ShaderWrapper> baseShaders = base.getShaders();
		ShaderWrapper? palette = null;

		int index = (currentWeapon?.index ?? 0) + 1;
		palette = player.bassPaletteShader;

		palette?.SetUniform("palette", index);
		palette?.SetUniform("paletteTexture", Global.textures["bass_palette_texture"]);

		//We don't apply palette shader on hypermode.
		if (palette != null && !isSuperBass) {
			shaders.Add(palette);
		}
		shaders.AddRange(baseShaders);

		return shaders;
	}

	public override List<byte> getCustomActorNetData() {
		// Get base arguments.
		List<byte> customData = base.getCustomActorNetData();

		// Per-character data.
		int weaponIndex = currentWeapon?.index ?? 255;
		byte ammo = (byte)MathF.Ceiling(currentWeapon?.ammo ?? 0);
		customData.Add((byte)weaponIndex);
		customData.Add(ammo);
		customData.Add((byte)phase);
		customData.Add((byte)evilEnergy[0]);
		customData.Add((byte)evilEnergy[1]);
		customData.Add((byte)evilEnergy[2]);

		bool[] flags = [
			isSuperBass,
			armless,
		];
		customData.Add(Helpers.boolArrayToByte(flags));

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
		phase = data[2];
		evilEnergy[0] = data[3];
		evilEnergy[1] = data[4];
		evilEnergy[2] = data[5];

		bool[] flags = Helpers.byteToBoolArray(data[6]);
		isSuperBass = flags[0];
		armless = flags[1];
	}
}

