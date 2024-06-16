using System;
using System.Collections.Generic;
using System.Linq;


namespace MMXOnline;


public class ProtoMan : Character {
	public List<ProtoBusterProj> lemonsOnField = new();
	public float lemonCooldown;
	public int coreMaxAmmo = 28;
	public int coreAmmo;
	public const float coreAmmoMaxCooldown = 30;
	public float coreAmmoIncreaseCooldown;
	public float coreAmmoDecreaseCooldown = coreAmmoMaxCooldown;
	public bool isShieldActive = true;
	public bool overheating;
	public decimal shieldHP = 18;
	public int shieldMaxHP = 18;
	public float healShieldHPCooldown = 15;
	public decimal shieldDamageDebt;

	public ProtoMan(
	 Player player, float x, float y, int xDir,
	 bool isVisible, ushort? netId, bool ownedByLocalPlayer,
	 bool isWarpIn = true
	 ) : base(
	 player, x, y, xDir, isVisible, netId, ownedByLocalPlayer, isWarpIn, false, false
	 ) {
		charId = CharIds.ProtoMan;
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
		return true;
	}

	public bool canShieldDash() {
		if (
			charState is ShieldDash ||
			!grounded
		) return false;

		return true;
	}

	public override string getSprite(string spriteName) {
		return "protoman_" + spriteName;
	}

	public override void changeSprite(string spriteName, bool resetFrame) {
		if (isShieldActive && Global.sprites.ContainsKey(spriteName + "_shield")) {
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

		if (healShieldHPCooldown <= 0 && shieldHP < shieldMaxHP) {
			playSound("heal", forcePlay: true, sendRpc: true);
			shieldHP++;
			healShieldHPCooldown = 15;
			if (shieldHP >= shieldMaxHP) {
				shieldHP = shieldMaxHP;
			}
		}
		if (coreAmmo >= coreMaxAmmo) {
			overheating = true;
		}
		if (isCharging()) {
			coreAmmoIncreaseCooldown += Global.speedMul;
			coreAmmoDecreaseCooldown = 15;
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
	}

	public override void onFlinchOrStun(CharState newState) {
		if (newState is Hurt) addCoreAmmo(3);
		base.onFlinchOrStun(newState);
	}

	public override bool normalCtrl() {
		if (player.dashPressed(out string slideControl) && canShieldDash()) {
			changeState(new ShieldDash(slideControl), true);
		}
		if ((player.input.isPressed(Control.WeaponLeft, player) ||
			player.input.isPressed(Control.WeaponRight, player))
		) {
			if (isShieldActive) {
				isShieldActive = false;
				if (sprite.name.EndsWith("_shield")) {
					changeSprite(sprite.name[..^7], false);
				}
			} else if (shieldHP > 0) {
				isShieldActive = true;
				if (!sprite.name.EndsWith("_shield")) {
					changeSprite(sprite.name + "_shield", false);
				}
			}
		}
		return base.normalCtrl();
	}

	public override bool attackCtrl() {
		bool shootPressed = player.input.isPressed(Control.Shoot, player);
		bool specialPressed = player.input.isPressed(Control.Special1, player);
		if (specialPressed) {
			if (!grounded) {
				changeState(new ProtoAirShoot(), true);
				return true;
			}
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
				addCoreAmmo(-2);
				playSound("buster3", sendRpc: true);
				lemonCooldown = 18;
			}
		}
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
		coreAmmoDecreaseCooldown = coreAmmoMaxCooldown;
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
		// Shield front block check.
		if (isShieldActive && shieldHP > 0 &&
			shootAnimTime == 0 && charState is not Hurt &&
			Damager.hitFromFront(this, actor, attacker, projId ?? -1)
		) {
			// 1 damage scenario.
			// Reduce damage only 50% of the time.
			if (damage < 2) {
				shieldDamageDebt += damage / 2m;
				damage = 0;
				if (shieldDamageDebt >= 1) {
					shieldDamageDebt--;
					shieldHP--;
				}
			}
			// High HP scenario.
			else if (shieldHP + 1 >= damage) {
				shieldHP -= damage - 1;
				damage = 0;
			}
			// Low HP scenario.
			else {
				damage -= shieldHP + 1;
				shieldHP = 0;
			}
			if (shieldHP <= 0) {
				isShieldActive = false;
				if (sprite.name.EndsWith("_shield")) {
					changeSprite(sprite.name[..^7], false);
				}
			}
		}
		// Back shield block check.
		if ((!isShieldActive || shieldHP <= 0 || shootAnimTime <= 0 || charState is Hurt) &&
			Damager.hitFromBehind(this, actor, attacker, projId ?? -1)
		) {
			if (damage < 2) {
				shieldDamageDebt += damage / 2m;
				damage = 0;
				if (shieldDamageDebt >= 1) {
					shieldDamageDebt--;
					damage = 1;
				}
			} else {
				damage--;
			}
		}
		if (damage > 0) {
			base.applyDamage(fDamage, attacker, actor, weaponIndex, projId);
			addRenderEffect(RenderEffectType.Hit, 0.05f, 0.1f);
			playSound("hit", sendRpc: true);
		} else {
			addDamageTextHelper(attacker, (float)damage, player.maxHealth, true);
			playSound("ding", sendRpc: true);
		}
	}
}
