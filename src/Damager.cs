using System;
using System.Collections.Generic;
using ProtoBuf;

namespace MMXOnline;

public class Damager {
	public Player owner;
	public float damage;
	public float hitCooldownSeconds {
		set => hitCooldown = MathF.Ceiling(value * 60f);
		get => hitCooldown / 60f;
	}
	public float hitCooldown;
	public float excludeSameHit;
	public int flinch; // Number of frames to flinch
	public float knockback;

	public const float envKillDamage = 2000;
	public const float switchKillDamage = 1000;
	public const float ohkoDamage = 500;
	public const float forceKillDamage = 999;
	public const float headshotModifier = 2;

	public static Dictionary<int, float> projectileFlinchCooldowns = new Dictionary<int, float>() {
		{ (int)BluesProjIds.LemonOverdrive, 60 * 2},
		{ (int)BluesProjIds.LemonAngled, 60 * 2},
		{ (int)BluesProjIds.SparkShock, 100},
		{ (int)BluesProjIds.ProtoLandPush, 60 * 1},
	};

	public static Dictionary<int, int> multiHitLimit = new() {
		{ (int)BluesProjIds.LemonAngled, 3 },
		{ (int)BluesProjIds.LemonOverdrive, 3 },
	};
	
	public static Dictionary<int, int> gfxCooldown = new() {
		{ (int)BassProjIds.WaveBurner, 8 },
		{ (int)BassProjIds.WaveBurnerUnderwater, 8 },
	};


	public Damager(Player owner, float damage, int flinch, float hitCooldown, float knockback = 0) {
		this.owner = owner;
		this.damage = damage;
		this.flinch = flinch;
		this.hitCooldownSeconds = hitCooldown;
		this.knockback = knockback;
	}

	// Normally, sendRpc would default to false, but literally over 20 places need it to true
	// and one place needs it false so in this case, we invert the convention
	public bool applyDamage(
		IDamagable victim, bool weakness, Weapon weapon, Actor actor,
		int projId, float? overrideDamage = null, int? overrideFlinch = null, bool sendRpc = true
	) {
		float newDamage = (overrideDamage ?? damage);
		int newFlinch = (overrideFlinch ?? flinch);

		bool didDamage = applyDamage(
			owner, newDamage, hitCooldown, newFlinch, victim.getActor,
			weakness, weapon.index, weapon.killFeedIndex, actor, projId, sendRpc
		);
		if (didDamage && actor is Projectile proj) {
			proj.afterDamage(victim, didDamage);
		}
		return didDamage;
	}

	public static bool applyDamage(
		Player owner, float damage, float hitCooldown, int flinch,
		Actor victim, bool weakness, int weaponIndex, int weaponKillFeedIndex,
		Actor? damagingActor, int projId, bool sendRpc = true
	) {
		if (owner == null) {
			throw new Exception("Null damage player source. Use stage or self if not from another player.");
		}
		if (victim is Character chr && chr.invulnTime > 0) {
			return false;
		}
		if (damagingActor is GenericMeleeProj tgmp &&
			tgmp.ownerActor is Character { isDarkHoldState: true }
		) {
			return false;
		}
		string key = $"{projId}_{owner.id}";
		if (projId == (int)BassProjIds.IceWallLemon) {
			key = $"{(int)BassProjIds.BassLemon}_{owner.id}";
		}

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

		Character? preCharacter = victim as Character;
		CharState? charState = preCharacter?.charState;

		if (!damagable.projectileCooldown.ContainsKey(key)) {
			damagable.projectileCooldown[key] = 0;
		}
		if (damagable.projectileCooldown[key] != 0) {
			if (projId == (int)BassProjIds.BassLemon) {
				damage = 0;
			} else {
				return false;
			}
		} else {
			damagable.projectileCooldown[key] = hitCooldown;
		}
		damagable.projectileCooldown[key] = hitCooldown;

		if (damagable.isInvincible(owner, projId) && damage > 0) {
			victim.playSound("ding");
			if (Helpers.randomRange(0, 50) == 10) {
				victim.addDamageText("Bloqueo! Por 48 horas!", 1);
			}
			return true;
		}

		// Would only get reached client-side due to lag.
		// Otherwise, the owner that initiates the applyDamage call
		// would have already considered it and avoided entering the method
		// This allows dodge abilities to "favor the defender"
		if (!damagable.canBeDamaged(owner.alliance, owner.id, projId)) {
			return true;
		}

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
				if (gmp.ownerActor?.netId != null) {
					actorNetIdBytes = BitConverter.GetBytes(gmp.ownerActor?.netId ?? 0);
				} else {
					actorNetIdBytes = BitConverter.GetBytes(gmp.owner.character?.netId ?? 0);
				}
			}
			var byteParams = new List<byte> {
				(byte)owner.id, // 0
				damageBytes[0], // 1
				damageBytes[1],
				damageBytes[2],
				damageBytes[3],
				hitCooldownBytes[0], // 5
				hitCooldownBytes[1],
				hitCooldownBytes[2],
				hitCooldownBytes[3],
				(byte)flinch, // 9
				victimNetIdBytes[0], // 10
				victimNetIdBytes[1],
				weakness ? (byte)1 : (byte)0, // 12
				(byte)weaponIndex, // 13
				(byte)weaponKillFeedIndex,
				actorNetIdBytes[0], // 15
				actorNetIdBytes[1],
				projIdBytes[0], // 17
				projIdBytes[1],
				linkedMeleeId, // 19
			};
			RPC.applyDamage.sendRpc(byteParams.ToArray());
		}

		if (damagable != null && damagable is not CrackedWall && owner.ownedByLocalPlayer && !isDot(projId)) {
			if (owner.isMainPlayer) {
				owner.delayETank();
			}
			if (damagingActor is Projectile proj && proj.ownerActor is Character chara) {
				chara.enterCombat();
			} else {
				owner.character?.enterCombat();
			}
		}

		if (damagable is CrackedWall cw) {
			float? overrideDamage = CrackedWall.canDamageCrackedWall(damage, cw);
			if (overrideDamage != null && overrideDamage == 0 && damage > 0) {
				cw.playSound("ding");
				return true;
			}
			damage = overrideDamage ?? damage;
		}

		if (damagable != null) {
			DamagerMessage? damagerMessage = null;
			Projectile? proj = damagingActor as Projectile;

			if (proj != null) {
				damagerMessage = proj.onDamage(damagable, owner);
				if (damagerMessage?.flinch != null) flinch = damagerMessage.flinch.Value;
				if (damagerMessage?.damage != null) damage = damagerMessage.damage.Value;
			}

			switch (projId) {
				case (int)RockProjIds.ScorchWheel:
				case (int)RockProjIds.ScorchWheelMove:
					damagerMessage = onScorchDamage(damagable, owner, 1);
					break;
				case (int)RockProjIds.DangerWrap:
					damagerMessage = onDWrapDamage(damagable, owner);
					break;
			}
			if (damagerMessage?.flinch != null) flinch = damagerMessage.flinch.Value;
			if (damagerMessage?.damage != null) damage = damagerMessage.damage.Value;

			proj.onDamageEX(damagable);
		}

		// Character section
		bool spiked = false;
		if (victim is Character character) {
			// Ride armor stomp
			bool isStompWeapon = false;
			if (isStompWeapon) {
				character.flattenedTime = 30;
			}
			// Status effects.
			switch (projId) {
				case (int)BluesProjIds.SparkShock: {
					character.root(60, 100, owner.id);
					break;
				}
				case (int)BassProjIds.IceWall: {
					if (damagingActor is IceWallProj iceWall) {
						// Ice wall. As the freeze is the same as flinch
						// it just acts as a fancy flinch effect.
						// Does not really affect balance.
						if (iceWall.startedMoving) {
							if (!character.charState.superArmor) {
								character.freeze(Global.halfFlinch, 140, owner.id);
							}
						}
						else if (iceWall.isFalling){
							character.freeze(Global.defFlinch, 140, owner.id);
							damage += 2;
							iceWall.destroySelf();
						} else {
							damage = 0;
						}
					}
					break;
				}
				/*case (int)BassProjIds.IceWallLemon: {
					character.addIgFreezeProgress(0.5f);
					break;
				}*/
				case (int)ProjIds.TenguBladeDash: {
					character.xPushVel += 3 * damagingActor?.xDir ?? 3 * -character.xDir;
					break;
				} 
				case (int)BassProjIds.MagicCardFlip:
					character.xDir *= -1;
					break;
				case (int)BassProjIds.SpreadDrillMid:
					character.vel = character.vel.times(0.5f);
					character.slowdownTime = MathF.Max(character.slowdownTime, 20);
					break;
				case (int)BassProjIds.WaveBurnerUnderwater:
					if (damagingActor != null) {
						Point push = Point.createFromByteAngle(damagingActor.byteAngle).times(5);
						character.xPushVel += push.x;
						character.yPushVel += push.y;
					}
					break;
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
				string flinchKey = getFlinchKey(projId, owner.id);
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
			if (damage > 0 || projId == (int)BassProjIds.BassLemon) {
				//Hurt Direction
				int hurtDir = -character.xDir;
				if (damagingActor != null && hitFromBehind(character, damagingActor, owner, projId)) {
					hurtDir *= -1;
				}
				// Flinch above 0.
				if (flinch > 0 && !weakness) {
					character.playAltSound("hurt", altParams: "carmor");
					character.setHurt(hurtDir, flinch, spiked);
				}
				else if (victim is not Blues) {
					victim?.playSound("hit");
				}
			}
			if (owner.ownedByLocalPlayer && damage > 0) {
				owner.lastDamagedCharacter = character;
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
				victim.playSound("hit");
			}
		}

		if (damage > 0 || projId == (int)BassProjIds.BassLemon) {
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
			(int)RockProjIds.DangerWrapBubbleExplosion => true,
			(int)RockProjIds.DangerWrapExplosion => true,
			(int)RockProjIds.DangerWrapMine => true,
			(int)RockProjIds.DangerWrapMineLanded => true,
			(int)RockProjIds.LegBreaker => true,
			(int)BluesProjIds.ProtoStrike => true,
			(int)BluesProjIds.ProtoStrikePush => true,
			(int)BluesProjIds.BigBangStrike => true,
			(int)BluesProjIds.BigBangStrikeExplosion => true,
			(int)ProjIds.TenguBladeDash => true,
			(int)BassProjIds.TenguBladeDash => true,
			(int)BassProjIds.LightningBolt => true,
			(int)BassProjIds.RemoteMineExplosion => true,
			(int)BassProjIds.SpreadDrill => true,
			(int)BassProjIds.SpreadDrillMid => true,
			(int)BassProjIds.SpreadDrillSmall => true,
			(int)ProjIds.BassKick => true,
			(int)ProjIds.SonicCrusher => true,
			(int)BassProjIds.SweepingLaser => true,
			_ => false,
		};
	}

	public static bool isDot(int? projId) {
		if (projId == null) {
			return false;
		}
		return (ProjIds)projId switch {
			ProjIds.Burn => true,
			ProjIds.AcidBurstPoison => true,
			ProjIds.FlameRoundFlameProj => true,
			ProjIds.SelfDmg => true,
			_ => false
		};
	}

	public static string getFlinchKey(int projId, int playerId) {
		return $"{projId}_{playerId}";
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
		if (isDot(projId)) {
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
			if (proj.canBeLocal && damager.vel.x != 0) {
				if (checkDelta(actor, damager.vel.x)) {
					return true;
				} else {
					return false;
				}
			}
			if (proj.isMelee || proj.isOwnerLinked) {
				if (proj.ownerActor != null) {
					damagePos = proj.ownerActor.pos;
				} else if (projOwner?.character != null) {
					damagePos = projOwner.character.pos;
				}
			}
		}

		// Call function if pos is not null.
		return checkPos(actor, damagePos);
	}

	public static bool alwaysDirBlock(Actor actor, int projId) {
		if (projId == (int)BassProjIds.RemoteMineExplosion && actor is RemoteMineExplosionProj remoteMine) {
			return remoteMine.time >= 6f / 60f;
		}
		if (projId == (int)RockProjIds.ScorchWheel && actor is ScorchWheelProj) {
			return true;
		}
		return projId switch {
			(int)RockProjIds.ScorchWheelBurn => true,
			(int)ProjIds.AcidBurstPoison => true,
			(int)ProjIds.Burn => true,
			_ => false
		};
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
			//ProjIds.AirBlastProj => true,
			_ => false
		};
	}

	public static bool unassistable(int? projId) {
		if (projId == null) {
			return false;
		}
		// Never assist in any mode as they are DOT or self-damage. (Also Volt Tornado)
		bool alwaysNotAssist = (ProjIds)projId switch {
			// DOT stuff.
			ProjIds.Burn => true,
			ProjIds.AcidBurstPoison => true,
			ProjIds.FlameRoundFlameProj => true,
			ProjIds.SelfDmg => true,
			_ => false
		};
		if (alwaysNotAssist) {
			return true;
		}
		return false;
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
		if (CrackedWall.canDamageCrackedWall(1, null) != 0) {
			return true;
		}
		return true;
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

public enum EffectId {
	Flinch,
	ElecStun,
	IceStun,
	FireStun,
	IceSlow,
	ElecRoot,
	IceRoot,
	FireDot,
	AcidDot,
	RockBubble
}

[ProtoContract]
public class DamagerProtoBuff {
	[ProtoMember(1)] public uint? targetId;
	[ProtoMember(2)] public uint? damager;
	[ProtoMember(3)] public uint? damagerOwnerId;
	[ProtoMember(4)] public byte damagerPlayerId;
	[ProtoMember(5)] public byte? meleeId;

	[ProtoMember(6)] public float damage;
	[ProtoMember(7)] public float hitCooldown;
	[ProtoMember(8)] public int flinch;
	[ProtoMember(9)] public float? flinchCooldown;

	[ProtoMember(10)] public Dictionary<EffectId, float> effects = [];
	[ProtoMember(11)] public Dictionary<EffectId, float> effectCooldown = [];
	[ProtoMember(12)] public HashSet<ChipId> chips = [];
}



