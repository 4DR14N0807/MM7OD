using System;
using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;

public class MagicCard : Weapon {

	public List<MagicCardProj> cardsOnField = new List<MagicCardProj>();

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

		int effect = 0;
		bass.cardsCount++;

		if (bass.cardsCount >= 7) {
			bass.cardsCount = 0;
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
			new MagicCardSpecialSpawn(shootPos, character.getShootXDir(), 
				player, shootAngle, player.getNextActorNetId(), true);
		} else {
			var card = new MagicCardProj(
				this, shootPos, character.getShootXDir(), shootAngle, 
				player, player.getNextActorNetId(), effect, true
			);
			cardsOnField.Add(card);
			character.playSound("magiccard", true);
		}
	}
}


public class MagicCardProj : Projectile {
	bool reversed;
	Character shooter = null!;
	float maxReverseTime;
	const float projSpeed = 480;
	public Pickup? pickup;
	Weapon wep;
	int effect;
	int hits;
	float startAngle;

	public MagicCardProj(
		Weapon weapon, Point pos, int xDir, float byteAngle, Player player, 
		ushort? netProjId, int effect = 0, bool rpc = false
	) : base (
		MagicCard.netWeapon, pos, xDir, 0, 1,
		player, "magic_card_proj", 0, 0.3f, 
		netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)BassProjIds.MagicCard;
		maxTime = 3f;
		maxReverseTime = 0.45f;

		this.byteAngle = byteAngle;
		startAngle = byteAngle;
		this.effect = effect;
		wep = weapon;
		shooter = player.character ?? throw new NullReferenceException();
		destroyOnHit = effect != 3;

		vel = Point.createFromByteAngle(byteAngle) * 425;	
		
		canBeLocal = false;
		if (rpc) {
			rpcCreateByteAngle(pos, player, netId, byteAngle, (byte)(xDir + 1));
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new MagicCardProj(
			MagicCard.netWeapon, arg.pos, arg.extraData[0] - 1, 
			arg.byteAngle, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();

		if (ownedByLocalPlayer && (shooter == null || shooter.destroyed)) {
			destroySelf();
			return;
		}

		if (!ownedByLocalPlayer) return;

		if (hits >= 3 || time > maxReverseTime) reversed = true;

		if (reversed) {
			vel = new Point(0, 0);
			frameSpeed = -2;
			if (frameIndex == 0) frameIndex = sprite.totalFrameNum - 1;

			Point returnPos = shooter.getCenterPos();
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

			if (pos.distanceTo(returnPos) < 16) {
				foreach (Weapon w in shooter.player.weapons) {
					if (w == wep) {
						w.addAmmo(getAmmo(), shooter.player);
						continue;
					} 
				}
				destroySelf();
			}
		}

		if (!destroyed && pickup != null) {
			pickup.collider.isTrigger = true;
			pickup.useGravity = false;
			pickup.changePos(pos);
		}
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (!ownedByLocalPlayer) return;
		if (destroyed) return;

		if (other.gameObject is Pickup && pickup == null) {
			pickup = other.gameObject as Pickup;
			if (pickup != null) {
				playSound("magiccardCatch", true);
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

				new MagicCardSpecialProj(pos, xDir, damager.owner, damager.owner.getNextActorNetId(), startAngle, 1, true);
				new MagicCardSpecialProj(pos, xDir, damager.owner, damager.owner.getNextActorNetId(), startAngle, -1, true);
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

					if (effect == 1) chr.xDir *= -1;
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

	public MagicCardSpecialSpawn(
		Point pos, int xDir, Player player, float startAngle,
		ushort? netProjId, bool rpc = false
	) : base(
		MagicCard.netWeapon, pos, xDir, 0, 0,
		player, "generic_explosion", 0, 0, netProjId,
		player.ownedByLocalPlayer
	) {
		projId = (int)BassProjIds.MagicCardSSpawn;
		setIndestructableProperties();
		maxTime = 2;
		this.startAngle = startAngle;
		
		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)startAngle };

			rpcCreate(pos, player, netProjId, xDir, extraArgs);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new MagicCardSpecialSpawn(
			arg.pos, arg.xDir, arg.player, arg.extraData[0], arg.netId
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

			new MagicCardSpecialProj(pos, xDir, damager.owner, 
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
		Point pos, int xDir, Player player, ushort? netProjId, 
		float startAngle, int type, bool rpc = false
	) : base(
		MagicCard.netWeapon, pos, xDir, 425, 1,
		player, "magic_card_proj", 0, 0, 
		netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)BassProjIds.MagicCardS;
		maxTime = 3;
		this.type = type;
		base.byteAngle = (type * 10) + startAngle;
		if (xDir < 0 && startAngle != 128) byteAngle = -byteAngle + 128;
		vel = Point.createFromByteAngle(byteAngle.Value).times(speed);

		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)startAngle, (byte)type };

			rpcCreate(pos, player, netProjId, xDir, extraArgs);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new MagicCardSpecialProj(
			arg.pos, arg.xDir, arg.player, arg.netId,
			arg.extraData[0], arg.extraData[1] 
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
