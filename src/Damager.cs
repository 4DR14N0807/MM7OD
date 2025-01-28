using System;
using System.Collections.Generic;

namespace MMXOnline;

public class Damager {
	public Player owner;
	public float damage;
	public float hitCooldownSeconds {
		set => hitCooldown = MathF.Ceiling(value * 60f);
		get => hitCooldown / 60f;
	}
	public float hitCooldown;
	public int flinch; // Number of frames to flinch
	public float knockback;

	public const float envKillDamage = 2000;
	public const float switchKillDamage = 1000;
	public const float ohkoDamage = 500;
	public const float headshotModifier = 2;

	public static Dictionary<int, float> projectileFlinchCooldowns = new Dictionary<int, float>() {
		{ (int)BluesProjIds.LemonOverdrive, 60 * 2},
		{ (int)BluesProjIds.LemonAngled, 60 * 2},
	};

	public static Dictionary<int, int> multiHitLimit = new() {
		{ (int)BluesProjIds.LemonAngled, 2 },
	};
	

	public Damager(Player owner, float damage, int flinch, float hitCooldown, float knockback = 0) {
		this.owner = owner;
		this.damage = damage;
		this.flinch = flinch;
		hitCooldownSeconds = hitCooldown;
		this.knockback = knockback;
	}

	// Normally, sendRpc would default to false, but literally over 20 places need it to true
	// and one place needs it false so in this case, we invert the convention
	public bool applyDamage(
		IDamagable victim, bool weakness, Weapon weapon, Actor actor,
		int projId, float? overrideDamage = null, int? overrideFlinch = null, bool sendRpc = true
	) {
		float newDamage = (overrideDamage != null ? (float)overrideDamage : damage);
		int newFlinch = (overrideFlinch != null ? (int)overrideFlinch : flinch);

		return applyDamage(
			owner, newDamage, hitCooldown, newFlinch, victim as Actor,
			weakness, weapon.index, weapon.killFeedIndex, actor, projId, sendRpc
		);
	}

	public static bool applyDamage(
		Player owner, float damage, float hitCooldown, int flinch,
		Actor victim, bool weakness, int weaponIndex, int weaponKillFeedIndex,
		Actor damagingActor, int projId, bool sendRpc = true
	) {
		if (victim is Character chr && chr.invulnTime > 0) {
			return false;
		}
		if (damagingActor is GenericMeleeProj tgmp &&
			tgmp.owningActor is Character { isDarkHoldState: true }
		) {
			return false;
		}
		string key = projId.ToString() + "_" + owner.id.ToString();

		if (victim is not IDamagable damagable) {
			return false;
		}
		if (multiHitLimit.ContainsKey(projId)) {
			int hitLimit = multiHitLimit[projId];
			for (int i = 0; i < hitLimit; i++) {
				string tempKey = key;
				if (i != 0) {
					tempKey = $"{projId}_h{i}_{owner.id}";
				}
				if (damagable.projectileCooldown.GetValueOrDefault(tempKey) <= 0) {
					key = tempKey;
					break;
				}
			}
		}
		if (!damagable.projectileCooldown.ContainsKey(key)) {
			damagable.projectileCooldown[key] = 0;
		}
		if (damagable.projectileCooldown[key] != 0) {
			return false;
		}
		damagable.projectileCooldown[key] = hitCooldown;

		// Run the RPC on all clients first, before it can modify the parameters, so clients can act accordingly
		if (sendRpc && victim.netId != null && Global.serverClient?.isLagging() == false) {
			byte[] damageBytes = BitConverter.GetBytes(damage);
			byte[] hitCooldownBytes = BitConverter.GetBytes(hitCooldown);
			byte[] victimNetIdBytes = BitConverter.GetBytes((ushort)victim.netId);
			byte[] actorNetIdBytes = BitConverter.GetBytes(damagingActor?.netId ?? 0);
			var projIdBytes = BitConverter.GetBytes(projId);
			byte linkedMeleeId = byte.MaxValue;

			if (damagingActor is GenericMeleeProj gmp &&
				(gmp.netId == null || gmp.netId == 0) &&
				gmp.meleeId != -1
			) {
				linkedMeleeId = (byte)gmp.meleeId;
				if (gmp.owningActor?.netId != null) {
					actorNetIdBytes = BitConverter.GetBytes(gmp.owningActor?.netId ?? 0);
				} else {
					actorNetIdBytes = BitConverter.GetBytes(gmp.owner?.character?.netId ?? 0);
				}
			}
			var byteParams = new List<byte> {
				(byte)owner.id,
				damageBytes[0],
				damageBytes[1],
				damageBytes[2],
				damageBytes[3],
				hitCooldownBytes[0],
				hitCooldownBytes[1],
				hitCooldownBytes[2],
				hitCooldownBytes[3],
				(byte)flinch,
				victimNetIdBytes[0],
				victimNetIdBytes[1],
				weakness ? (byte)1 : (byte)0,
				(byte)weaponIndex,
				(byte)weaponKillFeedIndex,
				actorNetIdBytes[0],
				actorNetIdBytes[1],
				projIdBytes[0],
				projIdBytes[1],
				linkedMeleeId,
			};
			RPC.applyDamage.sendRpc(byteParams.ToArray());
		}

		if (damagable.isInvincible(owner, projId) && damage > 0) {
			victim.playSound("ding");
			if (Helpers.randomRange(0, 50) == 10) {
				victim.addDamageText("Bloqueo! Por 48 horas!", 1);
			}
			return true;
		}

		// Would only get reached due to lag. Otherwise, the owner that initiates the applyDamage.
		// call would have already considered it and avoided entering the method
		// This allows dodge abilities to "favor the defender"
		if (!damagable.canBeDamaged(owner.alliance, owner.id, projId)) {
			return true;
		}

		if (damagable is CrackedWall cw) {
			float? overrideDamage = CrackedWall.canDamageCrackedWall(projId, cw);
			if (overrideDamage != null && overrideDamage == 0 && damage > 0) {
				cw.playSound("ding");
				return true;
			}
			damage = overrideDamage ?? damage;
		}

		if (damagable != null) {
			DamagerMessage? damagerMessage = null;

			var proj = damagingActor as Projectile;
			if (proj != null) {
				damagerMessage = proj.onDamage(damagable, owner);
				if (damagerMessage?.flinch != null) flinch = damagerMessage.flinch.Value;
				if (damagerMessage?.damage != null) damage = damagerMessage.damage.Value;
			}

			switch (projId) {
				case (int)RockProjIds.ScorchWheel:
					damagerMessage = onScorchDamage(damagable, owner, 1);
					break;
				case (int)RockProjIds.ScorchWheelLoop:
					damagerMessage = onScorchDamage(damagable, owner, 1);
					break;
				case (int)RockProjIds.ScorchWheelMove:
					damagerMessage = onScorchDamage(damagable, owner, 2);
					break;
				case (int)RockProjIds.DangerWrap:
					damagerMessage = onDWrapDamage(damagable, owner);
					break;
			}
			if (damagerMessage?.flinch != null) flinch = damagerMessage.flinch.Value;
			if (damagerMessage?.damage != null) damage = damagerMessage.damage.Value;
		}

		// Character section
		bool spiked = false;
		if (victim is Character character) {
			// Ride armor stomp
			bool isStompWeapon = false;
			if (isStompWeapon) {
				character.flattenedTime = 0.5f;
			}
			// Status effects.
			switch (projId) {
				case (int)BluesProjIds.SparkShock: {
					character.paralize(60, 90);
					break;
				}
			}
			float flinchCooldown = 0;
			if (projectileFlinchCooldowns.ContainsKey(projId)) {
				flinchCooldown = projectileFlinchCooldowns[projId];
			}
			// Flinch immunity.
			if (character.ownedByLocalPlayer && character.isFlinchImmune()) {
				flinch = 0;
			}
			// Flinch cooldown.
			if (flinchCooldown > 0 && flinch > 0) {
				int flinchKey = getFlinchKeyFromProjId(projId, owner.id);
				if (!character.flinchCooldown.ContainsKey(flinchKey)) {
					character.flinchCooldown[flinchKey] = 0;
				}
				if (character.flinchCooldown[flinchKey] > 0) {
					flinch = 0;
				} else {
					character.flinchCooldown[flinchKey] = flinchCooldown;
				}
			}

			// On Damage effects.
			if (damage > 0) {
				//Hurt Direction
				int hurtDir = -character.xDir;
				if (damagingActor != null && hitFromBehind(character, damagingActor, owner, projId)) {
					hurtDir *= -1;
				}
				// Flinch above 0.
				if (flinch > 0 && !weakness) {
					victim?.playSound("hurt");
					character.setHurt(hurtDir, flinch, spiked);
				}
				else if (victim is not Blues) {
					victim?.playSound("hit");
				}
			}
		}

		// Rush Jet flinch
		else if (victim is Rush rush) {
			if (flinch > 0 && rush.rushState is RushJetState) {
				rush.changeState(new RushHurt(rush.xDir)); 
			}
		}

		// Misc section
		else {
			if (damage > 0) {
				victim?.playSound("hit");
			}
		}

		if (damage > 0) {
			if (damagingActor == null ||
				victim is not Blues blues || !(
					blues.isShieldFront() && hitFromFront(blues, damagingActor, owner, projId) ||
					!blues.isShieldFront() && hitFromBehind(blues, damagingActor, owner, projId) && damage <= 1
				)
			) {
				victim?.addRenderEffect(RenderEffectType.Hit, 3, 5);
			}
		}
		float finalDamage = damage;
		if (weakness && damage > 0) {
			damage += 1;
		}
		finalDamage *= owner.getDamageModifier();

		damagable?.applyDamage(finalDamage, owner, damagingActor, weaponKillFeedIndex, projId);
		return true;
	}

	public static bool isArmorPiercing(int? projId) {
		if (projId == null) return false;
		return projId switch {
			(int)RockProjIds.SlashClaw => true,
			(int)RockProjIds.DangerWrapExplosion => true,
			(int)RockProjIds.DangerWrapBubbleExplosion => true,
			(int)RockProjIds.ScorchWheelBurn => true,
			(int)RockProjIds.DangerWrapMine => true,
			(int)BluesProjIds.GravityHold => true,
			(int)BluesProjIds.ProtoStrike => true,
			(int)BluesProjIds.BigBangStrike => true,
			(int)BluesProjIds.BigBangStrikeExplosion => true,
			(int)ProjIds.TenguBladeDash => true,
			(int)BassProjIds.TenguBladeDash => true,
			//(int)BassProjIds.TenguBladeProj => true,
			(int)BassProjIds.LightningBolt => true,
			_ => false,
		};
	}

	public static bool isDot(int? projId) {
		if (projId == null) return false;
		return projId switch {
			(int)ProjIds.AcidBurstPoison => true,
			(int)ProjIds.Burn => true,
			_ => false
		};
	}

	private static int getFlinchKeyFromProjId(int projId, int playerId) {
		return (playerId * 10000) + projId;
	}

	public static bool hitFromBehind(Actor actor, Actor? damager, Player? projOwner, int projId) {
		return hitFromSub(
			actor, damager,
			projOwner, projId,
			delegate (Actor actor, float deltaX) {
				if (deltaX != 0 && (
					actor.xDir == -1 && deltaX < 0 ||
					actor.xDir == 1 && deltaX > 0
				)) {
					return true;
				}
				return false;
			},
			delegate (Actor actor, Point damagePos) {
				if (actor.pos.x == damagePos.x ||
					actor.pos.x < damagePos.x + 2 && actor.xDir == -1 ||
					actor.pos.x > damagePos.x - 2 && actor.xDir == 1
				) {
					return true;
				}
				return false;
			}
		);
	}

	public static bool hitFromFront(Actor actor, Actor? damager, Player? projOwner, int projId) {
		return hitFromSub(
			actor, damager,
			projOwner, projId,
			delegate (Actor actor, float deltaX) {
				if (deltaX != 0f && (
					actor.xDir == -1 && deltaX > 0f ||
					actor.xDir == 1 && deltaX < 0f
				)) {
					return true;
				}
				return false;
			},
			delegate (Actor actor, Point damagePos) {
				if (actor.pos.x == damagePos.x ||
					actor.pos.x > damagePos.x - 2 && actor.xDir == -1 ||
					actor.pos.x < damagePos.x + 2 && actor.xDir == 1
				) {
					return true;
				}
				return false;
			}
		);
	}

	private static bool hitFromSub(
		Actor actor, Actor? damager, Player? projOwner, int projId,
		Func<Actor, float, bool> checkDelta,
		Func<Actor, Point, bool> checkPos
	) {
		if (damager == null) {
			return false;
		}
		if (projId >= 0 && (
			projId == (int)ProjIds.Burn ||
			projId == (int)ProjIds.SelfDmg ||
			projId == (int)ProjIds.RumblingBangProj ||
			projId == (int)ProjIds.FlameRoundFlameProj ||
			projId == (int)ProjIds.MaroonedTomahawk ||
			projId == (int)ProjIds.AcidBurstPoison
		)) {
			return false;
		}
		if (damager is not Projectile { isMelee: false }) {
			if (damager.deltaPos.x != 0) {
				if (checkDelta(actor, damager.deltaPos.x)) {
					return true;
				} else {
					return false;
				}
			}
		}
		// Calculate based on other values if speed is 0.
		Point damagePos = damager.pos;

		if (damager is Projectile proj) {
			if (proj.isMelee || proj.isOwnerLinked) {
				if (proj.owningActor != null) {
					damagePos = proj.owningActor.pos;
				} else if (projOwner?.character != null) {
					damagePos = projOwner.character.pos;
				}
			}
		}

		// Call function if pos is not null.
		return checkPos(actor, damagePos);
	}

	private static bool isVictimImmuneToQuake(Actor victim) {
		if (victim is CrackedWall) return false;
		if (!victim.grounded) return true;
		if (victim is Character chr && chr.charState is WallSlide) return true;
		return false;
	}

	public static DamagerMessage? onDWrapDamage(IDamagable damagable, Player attacker) {
		var chr = damagable as Character;
		if (chr != null && chr.ownedByLocalPlayer && !chr.hasBubble) {
			chr.addBubble(attacker);
			chr.playSound("hit", sendRpc: true);
		}

		return null;
	}
	public static DamagerMessage? onAcidDamage(IDamagable damagable, Player attacker, float acidTime) {
		(damagable as Character)?.addAcidTime(attacker, acidTime);
		return null;
	}

	public static DamagerMessage? onScorchDamage(IDamagable damageable, Player attacker, float burnStacks) {
		(damageable as Character)?.addBurnStunStacks(burnStacks, attacker);
		return null;
	}
	// Count for kills and assist even if it does 0 damage.
	public static bool alwaysAssist(int? projId) {
		if (projId == null) {
			return false;
		}
		return (ProjIds)projId switch {
			ProjIds.AcidBurst => true,
			ProjIds.AcidBurstCharged => true,
			ProjIds.CrystalHunter => true,
			ProjIds.ElectricShock => true,
			ProjIds.AirBlastProj => true,
			_ => false
		};
	}
	public static bool lowTimeAssist(int? projId) {
		if (projId == null) {
			return false;
		}
		// The GM19 list now only counts for FFA mode.
		if (Global.level.gameMode is not FFADeathMatch) {
			return false;
		}
		return projId switch {

			_ => false
		};
	}

	public static bool unassistable(int? projId) {
		if (projId == null) {
			return false;
		}
		// Never assist in any mode as they are DOT or self-damage. (Also Volt Tornado)
		bool alwaysNotAssist = (ProjIds)projId switch {
			ProjIds.Burn => true,
			ProjIds.AcidBurstPoison => true,
			ProjIds.SelfDmg => true,
			ProjIds.FlameRoundFlameProj => true,
			ProjIds.BlastLauncherMineGrenadeProj => true, 
			ProjIds.BoundBlasterRadar => true, 
			ProjIds.RayGunChargeBeam => true,
			ProjIds.PlasmaGunBeamProj => true,
			ProjIds.PlasmaGunBeamProjHyper => true,
			ProjIds.VoltTornado => true,
			ProjIds.VoltTornadoHyper => true,
			ProjIds.FlameBurner => true,
			ProjIds.FlameBurnerHyper => true,
			_ => false
		};
		if (alwaysNotAssist) {
			return true;
		}
		// The GM19 list now only counts for FFA mode.
		if (Global.level.gameMode is not FFADeathMatch) {
			return false;
		}
		return projId switch {
			(int)ProjIds.Tornado => true,
			(int)ProjIds.BoomerangCharged => true,
			(int)ProjIds.TornadoFang => true,
			(int)ProjIds.TornadoFang2 => true,
			(int)ProjIds.GravityWell => true,
			(int)ProjIds.SpinWheel => true,
			(int)ProjIds.TriadThunder => true,
			(int)ProjIds.TriadThunderBeam => true,
			(int)ProjIds.DistanceNeedler => true,
			(int)ProjIds.RumblingBangProj => true,
			(int)ProjIds.FlameRoundWallProj => true,
			(int)ProjIds.SplashHitProj => true,
			(int)ProjIds.CircleBlaze => true,
			(int)ProjIds.CircleBlazeExplosion => true,
			(int)ProjIds.BlastLauncherGrenadeSplash => true,
			_ => false
		};
	}

	public static DamagerMessage? onParasiticBombDamage(IDamagable damagable, Player attacker) {
		var chr = damagable as Character;
		if (chr != null && chr.ownedByLocalPlayer && !chr.hasParasite) {
			chr.addParasite(attacker);
			//chr.playSound("parasiteBombLatch", sendRpc: true);
		}

		return null;
	}


	public static bool canDamageFrostShield(int projId) {
		if (CrackedWall.canDamageCrackedWall(projId, null) != 0) {
			return true;
		}
		return projId switch {
			(int)ProjIds.FireWave => true,
			(int)ProjIds.FireWaveCharged => true,
			(int)ProjIds.SpeedBurner => true,
			(int)ProjIds.SpeedBurnerCharged => true,
			(int)ProjIds.FlameRoundProj => true,
			(int)ProjIds.FlameRoundFlameProj => true,
			(int)ProjIds.Ryuenjin => true,
			(int)ProjIds.FlameBurner => true,
			(int)ProjIds.FlameBurnerHyper => true,
			(int)ProjIds.CircleBlazeExplosion => true,
			(int)ProjIds.QuakeBlazer => true,
			(int)ProjIds.QuakeBlazerFlame => true,
			(int)ProjIds.FlameMFireball => true,
			(int)ProjIds.FlameMOilFire => true,
			(int)ProjIds.VelGFire => true,
			(int)ProjIds.SigmaWolfHeadFlameProj => true,
			(int)ProjIds.WildHorseKick => true,
			(int)ProjIds.Sigma3Fire => true,
			(int)ProjIds.FStagDashCharge => true,
			(int)ProjIds.FStagDash => true,
			(int)ProjIds.FStagFireball => true,
			_ => false
		};
	}

	public static bool isBoomerang(int? projId) {
		if (projId == null) return false;
		return projId switch {
			(int)ProjIds.Boomerang => true,
			(int)ProjIds.BoomerangCharged => true,
			(int)ProjIds.BoomerangKBoomerang => true,
			_ => false
		};
	}

	public static bool isSonicSlicer(int? projId) {
		if (projId == null) return false;
		return projId switch {
			(int)ProjIds.SonicSlicer => true,
			(int)ProjIds.SonicSlicerCharged => true,
			(int)ProjIds.SonicSlicerStart => true,
			(int)ProjIds.OverdriveOSonicSlicer => true,
			(int)ProjIds.OverdriveOSonicSlicerUp => true,
			_ => false
		};
	}
}

public class DamagerMessage {
	public int? flinch;
	public float? damage;
}
