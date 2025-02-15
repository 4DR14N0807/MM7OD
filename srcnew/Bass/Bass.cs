using System;
using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;

namespace MMXOnline;

public class Bass : Character {
	// Weapons.
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
	public int[] evilEnergy = new int[2] { 0, 0 };
	public int evilEnergy2;
	public const int MaxEvilEnergy = 28;
	public float flyTime;
	public const float MaxFlyTime = 240;
	bool refillFly;

	// AI Stuff.
	public float aiWeaponSwitchCooldown = 120;

	public Bass(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer,
		bool isWarpIn = true
	) : base(
		player, x, y, xDir, isVisible, netId, ownedByLocalPlayer, isWarpIn, false, false
	) {
		charId = CharIds.Bass;
		weapons = getLoadout();
		charge1Time = 50;
		maxHealth -= (decimal)player.evilEnergyStacks * (decimal)player.hpPerStack;
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
		if (currentWeapon.canHealAmmo && currentWeapon.ammo < currentWeapon.maxAmmo) {
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
		phase = 1;
		player.changeWeaponSlot(0);
		player.weapons.Clear();
		player.weapons.Add(new SBassBuster());
	}

	public void nextPhase(int level) {
		phase = level;
		player.pendingEvilEnergyStacks = level - 1;
	}

	public override void update() {
		base.update();
	
		//Hypermode Music.
		if (!Global.level.isHyper1v1()) {
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

		if (isSuperBass) {
			chargeLogic(shoot);
		}
		//quickAdaptorUpgrade(); 
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
		grounded && tBladeDashCooldown <= 0;
	}

	public override bool normalCtrl() {
		if (player.input.isPressed(Control.Dash, player) && canUseTBladeDash()) {
			changeState(new TenguBladeDash(), true);
			return true;
		}
		if (isSuperBass) {
			if (player.input.isPressed(Control.Jump, player) && !grounded && dashedInAir <= 0) {
				dashedInAir++;
				refillFly = false;
				changeState(new BassFly(), true);
				return true;
			}
			if (
				player.input.isHeld(Control.Special2, player) &&
				charState is not EnergyCharge &&
				(evilEnergy[(int)Helpers.clampMax(phase - 1, 1)] < MaxEvilEnergy)
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

		if (shootPressed) {
			if (weaponCooldown <= 0 && currentWeapon?.canShoot(0, this) == true) {
				shoot(getChargeLevel());
				return true;
			}
		}

		if (isSuperBass && player.input.isPressed(Control.Special1, player)) {
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

		turnToInput(player.input, player);
		if (currentWeapon?.hasCustomAnim == false) {
			if (charState is LadderClimb lc) {
				changeState(new BassShootLadder(lc.ladder), true);
			}
			else if (charState is BassShootLadder bsl) {
				changeState(new BassShootLadder(bsl.ladder), true);
			}
			else {
				changeState(new BassShoot(), true);
			}
		} /* else {
			changeSprite(getSprite(charState.shootSprite), true);
		} */
		currentWeapon?.shoot(this, chargeLevel);
		weaponCooldown = currentWeapon?.fireRate ?? 0;
		currentWeapon?.addAmmo(-currentWeapon?.getAmmoUsage(0) ?? 0, player);
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
		if (Global.level.isTraining() && !Global.level.server.useLoadout) {
			return getAllWeapons();
		} else if (Global.level.is1v1()) {
			return getAllWeapons();
		}
		return getWeaponsFromLoadout(player.loadout.bassLoadout);
	}

	public static List<Weapon> getAllWeapons() {
		return new List<Weapon>() {
			new BassBuster(),
			new IceWall(),
			new CopyVision(),
			new SpreadDrill(),
			new WaveBurner(),
			new RemoteMine(),
			new LightningBolt(),
			new TenguBlade(),
			new MagicCard(),
		};
	}

	public List<Weapon> getWeaponsFromLoadout(BassLoadout loadout) {
		var list = new List<Weapon>();

		list.Add(getAllWeapons()[loadout.weapon1]);
		list.Add(getAllWeapons()[loadout.weapon2]);
		list.Add(getAllWeapons()[loadout.weapon3]);

		return list;
	}

	public static List<Weapon> getRandomLoadout() {
		Random slot0 = new Random(), slot1 = new Random(), slot2 = new Random();
		bool hasRepeatedWeapons = true;
		int[] weaponIndexes = new int[3];
		var indices = new List<byte>();

		while (hasRepeatedWeapons) {
			weaponIndexes[0] = Helpers.randomRange(0, 8);
			weaponIndexes[1] = Helpers.randomRange(0, 8);
			weaponIndexes[2] = Helpers.randomRange(0, 8);

			if (weaponIndexes[0] != weaponIndexes[1]
				&& weaponIndexes[0] != weaponIndexes[2]
				&& weaponIndexes[1] != weaponIndexes[2]) hasRepeatedWeapons = false;
		}

		indices.Add((byte)weaponIndexes[0]);
		indices.Add((byte)weaponIndexes[1]);
		indices.Add((byte)weaponIndexes[2]);

		List<Weapon> nullableWeapons = indices.Select(index => {
			return getAllWeapons().Find(w => w.index == index)!.clone();
		}).ToList();
		nullableWeapons.RemoveAll(item => item == null);

		return nullableWeapons;
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
			return (24, 24);
		}
		return (24, 30);
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
			charState is Dash ||
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
		return 2;
	}
	/* public override bool canChangeWeapons() {
		return base.canChangeWeapons() && charState is not LightningBoltState;
	} */

	public override bool canAirJump() {
		return dashedInAir == 0 && rootTime <= 0 && charState is not BassShootLadder;
	}

	public override bool canAirDash() {
		return false;
	}

	public override bool canWallClimb() {
		return false;
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
		List<byte> customData = base.getCustomActorNetData() ?? new();

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
		base.updateCustomActorNetData(data);
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

