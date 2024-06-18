using System;
using System.Collections.Generic;
using System.Linq;


namespace MMXOnline;

public class ProtoMan : Character {
	public List<ProtoBusterProj> lemonsOnField = new();
	public float lemonCooldown;
	public int coreMaxAmmo = 28;
	public int coreAmmo;
	public float coreAmmoMaxCooldown = 60;
	public float coreAmmoIncreaseCooldown;
	public float coreAmmoDecreaseCooldown;
	public bool isShieldActive = true;
	public bool overheating;
	public float overheatEffectTime;
	public decimal shieldHP = 18;
	public int shieldMaxHP = 18;
	public float healShieldHPCooldown = 15;
	public decimal shieldDamageDebt;
	public StarCrashProj starCrash;
	public Weapon specialWeapon;

	public ProtoMan(
	 Player player, float x, float y, int xDir,
	 bool isVisible, ushort? netId, bool ownedByLocalPlayer,
	 bool isWarpIn = true
	 ) : base(
	 player, x, y, xDir, isVisible, netId, ownedByLocalPlayer, isWarpIn, false, false
	 ) {
		charId = CharIds.ProtoMan;
		int protomanLoadout = player.loadout.protomanLoadout.weapon1;

		specialWeapon = protomanLoadout switch {
			0 => new GeminiLaser(),
			1 => new HardKnuckle(),
			2 => new SearchSnake(),
			3 => new SparkShock(),
			4 => new PowerStone(),
			5 => new GyroAttack(),
			_ => new StarCrash(),
		};
	}

	public override float getRunSpeed() {
		float runSpeed = Physics.WalkSpeed;
		if (overheating) {
			runSpeed *= 0.5f;
		} else if (isShieldActive) {
			runSpeed *= 0.75f;
		}
		return runSpeed * getRunDebuffs();
	}

	public override float getJumpPower() {
		float jumpSpeed = Physics.JumpSpeed;
		if (overheating) {
			jumpSpeed *= 0.75f;
		} else if (isShieldActive) {
			jumpSpeed *= 0.85f;
		}
		return jumpSpeed * getJumpModifier();
	}

	public override bool canTurn() {
		if (charState is ProtoAirShoot) return false;
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
		if (charState is ProtoStrike) return false;
		return base.canCharge();
	}

	public bool canBlock() {
		if (!grounded) return false;
		return true;
	}

	public bool canShieldDash() {
		return (
			(grounded || dashedInAir == 0) &&
			charState is not ShieldDash &&
			!overheating && shieldHP > 0
		);
	}

	public bool canShootSpecial() {
		if (isCharging() || 
			specialWeapon.shootCooldown > 0 ||
			starCrash != null) {
			return false;
		}
		return true;
	}

	public void destroyStarCrash() {
		StarCrash sa = new StarCrash();
		if (starCrash != null) starCrash.destroySelf();
		starCrash = null;
		gravityModifier = 1;
		if (specialWeapon != null) specialWeapon.shootCooldown = sa.fireRateFrames;
	}

	public override string getSprite(string spriteName) {
		return "protoman_" + spriteName;
	}

	public override void changeSprite(string spriteName, bool resetFrame) {
		if (isShieldActive && spriteName == "protoman_idle" && getChargeLevel() >= 2) {
			spriteName = "protoman_charge";
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
	}

	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			//"protoman_block" => MeleeIds.ShieldBlock,
			_ => MeleeIds.None
		});
	}

	public override Projectile? getMeleeProjById(int id, Point projPos, bool addToLevel = true) {
		return id switch {
			/*(int)MeleeIds.ShieldBlock => new GenericMeleeProj(
				new Weapon(), projPos, ProjIds.ShieldBlock, player, 0, 0, 0, isShield: true
			),*/

			_ => null
		};
	}

	public override bool chargeButtonHeld() {
		return player.input.isHeld(Control.Shoot, player);
	}

	public override void increaseCharge() {
		float factor = 0.75f;
		chargeTime += Global.spf * factor;
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;

		Helpers.decrementFrames(ref lemonCooldown);
		Helpers.decrementFrames(ref healShieldHPCooldown);
		if (specialWeapon != null) Helpers.decrementFrames(ref specialWeapon.shootCooldown);

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
			playSound("danger_wrap_explosion", sendRpc: true);
			stopCharge();
		}
		if (isCharging()) {
			if (chargeTime <= charge2Time) {
				coreAmmoIncreaseCooldown += Global.speedMul;
				if (coreAmmoDecreaseCooldown < coreAmmoMaxCooldown) {
					coreAmmoDecreaseCooldown = coreAmmoMaxCooldown;
				}
			}
		} else {
			coreAmmoIncreaseCooldown = 0;
			Helpers.decrementFrames(ref coreAmmoDecreaseCooldown);
			if (overheating) {
				Helpers.decrementFrames(ref coreAmmoIncreaseCooldown);
			}
		}
		if (coreAmmoIncreaseCooldown >= 15) {
			if (coreAmmo < coreMaxAmmo) {
				coreAmmo++;
			}
			coreAmmoIncreaseCooldown = 0;
		}
		if (coreAmmoDecreaseCooldown <= 0) {
			coreAmmo--;
			if (coreAmmo <= 0) {
				overheating = false;
				coreAmmo = 0;
			}
			coreAmmoDecreaseCooldown = 15;
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

		if (isShieldActive && getChargeLevel() >= 2 && sprite.name == "protoman_idle_shield") {
			changeSpriteFromName("charge", true);
		}

		if (overheating) {
			overheatEffectTime += Global.speedMul;
			if (overheatEffectTime >= 3) {
				overheatEffectTime = 0;
				Point burnPos = pos.addxy(xDir * 2, -15);

				Anim tempAnim = new Anim(burnPos.addRand(14, 15), "dust", 1, null, true, host: this);
				tempAnim.vel.y = -120;
				tempAnim.addRenderEffect(RenderEffectType.ChargeOrange, 0.033333f, 2);
			}
			addRenderEffect(RenderEffectType.ChargeOrange, 0.033333f, 0.1f);
		}
	}

	public override void onFlinchOrStun(CharState newState) {
		if (newState is Hurt) {
			addCoreAmmo(3);
		}
		base.onFlinchOrStun(newState);
	}

	public override bool normalCtrl() {
		if ((player.input.isPressed(Control.WeaponLeft, player) ||
			player.input.isPressed(Control.WeaponRight, player)
		)) {
			if (isShieldActive) {
				isShieldActive = false;
				if (sprite.name.EndsWith("_shield")) {
					changeSprite(sprite.name[..^7], false);
				}
				if (sprite.name == "protoman_charge") {
					changeSpriteFromName("idle", true);
				}
			} else if (shieldHP > 0) {
				isShieldActive = true;
				if (!sprite.name.EndsWith("_shield")) {
					changeSprite(sprite.name + "_shield", false);
				}
			}
		}
		if (player.dashPressed(out string slideControl) && canShieldDash()) {
			changeState(new ShieldDash(slideControl), true);
			addCoreAmmo(2);
			return true;
		}
		return base.normalCtrl();
	}

	public override bool attackCtrl() {
		bool shootPressed = player.input.isPressed(Control.Shoot, player);
		bool specialPressed = player.input.isPressed(Control.Special1, player);
		bool downHeld = player.input.isHeld(Control.Down, player);

		if (specialPressed) {
			if (canShootSpecial()) shootPoderzinho(getChargeLevel());
			return true;
		}

		if (shootPressed && downHeld && !grounded) {
			changeState(new ProtoAirShoot(), true);
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
		if (chargeLevel == 0) {
			for (int i = lemonsOnField.Count - 1; i >= 0; i--) {
				if (lemonsOnField[i].destroyed) {
					lemonsOnField.RemoveAt(i);
				}
			}
			if (lemonsOnField.Count >= 3) { return; }
		}
		// Cancel non-invincible states.
		if (!charState.attackCtrl && !charState.invincible) {
			changeToIdleOrFall();
		}
		// Shoot anim and vars.
		setShootAnim();
		Point shootPos = getShootPos();
		int xDir = getShootXDir();

		if (chargeLevel < 2) {
			var lemon = new ProtoBusterProj(
				shootPos, xDir, player, player.getNextActorNetId(), rpc: true
			);
			lemonsOnField.Add(lemon);
			playSound("buster", sendRpc: true);
			resetCoreCooldown();
			lemonCooldown = 18;
		} else if (chargeLevel >= 2) {
			if (player.input.isHeld(Control.Up, player)) {
				changeState(new ProtoStrike(), true);
			} else {
				new ProtoBusterChargedProj(
					shootPos, xDir, player, player.getNextActorNetId(), rpc: true
				);
				resetCoreCooldown();
				playSound("buster3", sendRpc: true);
				lemonCooldown = 18;
			}
		}
	}

	public void shootPoderzinho(int chargeLevel) {
		if (specialWeapon == null) {
			return;
		}
		if (!charState.attackCtrl && !charState.invincible) {
			changeToIdleOrFall();
		}
		// Shoot anim and vars.
		setShootAnim();
		Point shootPos = getShootPos();
		int xDir = getShootXDir();
		
		specialWeapon.shoot(this, chargeLevel);
		specialWeapon.shootCooldown = specialWeapon.fireRateFrames;
		addCoreAmmo(MathInt.Ceiling(specialWeapon.getAmmoUsage(chargeLevel)));
	}
 
	public void setShootAnim() {
		string shootSprite = getSprite(charState.shootSprite);
		if (!Global.sprites.ContainsKey(shootSprite)) {
			if (grounded) {
				shootSprite = "protoman_shoot";
			} else {
				shootSprite = "protoman_jump_shoot";
			}
		}
		if (shootAnimTime == 0) {
			changeSprite(shootSprite, false);
		} else if (charState is Idle) {
			frameIndex = 0;
			frameTime = 0;
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

	public void addCoreAmmo(int amount) {
		coreAmmo += amount;
		if (coreAmmo > coreMaxAmmo) coreAmmo = coreMaxAmmo;
		if (coreAmmo < 0) coreAmmo = 0;
		resetCoreCooldown();
	}

	public void resetCoreCooldown() {
		coreAmmoIncreaseCooldown = 0;
		if (coreAmmoDecreaseCooldown < coreAmmoMaxCooldown) {
			coreAmmoDecreaseCooldown = coreAmmoMaxCooldown;
		}
	}

	public bool isShieldFront() {
		if (!ownedByLocalPlayer) {
			return isShieldActive;
		}
		return (
			(isShieldActive || charState is ShieldDash) &&
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
				if (sprite.name == "protoman_charge") {
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
			coreAmmo += MathInt.Ceiling(oldHealth - player.health);
			coreAmmoDecreaseCooldown = coreAmmoMaxCooldown;
		}
		if (ogShieldHP > shieldHP) {
			string damageText = (ogShieldHP - shieldHP).ToString();
			addDamageText(damageText, (int)FontType.Blue);
			RPC.addDamageText.sendRpc(attacker.id, netId, float.Parse(damageText));
		}
	}
}
