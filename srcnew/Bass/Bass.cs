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
	public int hyperModeNum;
	public bool isTrebbleBoost;
	public bool isSuperBass;
	public const int TrebleBoostCost = 75;
	public int phase;
	public float evilEnergy = 0;
	public float maxEvilEnergy = 16;
	public float flyTime;
	public float MaxFlyTime = 240;
	public bool canRefillFly;
	public float sonicCrusherCooldown;
	public float evilEnergyEffectTime;
	public Sprite? evilAura;
	public bool evilAuraActive;
	public bool onSpecialAttack;
	public float superBassMusicTime;
	public float superBassMusicStacks;

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
				hypermode = player.loadout.bassLoadout.hypermode,
			};
		}
		this.loadout = loadout;
		hyperModeNum = loadout.hypermode;
		weapons = getLoadout();

		maxHealth = (decimal)player.getMaxHealth(charId);
		maxHealth -= player.evilEnergyHP;
		health = maxHealth;

		charge1Time = 40;
		charge2Time = 105;
		charge3Time = 180;

		addAttackCooldown(
			(int)AttackIds.Kick,
			new AttackCooldown(
				(int)BassWeaponIds.BassKick,
				"hud_weapon_icon_bass", 75
			)
		);
		addAttackCooldown(
			(int)AttackIds.SweepingLaser,
			new AttackCooldown(
				(int)BassWeaponIds.SweepingLaser,
				"hud_weapon_icon_bass", 90
			)
		);
		addAttackCooldown(
			(int)AttackIds.DarkComet,
			new AttackCooldown(
				(int)BassWeaponIds.DarkComet,
				"hud_weapon_icon_bass", 90
			)
		);
		addAttackCooldown(
			(int)AttackIds.LowerEvilness,
			new AttackCooldown(
				14, "hud_weapon_icon_bass", 60 * 4
		));

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
			!isSuperBass && !isTrebbleBoost && player.currency >= TrebleBoostCost &&
			player.evilEnergyStacks <= 0 && player.pendingEvilEnergyStacks <= 0
		);
	}

	public void setSuperBass() {
		if (isSuperBass || isTrebbleBoost) {
			return;
		}
		if (hyperModeNum == 0) {
			isTrebbleBoost = true;
		} else {
			isSuperBass = true;
		}
		if (!isTrebbleBoost) {
			maxHealth += 2;
		}
		heal(player, (float)(maxHealth - health));
		phase = 0;
		evilEnergy = 0;
		maxEvilEnergy = Math.Min(MathF.Floor(10 + phase * 3f), 18);
		player.changeWeaponSlot(0);
		weapons.Clear();
		weapons.Add(new SBassBuster());
		weapons.Add(new SBassRP());
	}

	public void addEvilness(float ammo) {
		if (!isTrebbleBoost && phase >= 3) {
			return;
		}
		evilEnergyEffectTime = 8;
		evilEnergy += ammo;
		if (evilEnergy >= maxEvilEnergy && isTrebbleBoost) {
			float excessEnergy = evilEnergy - maxEvilEnergy;
			if (phase >= 4) {
				changeState(new BassEvilOverload());
				wince(60 * 2, 0, 0, player.id);
				heal(player, 3);
				playSound("super_bass_aura", sendRpc: true);
				playSound("hurt", sendRpc: true);
				int rand = Helpers.randomRange(0, 10);
				string text = rand switch {
					1 => "ugh...",
					2 => "darn...",
					3 => "this power...",
					4 => "too much...",
					5 => "need more...",
					_ => "...",
				};
				addDamageText(text, (int)FontType.Purple);
			} else {
				changeState(new EnergyIncrease());
				nextPhase(phase + 1);
			}
			evilEnergy = Helpers.clampMax(excessEnergy, maxEvilEnergy - 2);
		}
	}

	public void removeEvilness(float ammo) {
		evilEnergy -= ammo;
		if (evilEnergy <= 0) {
			evilEnergy = 0;
		}
	}

	public void nextPhase(int level) {
		if (phase >= 4 || phase >= level) {
			return;
		}
		// Incrase level.
		phase = level;
		player.pendingEvilEnergyStacks = level;
		// Add HP and heal.
		int hpToAdd = phase >= 4 ? 3 : 2;

		maxHealth += hpToAdd;
		heal(player, hpToAdd);
		maxEvilEnergy = Math.Min(MathF.Floor(10 + phase * 3f), 18);

		if (phase < 4) {
			addDamageText("POWER\n    UP!", (int)FontType.PurpleMenu);
		} else {
			if (Helpers.randomRange(0, 3) == 0) {
				addDamageText("too..\n   much...\n      power...", (int)FontType.Purple);
			} else {
				addDamageText("POWER\n  OVERLOAD!", (int)FontType.PurpleMenu);
			}
		}
		setSuperMusicTime(60 * 10);
	}

	public void lowerPhase(int level) {
		if (phase >= 4 || phase <= level) {
			return;
		}
		int ogPhase = phase;
		// Decrease level.
		phase = level;
		player.pendingEvilEnergyStacks = level;
		if (phase < 0) {
			phase = 0;
			evilEnergy = 0;
		}
		// Remove HP and clamp.
		if (phase < ogPhase) {
			maxHealth -= 2;
		}
		health = Math.Min(health, maxHealth);
		maxEvilEnergy = Math.Min(MathF.Floor(10 + phase * 3f), 18);
		addDamageText("POWER\n  DOWN!", (int)FontType.PurpleMenu);
		setSuperMusicTime(60 * 10);
	}

	public void setSuperMusicTime(float time) {
		superBassMusicTime = Math.Max(superBassMusicTime, time);
	}

	public override void preUpdate() {
		base.preUpdate();

		if (evilAuraActive) {
			evilAura ??= new Sprite("sbass_dmgaura");
			evilAura.update();
		} else if (evilAura != null) {
			evilAura = null;
		}

		if (!ownedByLocalPlayer) {
			return;
		}

		onSpecialAttack = charState is SweepingLaserState or DarkCometState;

		Helpers.decrementFrames(ref sonicCrusherCooldown);
		Helpers.decrementFrames(ref evilEnergyEffectTime);
		Helpers.decrementFrames(ref superBassMusicTime);
		if (evilAuraActive && charState is not BassEvilOverflow) {
			removeEvilness(speedMul / 20);
			if (evilEnergy <= 0) {
				evilAuraActive = false;
				if (charState.normalCtrl || charState.attackCtrl || charState is Hurt or GenericStun) {
					playSound("super_bass_aura", sendRpc: true);
					changeState(new BassEvilOverload());
					wince(60 * 2, 0, 0, player.id);
				}
			}
		}
	}

	public override void update() {
		base.update();

		//Hypermode Music.
		if (Global.level.enabledBreakmanMusic()) {
			if (isSuperBass || isTrebbleBoost) {
				if (musicSource == null) {
					addMusicSource("basstheme", getCenterPos(), true);
				} else {
					if (phase >= 4 || superBassMusicTime > 0) {
						if (!musicSource.isPlaying()) { musicSource.play(); }
					} else if (musicSource.isPlaying()) {
						musicSource.pause();
					}
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
		armless = sbRocketPunch?.destroyed == false;
		if (flyTime > MaxFlyTime) {
			flyTime = MaxFlyTime;
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

		if (isSuperBass || isTrebbleBoost) {
			chargeLogic(shootSuper);
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
			RenderEffectType renderGfx = level switch {
				1 => RenderEffectType.ChargeBlue,
				2 => RenderEffectType.ChargeGreenBass2,
				3 => RenderEffectType.ChargePurpleBass,
				_ => RenderEffectType.None,
			};
			chargeRE = addRenderEffect(renderGfx, 3, 5);
			chargeEffect.character = this;
			chargeEffect.update(level, 0);
		} else {
			chargeRE = null;
		}
	}

	public override void onEnemyDamage(float amount) {
		base.onEnemyDamage(amount);
		if (!ownedByLocalPlayer) {
			return;
		}
		if (isSuperBass || isTrebbleBoost) {
			addEvilness(amount / 2f);
		}
		if (superBassMusicTime > 0) {
			setSuperMusicTime(60 * 4);
			superBassMusicStacks = 0;
		} else {
			superBassMusicStacks += amount;
			if (superBassMusicStacks >= 6) {
				superBassMusicStacks = 0;
				setSuperMusicTime(60 * 8);
			}
		}
	}

	public override void onKill(bool isAssist, Player? enemy, Actor? damager, Character? enemyChar) {
		// Not needed as Bass gain extra DMG EXP on kills anyway.
		// if (isSuperBass || isTrebbleBoost) { addEvilness(2); }
		setSuperMusicTime(60 * 15);
		base.onKill(isAssist, enemy, damager, enemyChar);
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
		decimal shield = netShieldHp >= 0 ? netShieldHp : (shieldManager.totalHealth / modifier);
		decimal savings = curHP + (damageSavings / modifier);

		for (var i = 0; i < Math.Ceiling(maxHP); i++) {
			// Draw HP
			if (i < shield && i < savings) {
				Global.sprites["hud_weapon_full_blues"].drawToHUD(3, baseX, baseY);
			}
			else if (i < curHP) {
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
				if (i < shield) {
					float alpha = (float)(shield % 1);
					Global.sprites["hud_weapon_full_blues"].drawToHUD(3, baseX, baseY, alpha);
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
		if (isSuperBass || isTrebbleBoost) {
			renderSuperAmmo(offset, position);
			return;
		}
		base.renderAmmo(offset, position, weaponOverride);
	}

	public void renderSuperAmmo(Point offset, GameMode.HUDHealthPosition position) {
		Point energyBarPos = GameMode.getHUDHealthPosition(position, false).add(offset);
		int displayPhase = Math.Min(phase, 3);

		Global.sprites["hud_energy_base"].drawToHUD(displayPhase, energyBarPos.x, energyBarPos.y);
		bool active = false;

		if (evilEnergyEffectTime > 0 ||
			charState is EnergyCharge or EnergyIncrease ||
			phase >= 4 || isSuperBass && phase >= 3
		) {
			active = true;
		}
		if (active && Global.floorFrameCount % 4 < 2) {
			Global.sprites["hud_energy_eyes"].drawToHUD(displayPhase, energyBarPos.x, energyBarPos.y);
		}
		energyBarPos.y -= 16;

		int[][] index = [
			[0, 1, 2],
			[3, 4, 5],
			[6, 7, 8],
			[9, 10, 11],
			[9, 10, 11]
		];
		int subIndex = phase % 3;
		int subAltIndex = (phase - 1 + 3) % 3;
		if (active) {
			subIndex = (subIndex + MathInt.Floor(Global.floorFrameCount / 4f)) % 3;
		}
		float fEnergy = MathF.Floor(evilEnergy);
		float cEnergy = MathF.Ceiling(evilEnergy);
		float ceAlpha = evilEnergy - fEnergy;

		for (int i = 0; i < maxEvilEnergy; i++) {
			if (i < fEnergy) {
				Global.sprites["hud_energy_full"].drawToHUD(
					index[displayPhase][subIndex], energyBarPos.x, energyBarPos.y
				);
			} else {
				if (displayPhase > 0 && phase < 4) {
					Global.sprites["hud_energy_full"].drawToHUD(
						index[displayPhase][subAltIndex], energyBarPos.x, energyBarPos.y, 0.5f
					);
				}
				Global.sprites["hud_energy_empty"].drawToHUD(displayPhase, energyBarPos.x, energyBarPos.y);

				if (i < cEnergy) {
					Global.sprites["hud_energy_full"].drawToHUD(
						index[displayPhase][subIndex], energyBarPos.x, energyBarPos.y, ceAlpha
					);
				}
			}
			energyBarPos.y -= 2;
		}
	
		Global.sprites["hud_energy_top"].drawToHUD(displayPhase, energyBarPos.x, energyBarPos.y);

		if (player.isMainPlayer && (charState is BassFly || flyTime > 0) && alive) {
			decimal maxLength = 14;
			decimal length = (maxLength * (decimal)(MaxFlyTime - flyTime)) / (decimal)MaxFlyTime;
			int color = 4;
			if (length <= maxLength * (1 / 2)) color = 3;
			drawBarVHUD(length, maxLength, color, pos.addxy(-20 * xDir, -27));
		}
	}

	public override Point renderMiniHUD(Point offset) {
		return base.renderMiniHUD(offset);
	}

	public override (float ammo, float maxAmmo) getMiniHudAmmo() {
		if (isSuperBass || isTrebbleBoost) {
			return (evilEnergy, maxEvilEnergy);
		}
		return base.getMiniHudAmmo();
	}

	public override int getMiniWeaponLength() {
		if (isSuperBass || isTrebbleBoost) {
			return MathInt.Ceiling(maxEvilEnergy / getMiniHudScale());
		}
		return base.getMiniWeaponLength();
	}

	public void quickHyperUpgrade() {
		if (isSuperBass || isTrebbleBoost || !alive || !canGoSuperBass() ||
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
				new Weapon(), projPos, ProjIds.BassKick, player, 2, Global.halfFlinch, 45,
				addToLevel: addToLevel
			),
			(int)MeleeIds.SonicCrusher => new GenericMeleeProj(
				new Weapon(), projPos, ProjIds.SonicCrusher, player, 2, Global.halfFlinch, 60,
				addToLevel: addToLevel
			),
			_ => null
		};
	}

	public enum MeleeIds {
		None = -1,
		SuperAura,
		TenguBlade,
		TenguBladeDash,
		Kick,
		SonicCrusher,
	}

	public enum AttackIds {
		Kick,
		DarkComet,
		SweepingLaser,
		LowerEvilness,
	}

	public override Dictionary<int, Func<Projectile>> getGlobalProjs() {
		if (evilAuraActive && globalCollider != null) {
			Dictionary<int, Func<Projectile>> retProjs = new() {
				[(int)ProjIds.AwakenedAura] = () => {
					Projectile proj = new GenericMeleeProj(
						BassBuster.netWeapon, pos,
						ProjIds.AwakenedAura, player,
						1, 0, 30, addToLevel: true
					) {
						globalCollider = new Collider(
							new Rect(0f, 0f, 52, 58).getPoints(),
							false, this, false, false,
							HitboxFlag.Hitbox, Point.zero
						),
						meleeId = (int)MeleeIds.SuperAura,
						ownerActor = this
					};
					return proj;
				}
			};
			return retProjs;
		}
		return base.getGlobalProjs();
	}

	public bool canUseTBladeDash() {
		return player.weapon is TenguBlade tb && tb.ammo > 0 &&
		grounded && tBladeDashCooldown <= 0 && !isMovementLimited();
	}

	public override bool normalCtrl() {
		if (isSuperBass || isTrebbleBoost) {
			if (!isMovementLimited() &&
				player.input.isPressed(Control.Jump, player) &&
				!grounded && !canAirJump() && flyTime < MaxFlyTime) {
				dashedInAir++;
				changeState(new BassFly(), false);
				return true;
			}
			if ((phase < 3 || isTrebbleBoost) && player.input.isPressed(Control.Special2, player)) {
				int yDir = player.input.getYDir(player);
				if (isTrebbleBoost && yDir == 1 && phase < 4) {
					if (isCooldownOver((int)AttackIds.LowerEvilness) && grounded) {
						int ogPhase = phase;
						lowerPhase(phase - 1);
						evilEnergy = Helpers.clamp(evilEnergy - 2, 0, maxEvilEnergy - 2);
						playSound("super_bass_aura", sendRpc: true);
						if (ogPhase > 0) {
							addHealth(1);
						}
						changeState(new BassEvilRelease());
						triggerCooldown((int)AttackIds.LowerEvilness);
					}
				} else if (evilEnergy >= maxEvilEnergy && isSuperBass && phase < 3) {
					changeState(new EnergyIncrease());
					nextPhase(phase + 1);
					if (phase >= 3) {
						evilEnergy = maxEvilEnergy;
					} else {
						evilEnergy = 0;
					}
				} else if (phase >= 4) {
					if (evilAuraActive) {
						evilAuraActive = false;
						playSound("super_bass_aura", sendRpc: true);
						changeState(new BassEvilOverload());
						wince(60 * 2, 0, 0, player.id);
						return true;
					} else if (evilEnergy >= 2) {
						evilAuraActive = true;
						playSound("super_bass_aura", sendRpc: true);
						changeState(new BassEvilOverflow());
						return true;
					}
				} else if ((yDir != 1 || phase >= 4 || isSuperBass) && charState is not EnergyCharge) {
					changeState(new EnergyCharge(), true);
					return true;
				}
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
				if (!isSuperBass && !isTrebbleBoost) {
					shoot(getChargeLevel());
				} else {
					shootSuper(getChargeLevel());
				}
				return true;
			}
		}

		if ((isSuperBass || isTrebbleBoost) && player.input.isPressed(Control.Special1, player)) {
			int yInput = player.input.getYDir(player);

			if (yInput == 1 && phase >= 3 && !grounded) {
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
			if (grounded && yInput == -1 && phase >= 1) {
				if (isCooldownOver((int)AttackIds.Kick)) {
					changeState(new BassKick(), true);
					return true;
				}
				return false;
			}
			if (flyTime < MaxFlyTime && sonicCrusherCooldown <= 0 && charState is not SonicCrusher) {
				if (!isMovementLimited()) {
					Point spd = Point.zero;
					if (charState is BassFly bfly) {
						spd.x = bfly.getFlightMove().x;
					}
					changeState(new SonicCrusher(spd.addxy(xPushVel + xIceVel, 0)), true);
				}
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
				changeState(new BassShootLadder(lc.ladder));
			}
			else if (charState is BassShootLadder bsl) {
				changeState(new BassShootLadder(bsl.ladder));
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

	public void shootSuper(int chargeLevel) {
		if (!ownedByLocalPlayer || currentWeapon == null) {
			return;
		}
		turnToInput(player.input, player);
		if (!currentWeapon.hasCustomAnim) {
			if (charState is LadderClimb lc) {
				changeState(new BassShootLadder(lc.ladder));
			} else if (charState is BassShootLadder bsl) {
				changeState(new BassShootLadder(bsl.ladder));
			} else {
				if (charState is Dash or AirDash) {
					changeToIdleOrFall();
				} else if (charState is SonicCrusher) {
					changeState(new BassFly(), true);
				}
				string shootSprite = getSprite(charState.shootSprite);
				if (!Global.sprites.ContainsKey(shootSprite)) {
					if (grounded) {
						shootSprite = getSprite("shoot");
					} else {
						shootSprite = getSprite("jump_shoot");
					}
				}
				if (shootAnimTime == 0) {
					shootAnimTime = 18;
					changeSprite(shootSprite, false);
				}
				if (shootSprite == getSprite("shoot") || charState is BassFly) {
					frameIndex = 0;
					frameTime = 0;
					animTime = 0;
				}
				shootAnimTime = 18;
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


	public override Point getCamCenterPos(bool ignoreZoom = false) {
		Point basePos = base.getCamCenterPos(ignoreZoom);

		if (charState is LBoltBassCharge lstate && lstate.aim != null) {
			return (basePos + ((lstate.aim.pos - basePos) / 2)).round();
		}
		if (charState is LBoltBassShoot lsstate) {
			return (basePos + ((lsstate.shootPos - basePos) / 2)).round();
		}
		return basePos;
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
		if ((isSuperBass || isTrebbleBoost) && Global.sprites.ContainsKey("sbass_" + spriteName)) {
			return "sbass_" + spriteName;
		}
		return "bass_" + spriteName;
	}

	public override (float, float) getGlobalColliderSize() {
		if (sprite.name == getSprite("dash") || sprite.name == getSprite("tblade_dash")) {
			return (24, 30);
		}
		if (sprite.name == getSprite("soniccrusher")) return (38, 20);

		return (24, 36);
	}

	public override (float, float) getTerrainColliderSize() {
		if (sprite.name == getSprite("dash") || sprite.name == getSprite("tblade_dash")) {
			return (24, 24);
		}

		if (sprite.name == getSprite("soniccrusher")) {
			return (24, 20);
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

	public override bool canClimbLadder() {
		if (armless) return false;

		return base.canClimbLadder();
	}

	public override bool canMove() {
		if (!isSuperBass && !isTrebbleBoost && shootAnimTime > 0 && grounded) {
			return false;
		}
		return base.canMove();
	}

	public override bool canShoot() {
		if (weaponCooldown > 0 ||
			sbRocketPunch?.destroyed == false
		) {
			return false;
		}
		return base.canShoot();
	}

	public override bool canCharge() {
		return base.canCharge() && (isSuperBass || isTrebbleBoost) && charState.attackCtrl;
	}

	public override bool chargeButtonHeld() {
		return player.input.isHeld(Control.Shoot, player);
	}

	public override int getMaxChargeLevel() {
		if ((isSuperBass || isTrebbleBoost) && phase >= 2) return 3;
		return 2;
	}

	public override float getRunSpeed() {
		return Physics.WalkSpeed * getRunDebuffs();
	}
	public override float getDashSpeed() {
		return 3.5f * getRunDebuffs();
	}

	public override float getFallSpeed(bool checkUnderwater = true) {
		float modifier = 1;
		if (charState is EnergyCharge or EnergyIncrease) modifier = 0.05f;

		return base.getFallSpeed() * modifier;
	}

	public override bool canAirJump() {
		return (
			dashedInAir == 0 && rootTime <= 0 &&
			charState is not BassShootLadder && !isSuperBass && !isTrebbleBoost
		);
	}

	public override bool canAirDash() {
		return (isSuperBass || isTrebbleBoost) && dashedInAir <= 0 && phase >= 3 && !isMovementLimited();
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
			if (isSuperBass) {
				shoot(getChargeLevel());
			} else {
				shootSuper(getChargeLevel());
			}
		}
	}

	public override void render(float x, float y) {
		base.render(x, y);
		if (evilAura != null) {
			float aalpha = Global.floorFrameCount % 4 < 2 ? 1 : 0.2f;
			evilAura.drawSimple(pos.addxy(x, y + 2), xDir, zIndex - 1, aalpha, this, getShaders());
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
		if (palette != null && !isSuperBass && !isTrebbleBoost) {
			shaders.Add(palette);
		} else if (isSuperBass || isTrebbleBoost) {
			ShaderWrapper? palette2 = player.superBassPaletteShader;
			palette2?.SetUniform("palette", phase + 1);
			palette2?.SetUniform("paletteTexture", Global.textures["bass_superadaptor_palette"]);
			if (palette2 != null) shaders.Add(palette2);
		}
		if (player.superBassBodyShader != null && (
			(onSpecialAttack || evilAuraActive) && Global.floorFrameCount % 4 >= 2 ||
			getChargeLevel() > 0 && chargeRE?.isFlashing() != false
		)) {
			ShaderWrapper palette3 = player.superBassBodyShader;
			palette3.SetUniform("palette", phase + 1);
			shaders.Add(palette3);
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
		customData.Add((byte)evilEnergy);

		bool[] flags = [
			isSuperBass,
			armless,
			isTrebbleBoost,
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
		evilEnergy = data[3];

		bool[] flags = Helpers.byteToBoolArray(data[4]);
		isSuperBass = flags[0];
		armless = flags[1];
		isTrebbleBoost = flags[2];
	}
}

