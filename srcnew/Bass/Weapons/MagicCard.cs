using System;
using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;

public class MagicCard : Weapon {
	public static MagicCard netWeapon = new();
	public static  List<MagicCardProj> cardsOnField = new();
	public int cards;

	public MagicCard() : base() {
		index = (int)BassWeaponIds.MagicCard;
		displayName = "MAGIC CARD";
		weaponSlotIndex = index;
		weaponBarBaseIndex = index;
		weaponBarIndex = index;
		fireRate = 20;
	}

	public override void shoot(Character character, params int[] args) {
		if (character is not Bass bass) {
			return;
		}

		Point shootPos = character.getShootPos();
		float shootAngle = bass.getShootAngle(true, false);
		Player player = character.player;
		
		if (shootAngle is 0 or 128) {
			int offset = 12;
			int offset2 = Math.Min(cardsOnField.Count * offset, 2 * offset);

			shootPos = shootPos.addxy(0, offset - offset2);
		}

		int effect = 0;
		cards++;

		if (cards >= 7) {
			cards = 0;
			addAmmo(-3, player);
			bass.playSound("upgrade");
			effect = Helpers.randomRange(1,4);
			// 0: No effect.
			// 1: xDir flip.
			// 2: Duplicate on collision.
			// 3: Ammo refill.
			// 4: Multiple Cards.

			bass.showNumberTime = 60;
			bass.lastCardNumber = effect;
		
		}
		if (effect >= 4) {
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

	public MagicCardProj(
		Actor owner, Weapon weapon, Point pos, int xDir, float byteAngle,
		ushort? netProjId, int effect = 0, bool rpc = false, Player? altPlayer = null
	) : base (
		pos, xDir, owner, "magic_card_proj", netProjId, altPlayer
	) {
		projId = (int)BassProjIds.MagicCard;
		maxTime = 3f;
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
		destroyOnHit = effect != 3;

		vel = Point.createFromByteAngle(byteAngle) * 425;	
		damager.damage = 1;
		damager.hitCooldown = 18;
		ownChr = owner;
		
		canBeLocal = false;
		if (rpc) {
			rpcCreateByteAngle(pos, ownerPlayer, netId, byteAngle, (byte)(xDir + 1));
		}

		if (effect == 1) projId = (int)BassProjIds.MagicCard1;
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

		/* if (!destroyed && pickup != null) {
			pickup.collider.isTrigger = true;
			pickup.useGravity = false;
			pickup.changePos(pos);
		} */
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

		if (effect == 2) {
			var proj = other.gameObject as Projectile;
			
			if (proj != null && proj.owner.alliance != damager.owner.alliance) {
				destroySelf();

				new MagicCardSpecialProj(ownChr, pos, xDir, damager.owner.getNextActorNetId(), startAngle, 1, true);
				new MagicCardSpecialProj(ownChr, pos, xDir, damager.owner.getNextActorNetId(), startAngle, -1, true);
				playSound("magiccard", true);
			}
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
					if (hits >= 4) destroySelf();
				}
			}
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		if (pickup != null) {
			pickup.useGravity = true;
			pickup.collider.isTrigger = false;
		}

		MagicCard.cardsOnField.Remove(this);
	}

	float getAmmo() {
		if (effect != 3 || hits == 0) return 1;
		return 2 * hits;
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
				damager.owner.getNextActorNetId(), startAngle, (int)t, true);
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
		base.byteAngle = (type * 10 ) + startAngle;
		if (xDir < 0 && startAngle != 128) byteAngle = -byteAngle + 128;
		vel = Point.createFromByteAngle(byteAngle.Value).times(speed);
		damager.damage = 1;

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
