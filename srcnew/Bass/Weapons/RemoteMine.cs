using System;
using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;

public class RemoteMine : Weapon {
	public static RemoteMine netWeapon = new();
	public bool shootOnFrame;
	public RemoteMineProj? activeMine;
	public List<Projectile> landedMines = [];

	public RemoteMine() : base() {
		iconSprite = "hud_weapon_icon_bass";
		index = (int)BassWeaponIds.RemoteMine;
		displayName = "REMOTE MINE";
		maxAmmo = 20;
		ammo = maxAmmo;
		weaponSlotIndex = index;
		weaponBarBaseIndex = index;
		weaponBarIndex = index;
		fireRate = 45;
		switchCooldown = 30;
		descriptionV2 = [
			[ "Sticks on enemies and walls.\n" +
			"Press UP or DOWN after shooting to aim.\n" +
			"Press SHOOT button after sticking to detonate."]
		];
	}

	public override void charLinkedUpdate(Character character, bool isAlwaysOn) {
		base.charLinkedUpdate(character, isAlwaysOn);

		if (!shootOnFrame && activeMine?.destroyed == false && character.currentWeapon == this &&
			character.player.input.isPressed(Control.Shoot, character.player)
		) {
			activeMine.explode();
			shootOnFrame = true;
		} else {
			shootOnFrame = false;
		}
		if (landedMines.Count > 0) {
			landedMines = landedMines.Where(mine => !mine.destroyed).ToList();
		}
	}

	public override bool canShoot(int chargeLevel, Character chara) {
		return (
			!shootOnFrame &&
			ammo > 0 &&
			activeMine?.destroyed != false
		);
	}

	public override void shoot(Character character, params int[] args) {
		Point shootPos = character.getShootPos();
		Player player = character.player;
		shootOnFrame = true;

		landedMines = landedMines.Where(mine => !mine.destroyed).ToList();
		activeMine = new RemoteMineProj(
			character, shootPos, character.getShootXDir(), player.getNextActorNetId(), true,
			linkedWeapon: this
		);
	}

	public void addMine(Projectile proj) {
		landedMines.Add(proj);

		landedMines = landedMines.Where(mine => !mine.destroyed).ToList();
		int removalTheshold = landedMines.Count - 1;

		for (int i = landedMines.Count - 1; i >= 0; i--) {
			if (i < removalTheshold) {
				Projectile mine = landedMines[i];
				landedMines.RemoveAt(i);
				mine.destroySelf();
			}
		}
	}
}

public class RemoteMineProj : Projectile {
	public Actor? attachHost;
	public Sprite altAnim = new Sprite("remote_mine_anim");
	public bool exploded;
	public bool landed;
	public RemoteMine? linkedWeapon;

	public RemoteMineProj(
		Actor owner, Point pos, int xDir, ushort? netProjId,
		bool rpc = false, Player? altPlayer = null, RemoteMine? linkedWeapon = null
	) : base(
			pos, xDir, owner, "remote_mine_proj", netProjId, altPlayer
	) {
		projId = (int)BassProjIds.RemoteMine;
		maxTime = 50 / 60f;
		destroyOnHit = false;

		vel.x = 240 * xDir;
		damager.hitCooldown = 30;

		if (ownedByLocalPlayer && owner.ownedByLocalPlayer) {
			this.linkedWeapon = linkedWeapon;
		}
		canBeLocal = false;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netProjId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new RemoteMineProj(
			arg.owner, arg.pos, arg.xDir, arg.netId, altPlayer: arg.player
		);
	}

	public override void update() {
		base.update();
		altAnim.update();
		if (!ownedByLocalPlayer) {
			return;
		}
		if (attachHost != null) {
			changePos(attachHost.getCenterPos());
		}
		/*
		if (time >= maxTime && !destroyed && bass != null && bass.ownedByLocalPlayer) {
			// ruben: cant put this as fade anim
			// or on destroy because it will conflict with the explosion anim
			new Anim(
				getCenterPos(), "remote_mine_fade_anim", xDir,
				bass.player.getNextActorNetId(), true, true
			);
		}
		*/
		int moveY = owner.input.getYDir(owner);
		if (moveY != 0) {
			moveXY(0, 1.5f * moveY);
		}
	}

	public override void render(float x, float y) {
		base.render(x, y);
		if (!visible) {
			return;
		}
		Point center = getCenterPos();
		altAnim.draw(
			altAnim.frameIndex, center.x, center.y, xDir, yDir,
			null, alpha, 1, 1, zIndex
		);
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (!ownedByLocalPlayer) {
			return;
		}
		if (landed || exploded || attachHost != null) {
			return;
		}
		if (other.gameObject is KillZone) {
			return;
		}

		if (!landed && (
			other.gameObject is Wall ||
			other.gameObject is MovingPlatform ||
			other.gameObject is Actor actor && (actor.isPlatform || actor.isSolidWall)
		)) {
			landed = true;
			destroySelf();
			playSound("remotemineStick", true);
			return;
		}

		if (other.gameObject is not Character chr) {
			return;
		}

		bool characterLand = (
			chr != ownerActor &&
			chr.canBeDamaged(ownerPlayer.alliance, ownerPlayer.id, projId)
		);

		if (characterLand) {
			attachHost = chr;
			stopMoving();
			changeSprite("remote_mine_land", true);
			playSound("remotemineStick", true);
			maxTime = 2;
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		if (exploded || ownerActor == null) {
			return;
		}
		if (landed) {
			new RemoteMineLandProj(
				ownerActor, pos, xDir, damager.owner.getNextActorNetId(), true,
				linkedWeapon: linkedWeapon
			);
		}
		else if (time >= maxTime) {
			explode();
		}
	}

	public void explode() {
		if (!destroyed) {
			destroySelf();
		}
		if (exploded) {
			return;
		}
		// Explode if local player.
		if (ownedByLocalPlayer && ownerActor != null) {
			var rme = new RemoteMineExplosionProj(ownerActor, pos, xDir, damager.owner.getNextActorNetId(), true);
			playSound("remotemineExplode", true);
			// Apply direct damage if attached.
			if (attachHost is IDamagable damagable) {
				rme.damager.applyDamage(
					damagable, false, linkedWeapon ?? weapon,
					rme, (int)BassProjIds.RemoteMineMeleeExplosion
				);
			}
		}
		exploded = true;
	}
}

public class RemoteMineLandProj : Projectile {
	public Bass? bass;
	public Sprite altAnim = new Sprite("remote_mine_anim");
	public bool exploded;

	public RemoteMineLandProj(
		Actor owner, Point pos, int xDir, ushort? netId,
		bool rpc = false, Player? altPlayer = null, RemoteMine? linkedWeapon = null
	) : base(
		pos, xDir, owner, "remote_mine_land", netId, altPlayer
	) {
		projId = (int)BassProjIds.RemoteMineLand;
		maxTime = 4;
		destroyOnHit = true;

		if (ownedByLocalPlayer && owner.ownedByLocalPlayer) {
			linkedWeapon?.addMine(this);
		}
		canBeLocal = false;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new RemoteMineLandProj(
			arg.owner, arg.pos, arg.xDir, arg.netId, altPlayer: arg.player
		);
	}

	public override void update() {
		base.update();
		altAnim.update();
		if (!ownedByLocalPlayer) {
			return;
		}

		if (exploded) {
			return;
		}
		// Explode if not on a plaform anymore.
		moveWithMovingPlatform();
		if (!isColliding()) {
			explode();
			return;
		}
		// Explode if enemies are nearby even if not touched.
		Actor[] closeActors = getCloseActors(38).Where(obj => obj is Character).ToArray();
		if (closeActors.Length > 0) {
			explode();
		}
	}

	public override void render(float x, float y) {
		base.render(x, y);

		Point center = getCenterPos();
		altAnim.draw(
			altAnim.frameIndex, center.x, center.y, xDir, yDir,
			null, alpha, 1, 1, zIndex
		);
	}

	public override void onDestroy() {
		base.onDestroy();
		if (!ownedByLocalPlayer) {
			return;
		}
		if (!exploded) {
			explode();
		}
	}

	public void explode() {
		if (exploded) {
			return;
		}
		if (!destroyed) {
			destroySelf();
		}
		if (ownedByLocalPlayer && ownerActor != null) {
			new RemoteMineExplosionProj(ownerActor, pos, xDir, damager.owner.getNextActorNetId(), true);
			playSound("remotemineExplode", true);
		}
		exploded = true;
	}

	public bool isColliding() {
		List<CollideData> collideDatas = Global.level.getTerrainTriggerList(this, new Point(0, 1));
		foreach (CollideData collideData in collideDatas) {
			if (collideData.gameObject is Wall or MovingPlatform) {
				return true;
			}
			if (collideData.gameObject is Actor actor &&
				(actor.isSolidWall || actor.isPlatform)
			) {
				return true;

			}
		}
		return false;
	}
}

public class RemoteMineExplosionProj : Projectile {
	int expTime;
	Anim? part;
	int animLap;

	public RemoteMineExplosionProj(
		Actor owner, Point pos, int xDir, ushort? netProjId,
		bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "remote_mine_explosion", netProjId, altPlayer
	) {
		projId = (int)BassProjIds.RemoteMineExplosion;
		maxTime = 0.75f;
		destroyOnHit = false;
		shouldShieldBlock = false;

		damager.damage = 2;
		damager.flinch = Global.halfFlinch;
		damager.hitCooldown = 60;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netProjId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new RemoteMineExplosionProj(
			arg.owner, arg.pos, arg.xDir, arg.netId, altPlayer: arg.player
		);
	}

	public override void update() {
		base.update();
		expTime++;

		if (part == null) {
			animLap++;
			if (animLap > 4) animLap = 1;
			for (int i = 0; i < 5; i++) {
				if (!ownedByLocalPlayer) return;
				part = new Anim(pos, "remote_mine_explosion_part", xDir,
					damager.owner.getNextActorNetId(), true, true);

				switch (animLap) {
					case 1:
						part.vel = Point.createFromByteAngle(23 + (i * 21)) * 180;
						break;

					case 2:
						part.vel.y = 90 * (i - 2);
						break;

					case 3:
						part.vel = Point.createFromByteAngle(64 + (i * 16)) * 180;
						break;

					default:
						part.vel = Point.createFromByteAngle(0 + (i * 16)) * 180;
						break;
				}
			}
		} else {
			if (expTime % 2 != 0) part.visible = true;
			else part.visible = false;

			if (part.destroyed) part = null;
		}
	}
}
