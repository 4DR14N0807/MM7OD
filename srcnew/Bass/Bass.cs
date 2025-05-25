using System;
using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;

namespace MMXOnline;

public class Bass : Character {
	// Weapons.
	public BassLoadout loadout;
	public float weaponCooldown;
	public CopyVisionClone? cVclone;
	public SpreadDrillProj? sDrill;
	public SpreadDrillMediumProj? sDrillM;
	public RemoteMineProj? rMine;
	public RemoteMineExplosionProj? rMineExplosion;
	public LoopingSound? wBurnerSound;
	public float wBurnerAngle;
	public int wBurnerAngleMod = 1;
	public float tBladeDashCooldown;
	public int cardsCount = 4;
	public float showNumberTime;
	public int lastCardNumber;
	public SuperBassRP? sbRocketPunch;

	// Modes.
	public bool isSuperBass;
	public const int TrebleBoostCost = 75;
	public int phase;
	public int[] evilEnergy = new int[3] { 0, 0, 0 };
	public const int MaxEvilEnergy = 28;
	public float flyTime;
	public const float MaxFlyTime = 240;
	public bool refillFly;

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
				weapon3 = player.loadout.bassLoadout.weapon3
			};
		}
		this.loadout = loadout;

		maxHealth = (decimal)player.getMaxHealth(charId);
		weapons = getLoadout();
		charge1Time = 40;
		charge2Time = 105;
		charge3Time = 150;
		maxHealth -= (decimal)player.evilEnergyStacks * (decimal)player.hpPerStack;
		health = maxHealth;

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
		isSuperBass = true;
		phase = 0;
		player.changeWeaponSlot(0);
		weapons.Clear();
		weapons.Add(new SBassBuster());
		weapons.Add(new SBassRP());
	}

	public void nextPhase(int level) {
		evilEnergy[phase] = Bass.MaxEvilEnergy;
		phase = level;
		player.pendingEvilEnergyStacks = level;
		changeState(new EnergyIncrease());
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

		if (showNumberTime > 0) drawCardNumber(lastCardNumber);

		//Non-local players end here.
		if (!ownedByLocalPlayer) return;


		Helpers.decrementFrames(ref weaponCooldown);
		Helpers.decrementFrames(ref tBladeDashCooldown);
		Helpers.decrementFrames(ref showNumberTime);
		if (refillFly) {
			Helpers.decrementFrames(ref flyTime);
		}
		if (flyTime > MaxFlyTime) {
			flyTime = MaxFlyTime;
		}
		for (int i = 0; i < evilEnergy.Length; i++) {
			if (evilEnergy[i] > MaxEvilEnergy) evilEnergy[i] = MaxEvilEnergy;
		}
		player.changeWeaponControls();

		if (player.weapon is not WaveBurner || !player.input.isHeld(Control.Shoot, player)) {
			wBurnerAngleMod = 1;
			wBurnerAngle = 0;
			if (wBurnerSound != null) {
				wBurnerSound.stop();
				wBurnerSound = null!;
				//playSound("waveburnerEnd", true);
			}
		}

		// For the shooting animation.
		if (shootAnimTime > 0 || charState is LadderClimb) {
			Helpers.decrementFrames(ref shootAnimTime);
			if (shootAnimTime <= 0) {
				shootAnimTime = 0;
				if (sprite.name.EndsWith("_shoot")) {
					changeSpriteFromName(charState.defaultSprite, false);
				}
			}
		}

		if (isSuperBass) {
			chargeLogic(shoot);
		}
		quickAdaptorUpgrade(); 
	}

	public override (string, int) getBaseHpSprite() {
		return ("hud_health_base", 2);
	}

	public override void renderHUD(Point offset, GameMode.HUDHealthPosition position) {
		offset = offset.addxy(0, 17);
		base.renderHUD(offset, position);

		if (!isSuperBass) return;

		Point energyBarPos = GameMode.getHUDHealthPosition(position, false).add(offset);

		Global.sprites["hud_energy_base"].drawToHUD(phase, energyBarPos.x, energyBarPos.y);
		if (Global.frameCount % 6 >= 3 && charState is EnergyCharge or EnergyIncrease) {
			Global.sprites["hud_energy_eyes"].drawToHUD(phase, energyBarPos.x, energyBarPos.y);
		}
		energyBarPos.y -= 16;
		Point energyStartPos = energyBarPos;
		int[][] index = new int[][] {
			[0,1,2],
			[3,4,5],
			[6,7,8]			
		};

		for (int i = 0; i < phase + 1; i++) {
			int amount =  evilEnergy[i];
			energyBarPos = energyStartPos;
			for (int j = 0; j < MaxEvilEnergy; j++) {
				int k = i == 1 && evilEnergy[i] >= MaxEvilEnergy ? 2 : i;
				if (j < amount) {
					Global.sprites["hud_energy_full"].drawToHUD(index[phase][k], energyBarPos.x, energyBarPos.y);
				}
				else if (i == 0){
					Global.sprites["hud_energy_empty"].drawToHUD(phase, energyBarPos.x, energyBarPos.y);
				}
				energyBarPos.y -= 2;
			}
		}
	
		Global.sprites["hud_energy_top"].drawToHUD(phase, energyBarPos.x, energyBarPos.y);
	}

	public override void render(float x, float y) {
		base.render(x, y);

		if (player.isMainPlayer && (charState is BassFly || flyTime > 0)) {
			float healthPct = Helpers.clamp01((MaxFlyTime - flyTime) / MaxFlyTime);
			float sy = -27;
			float sx = 20;
			if (xDir == -1) sx = 90 - 20;
			drawFlightMeter(healthPct, sx, sy);
		}
	}

	public void quickAdaptorUpgrade() {
		if (!player.input.isHeld(Control.Special2, player)) {
			hyperProgress = 0;
			return;
		}
		if (!(charState is WarpIn) && canGoSuperBass()) {

			player.currency -= TrebleBoostCost;
			changeState(new SuperBassStart(), true);
			return;
		}
		if (hyperProgress < 1) {
			return;
		}
		hyperProgress = 0;
	}

	public void drawFlightMeter(float healthPct, float sx, float sy) {
		float healthBarInnerWidth = 30;
		Color color = Color.Magenta;
		float width = Helpers.clampMax(MathF.Ceiling(healthBarInnerWidth * healthPct), healthBarInnerWidth);
		if (healthPct <= 0.5f) color = Color.Red;

		DrawWrappers.DrawRect(pos.x - 47 + sx, pos.y - 16 + sy, pos.x - 42 + sx, pos.y + 16 + sy, true, Color.Black, 0, ZIndex.HUD - 1, outlineColor: Color.White);
		DrawWrappers.DrawRect(pos.x - 46 + sx, pos.y + 15 - width + sy, pos.x - 43 + sx, pos.y + 15 + sy, true, color, 0, ZIndex.HUD - 1);
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
			"bass_tblade_dash" => MeleeIds.TenguBladeDash,
			"sbass_kick" => MeleeIds.Kick,
			"sbass_soniccrusher" => MeleeIds.SonicCrusher,
			_ => MeleeIds.None
		});
	}

	public override Projectile? getMeleeProjById(int id, Point projPos, bool addToLevel = true) {
		Damager damager = sonicCrusherDamager();
		return id switch {
			(int)MeleeIds.TenguBladeDash => new TenguBladeMelee(
				projPos, player, addToLevel: addToLevel
			),
			(int)MeleeIds.Kick => new GenericMeleeProj(
				new Weapon(), projPos, ProjIds.BassKick, player, 2, Global.halfFlinch, 0.75f * 60,
				addToLevel: addToLevel
			),
			(int)MeleeIds.SonicCrusher => new GenericMeleeProj(
				new Weapon(), projPos, ProjIds.SonicCrusher, player, damager.damage, damager.flinch, 0.75f * 60,
				addToLevel: addToLevel
			),
			_ => null
		};
	}

	public enum MeleeIds {
		None = -1,
		TenguBladeDash,
		Kick,
		SonicCrusher,
	}

	Damager sonicCrusherDamager() {
		float damage = flyTime < MaxFlyTime ? 2 : 1;
		int flinch = flyTime < MaxFlyTime / 2 ? Global.halfFlinch : 0;

		return new Damager(player, damage, flinch, 0);
	}

	public bool canUseTBladeDash() {
		return player.weapon is TenguBlade tb && tb.ammo > 0 &&
		grounded && tBladeDashCooldown <= 0 && flag == null;
	}

	public override bool normalCtrl() {
		if (isSuperBass) {
			if (player.input.isPressed(Control.Jump, player) && !grounded && dashedInAir <= 0) {
				dashedInAir++;
				refillFly = false;
				changeState(new BassFly(), true);
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
			if (player.input.isHeld(Control.Down, player) && phase >= 1) {
				if (!grounded) {
					changeState(new SweepingLaserState(), true);
					return true;
				}
			}
			if (player.input.isHeld(Control.Up, player) && phase >= 1) {
				if (!grounded) {
					changeState(new DarkCometState(), true);
					return true;
				}
			}
			if (grounded) {
				changeState(new BassKick(), true);
				return true;
			} else {
				float? vel = null;
				if (charState is BassFly bfly) vel = bfly.getFlightMove().x;
				changeState(new SonicCrusher(vel), true);
				refillFly = false;
				return true;
			}
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

	// Loadout Stuff
	public List<Weapon> getLoadout() {
		if (Global.level.isTraining() && !Global.level.server.useLoadout || Global.level.is1v1()) {
			return getAllWeapons();
		}
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
		if (sprite.name == getSprite("dash")) {
			return (24, 30);
		}
		return (24, 36);
	}

	public override (float, float) getTerrainColliderSize() {
		if (sprite.name == getSprite("dash")) {
			return (24, 24);
		}
		return (24, 30);
	}

	public override Point getCenterPos() {
		float yCollider = getGlobalColliderSize().Item2 / 2;
		return pos.addxy(0, -yCollider);
	}

	public override bool canCrouch() {
		return false;
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
		//return 3.45f * 60 * getRunDebuffs();
		return 215 * getRunDebuffs();
	}

	public override float getFallSpeed(bool checkUnderwater = true) {
		float modifier = 1;
		if (charState is EnergyCharge or EnergyIncrease) modifier = 0.05f;

		return base.getFallSpeed() * modifier;
	}

	public override bool canAirJump() {
		return dashedInAir == 0 && rootTime <= 0 && charState is not BassShootLadder;
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
		refillFly = true;
	}

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
		List<ShaderWrapper> baseShaders = base.getShaders();
		List<ShaderWrapper> shaders = new();
		ShaderWrapper? palette = null;

		int index = (currentWeapon?.index ?? 0) + 1;
		palette = player.bassPaletteShader;

		palette?.SetUniform("palette", index);
		palette?.SetUniform("paletteTexture", Global.textures["bass_palette_texture"]);

		//We don't apply palette shader on hypermode.
		if (palette != null && !isSuperBass) {
			shaders.Add(palette);
		}
		
		if (shaders.Count == 0) {
			return baseShaders;
		}

		shaders.AddRange(baseShaders);
		return shaders;
	}

	public override List<byte> getCustomActorNetData() {
		// Get base arguments.
		List<byte> customData = base.getCustomActorNetData() ?? [1];

		// Per-character data.
		int weaponIndex = currentWeapon?.index ?? 255;
		byte ammo = (byte)MathF.Ceiling(currentWeapon?.ammo ?? 0);
		customData.Add((byte)weaponIndex);
		customData.Add(ammo);

		bool[] flags = [
			isSuperBass,
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

		bool[] flags = Helpers.byteToBoolArray(data[2]);
		isSuperBass = flags[0];
	}
}

