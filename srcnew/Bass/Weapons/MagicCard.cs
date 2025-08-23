using System;
using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;

public class MagicCard : Weapon {
	public static MagicCard netWeapon = new();
	public List<MagicCardProj> cardsOnField = new();
	public int cardCount = 7;
	string[] effectsText = new[] {
		"",
		"FLIP!",
		"AMMO REFILL!",
		"DOUBLE SHOT!",
		"MULTU-SHOT!!!",
	};
	string[] effectsSounds = new[] {
		"",
		"upgrade",
		"upgrade",
		"upgrade",
		"magiccard4"
	};

	public MagicCard() : base() {
		iconSprite = "hud_weapon_icon_bass";
		index = (int)BassWeaponIds.MagicCard;
		displayName = "MAGIC CARD";
		weaponSlotIndex = index;
		weaponBarBaseIndex = index;
		weaponBarIndex = index;
		fireRate = 20;
		ammoDisplayScale = 2;
		isStream = true;
		drawCooldown = false;
		descriptionV2 = [
			[ 
				"Can take health and ammo capsules.\n" + 
				"Shoots a projectile with a random effect\n" +
				"each 7 shots." 
			], 
			[
				"(1): Changes enemy direction on hit.\n" +
				"(2): Ammo refill\n" +
				"(The more hits, the more ammo you will get)." 
			],
			[
				"(3): Homing Double shot.\n" +
				"(4): Homing Multi-shot.\n" 
			],
		];
	}

	public override void shoot(Character character, params int[] args) {
		if (character is not Bass bass) {
			return;
		}

		Point shootPos = character.getShootPos();
		float shootAngle = bass.getShootAngle(true, false);
		Player player = character.player;

		for (int i = cardsOnField.Count - 1; i >= 0; i--) {
			if (cardsOnField[i].destroyed) {
				cardsOnField.RemoveAt(i);
			}
		}

		if (shootAngle is 0 or 128) {
			int offset = 8;
			int offset2 = Math.Min(cardsOnField.Count * offset, 2 * offset);
			shootPos = shootPos.addxy(0, offset - offset2);
		}

		int effect = 0;
		cardCount--;

		if (cardCount <= 0) {
			cardCount += 7;
			int[] effectChances = [
				1, 1, 1, 1,
				2, 2, 2, 2,
				3, 3, 3,
				4, 4
			];
			int effectSel = Helpers.randomRange(0, effectChances.Length - 1);
			effect = effectChances[effectSel];
			// 0: No effect.
			// 1: xDir flip.
			// 2: Ammo refill.
			// 3: Duplicate on collision.
			// 4: Multiple Cards.
			if (effect >= 2) {
				addAmmo(-effect + 1, player);
			}
			bass.showNumberTime = 60;
			bass.lastCardNumber = effect;
			bass.playSound(effectsSounds[effect], true);
			Global.level.gameMode.setHUDErrorMessage(player, effectsText[effect], false, overrideFont: FontType.WhiteSmall);
		}
		if (effect >= (int)MagicCardEffects.MultiShot) {
			new MagicCardSpecialSpawn(bass, shootPos, bass.getShootXDir(), 
				shootAngle, player.getNextActorNetId(), true);
		} else {
			var card = new MagicCardProj(
				bass, this, shootPos, character.getShootXDir(), shootAngle, 
				player.getNextActorNetId(), effect, true
			);
			cardsOnField.Add(card);
			character.playSound("magiccard", true);
		}
	}
}

public enum MagicCardEffects {
	None,
	Flip,
	Refill,
	Duplicate,
	MultiShot
}

public class MagicCardProj : Projectile {
	bool reversed;
	Character? shooter;
	float maxReverseTime;
	const float projSpeed = 480;
	public Pickup? pickup;
	Weapon wep;
	public int effect;
	int hits;
	float startAngle;
	Actor ownChr = null!;
	private Point returnPos;
	bool duplicated;
	int originalDir;

	public MagicCardProj(
		Actor owner, Weapon weapon, Point pos, int xDir, float byteAngle,
		ushort? netProjId, int effect = 0, bool rpc = false, Player? altPlayer = null
	) : base (
		pos, xDir, owner, "magic_card_proj", netProjId, altPlayer
	) {
		projId = (int)BassProjIds.MagicCard;
		maxTime = 5f;
		maxReverseTime = 0.45f;

		this.byteAngle = byteAngle;
		startAngle = byteAngle;
		this.effect = effect;
		wep = weapon;
		returnPos = pos;
		if (ownedByLocalPlayer) {
			shooter = ownerPlayer.character;
			if (shooter != null) {
				returnPos = shooter.getCenterPos();
			}
		}
		destroyOnHit = effect != (int)MagicCardEffects.Refill;
		if (effect >= 1) changeSprite(sprite.name + effect.ToString(), true);

		vel = Point.createFromByteAngle(byteAngle) * 425;	
		damager.damage = 1;
		damager.hitCooldown = 18;
		ownChr = owner;
		originalDir = xDir;

		canBeLocal = false;
		if (rpc) {
			rpcCreateByteAngle(pos, ownerPlayer, netId, byteAngle, (byte)(xDir + 1));
		}

		if (effect == (int)MagicCardEffects.Flip) projId = (int)BassProjIds.MagicCard1;
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new MagicCardProj(
			arg.owner, MagicCard.netWeapon, arg.pos, arg.extraData[0] - 1, 
			arg.byteAngle, arg.netId, altPlayer: arg.player
		);
	}

	public override void update() {
		base.update();

		if (ownedByLocalPlayer && (shooter == null || shooter.destroyed)) {
			destroySelf();
			return;
		}

		if (!ownedByLocalPlayer || shooter == null) return;

		if (hits >= 3 || time > maxReverseTime) reversed = true;

		if (reversed) {
			vel = new Point(0, 0);
			frameSpeed = -2;
			if (frameIndex == 0) frameIndex = sprite.totalFrameNum - 1;
			returnPos = shooter.getCenterPos();

			if (shooter.sprite.name.Contains("shoot")) {
				Point poi = shooter.pos;
				var pois = shooter.sprite.getCurrentFrame()?.POIs;
				if (pois != null && pois.Length > 0) {
					poi = pois[0];
				}
				returnPos = shooter.pos.addxy(poi.x * shooter.xDir, poi.y);
			}
			Point speed = pos.directionToNorm(returnPos).times(425);
			move(speed);
			byteAngle = speed.byteAngle;

			if (pos.distanceTo(returnPos) < 24) {
				foreach (Weapon w in shooter.weapons) {
					if (w == wep) {
						w.addAmmo(getAmmo(), shooter.player);
						continue;
					} 
				}
				destroySelf();
			}
		}
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (!ownedByLocalPlayer) return;
		if (destroyed) return;
		if (shooter == null) return;

		if (other.gameObject is Pickup && pickup == null) {
			pickup = other.gameObject as Pickup;
			if (pickup != null) {
				playSound("magiccardCatch", true);
				pickup.changePos(shooter.getCenterPos());
				if (!pickup.ownedByLocalPlayer) {
					pickup.takeOwnership();
					RPC.clearOwnership.sendRpc(pickup.netId);
				}
			}
		}

		var character = other.gameObject as Character;
		if (reversed && character != null && character.player == damager.owner) {
			if (pickup != null) {
				pickup.changePos(character.getCenterPos());
			}
			destroySelf();
		}
		if (effect == (int)MagicCardEffects.Duplicate && !duplicated) {
			var proj = other.gameObject as Projectile;
			if (proj != null && proj.owner.alliance != damager.owner.alliance) {
				destroySelf();
			}
			new MagicCardSpecialProj(
				ownChr, pos, originalDir, damager.owner.getNextActorNetId(), startAngle, effect, true
			);
			playSound("magiccard", true);
			duplicated = true;
		}
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);

		if (damagable.canBeDamaged(damager.owner.alliance, damager.owner.id, projId)) {
			if (damagable.projectileCooldown.ContainsKey(projId + "_" + owner.id) &&
				damagable.projectileCooldown[projId + "_" + owner.id] >= damager.hitCooldown
			) {
				if (damagable is not Character chr) return;
				else {
					hits++;
					if (effect == (int)MagicCardEffects.Refill) {
						playSound("magiccard2", true);
					}
					/* if (hits >= 4) {
						updateDamager(0);
					} */
				}
			}
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		if (pickup != null) {
			pickup.useGravity = true;
			if (pickup.collider != null) {
				pickup.collider.isTrigger = false;
			}
		}
		if (!ownedByLocalPlayer) { return; }
		if (effect == (int)MagicCardEffects.Duplicate && !duplicated) {
			new MagicCardSpecialProj(
				ownChr, pos, originalDir, damager.owner.getNextActorNetId(), startAngle, effect, true
			);
			playSound("magiccard", true);
		}
	}

	float getAmmo() {
		// Refund the ammo use and 1 for each extra hit.
		if (effect == (int)MagicCardEffects.Refill) {
			playSound("magiccard2", true);
			return 2 + hits;
		}
		// Refund only the ammo use.
		if (effect >= 2) {
			return effect;
		}
		return 1;
	}
}

public class MagicCardSpecialSpawn : Projectile {
	float cooldown;
	int count = 1;
	float startAngle;
	Actor ownChr = null!;

	public MagicCardSpecialSpawn(
		Actor owner, Point pos, int xDir, float startAngle,
		ushort? netProjId, bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "generic_explosion", netProjId, altPlayer
	) {
		projId = (int)BassProjIds.MagicCardSSpawn;
		setIndestructableProperties();
		maxTime = 2;
		this.startAngle = startAngle;
		ownChr = owner;

		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)startAngle };

			rpcCreate(pos, owner, ownerPlayer, netProjId, xDir, extraArgs);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new MagicCardSpecialSpawn(
			arg.owner, arg.pos, arg.xDir, arg.extraData[0], arg.netId, altPlayer: arg.player
		);
	}

	public override void update() {
		base.update();

		if (!ownedByLocalPlayer) return;

		Helpers.decrementFrames(ref cooldown);

		if (cooldown <= 0) {
			float t = count;
			if (t % 2 == 0) {
				t /= 2;
				t *= -1;
			} else {
				t = MathF.Ceiling(t / 2);
			}

			new MagicCardSpecialProj(ownChr, pos, xDir,  
				damager.owner.getNextActorNetId(), startAngle, (int)MagicCardEffects.MultiShot, true);
			playSound("magiccard", true);
			count++;
			cooldown = 9;

			if (count >= 5) destroySelf();
		} 
	}
}

public class MagicCardSpecialProj : Projectile {
	int type;
	Actor? closestEnemy;

	public MagicCardSpecialProj(
		Actor owner, Point pos, int xDir, ushort? netProjId, 
		float startAngle, int type, bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "magic_card_proj", netProjId, altPlayer
	) {
		projId = (int)BassProjIds.MagicCardS;
		maxTime = 3;

		this.type = type;
		changeSprite(sprite.name + type.ToString(), true);

		base.byteAngle = (type * 10 ) + startAngle;
		if (xDir < 0 && startAngle != 128) byteAngle = -byteAngle + 128;
		vel = Point.createFromByteAngle(byteAngle).times(speed);

		damager.damage = 1;

		canBeLocal = false;

		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)startAngle, (byte)type };

			rpcCreate(pos, owner, ownerPlayer, netProjId, xDir, extraArgs);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new MagicCardSpecialProj(
			arg.owner, arg.pos, arg.xDir, arg.netId,
			arg.extraData[0], arg.extraData[1], altPlayer: arg.player
		);
	}

	public override void update() {
		base.update();

		if (!ownedByLocalPlayer) return;

		if (time >= 0.33f) followTarget();
		else byteAngle = vel.byteAngle;
	}

	public void followTarget() {
		if (closestEnemy == null) {
			closestEnemy = Global.level.getClosestTarget(
			new Point (pos.x, pos.y),
			damager.owner.alliance,
			false, 200
			);
		} else {
			Point enemyPos = closestEnemy.getCenterPos();
			float moveSpeed = 425;
			stopMovingWeak();
			Point speed = pos.directionToNorm(enemyPos).times(moveSpeed);
			byteAngle = speed.byteAngle;

			// X axis follow.
			if (pos.x < enemyPos.x) {
				move(new Point(moveSpeed, 0));
				if (pos.x > enemyPos.x) { pos.x = enemyPos.x; }
			} else if (pos.x > enemyPos.x) {
				move(new Point(-moveSpeed, 0));
				if (pos.x < enemyPos.x) { pos.x = enemyPos.x; }
			}
			// Y axis follow.
			if (pos.y < enemyPos.y) {
				move(new Point(0, moveSpeed));
				if (pos.y > enemyPos.y) { pos.y = enemyPos.y; }
			} else if (pos.y > enemyPos.y) {
				move(new Point(0, -moveSpeed));
				if (pos.y < enemyPos.y) { pos.y = enemyPos.y; }
			}
		}
	}
}
