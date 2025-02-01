using System;
using System.Collections.Generic;

namespace MMXOnline;

public class SARocketPunch : Weapon {
	public static SARocketPunch netWeapon = new();

	public List<RockBusterProj> lemonsOnField = new List<RockBusterProj>();

	public SARocketPunch() : base() {
		index = (int)RockWeaponIds.SARocketPunch;
		killFeedIndex = 0;
		weaponSlotIndex = (int)RockWeaponSlotIds.SARocketPunch;
		hasCustomAnim = true;
	}

		public override bool canShoot(int chargeLevel, Player player) {
		if (!base.canShoot(chargeLevel, player)) return false;
		if (chargeLevel > 1) {
			return true;
		}
		for (int i = lemonsOnField.Count - 1; i >= 0; i--) {
			if (lemonsOnField[i].destroyed) {
				lemonsOnField.RemoveAt(i);
				continue;
			}
		}
		return lemonsOnField.Count < 3;
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 0;
	}
	
	public override void shootRock(Rock rock, params int[] args) {
		base.shootRock(rock, args);
		int xDir = rock.getShootXDir();
		Player player = rock.player;
		int chargeLevel = args[0];

		if (chargeLevel <= 1) rock.setShootAnim();
		Point shootPos = rock.getShootPos();

		if (chargeLevel >= 2) {
			if (rock.grounded) rock.changeState(new SARocketPunchState(), true);
			else new SARocketPunchProj(rock, shootPos, xDir, player.getNextActorNetId(), true, player);
			rock.playSound("super_adaptor_punch", sendRpc: true);

		} else if (chargeLevel == 1) {
			new RockBusterMidChargeProj(rock, shootPos, xDir, player.getNextActorNetId(), 0, true);
			rock.playSound("buster2", sendRpc: true);

		} else {
			var proj = new RockBusterProj(rock, shootPos, xDir, player.getNextActorNetId(), true);
			lemonsOnField.Add(proj);
			rock.lemons++;
			rock.playSound("buster", sendRpc: true);

			rock.timeSinceLastShoot = 0;
			rock.lemonTime += 20f * rock.lemons;
			if (rock.lemonTime >= 60f) {
				rock.lemonTime = 0;
				rock.weaponCooldown = 30;
			}
		}
	}
}


public class SARocketPunchProj : Projectile {

	public bool reversed;
	public bool returned;
	Character shooter = null!;
	Player player;
	Rock rock = null!;
	public float maxReverseTime;
	public float minTime;
	public Actor? target;
	public SARocketPunch? saRocketPunch;
	float projSpeed = 240;

	public SARocketPunchProj(
		Actor owner, Point pos, int xDir, ushort? netProjId, 
		bool rpc = false, Player? altPlayer = null
	) : base(
			pos, xDir, owner, "sa_rocket_punch", netProjId, altPlayer
	) {

		projId = (int)RockProjIds.SARocketPunch;
		minTime = 0.2f;
		rock = owner as Rock ?? throw new NullReferenceException();
		rock.saRocketPunchProj = this;
		maxReverseTime = 0.5f;
		this.player = ownerPlayer;
		shooter = owner as Character ?? throw new NullReferenceException();
		destroyOnHit = false;
		canBeLocal = false;

		vel.x = projSpeed * xDir;
		damager.damage = 3;
		damager.flinch = Global.halfFlinch;
		damager.hitCooldown = 0.5f;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netProjId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new SARocketPunchProj(
			arg.owner, arg.pos, arg.xDir, arg.netId, altPlayer: arg.player
		);
	}


	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;

		if (ownedByLocalPlayer && (shooter == null || shooter.destroyed)) {
			destroySelf("generic_explosion");
			return;
		}

		var targets = Global.level.getTargets(shooter.pos, player.alliance, true);
		foreach (var t in targets) {
			if (shooter.isFacing(t) && MathF.Abs(t.pos.y - shooter.pos.y) < 80) {
				target = t;
				break;
			}
		}

		if (!reversed && target != null) {
			vel = new Point(0, 0);
			if (pos.x > target.pos.x) xDir = -1;
			else xDir = 1;
			Point targetPos = target.getCenterPos();
			move(pos.directionToNorm(targetPos).times(projSpeed));
			if (pos.distanceTo(targetPos) < 5) {
				reversed = true;
			}
		}

		if (!reversed && time > maxReverseTime) reversed = true;

		if (reversed) {
			vel = new Point(0, 0);
			if (pos.x > shooter.pos.x) xDir = -1;
			else xDir = 1;

			Point returnPos = shooter.getCenterPos();
			if (shooter.sprite.name == "rock_rocket_punch") {
				Point poi = shooter.pos;
				var pois = shooter.sprite.getCurrentFrame()?.POIs;
				if (pois != null && pois.Length > 0) {
					poi = pois[0];
				}
				returnPos = shooter.pos.addxy(poi.x * shooter.xDir, poi.y);
			}

			move(pos.directionToNorm(returnPos).times(projSpeed));
			if (pos.distanceTo(returnPos) < 10) {
				returned = true;
				destroySelf();
				Global.playSound("super_adaptor_punch_recover");
			}
		}
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		if (locallyControlled) {
			reversed = true;
		}
		if (isRunByLocalPlayer()) {
			reversed = true;
			//RPC.actorToggle.sendRpc(netId, RPCActorToggleType.ReverseRocketPunch);
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		if (rock != null) rock.saRocketPunchProj = null;
	}
}


public class SARocketPunchState : CharState {

	bool fired;
	Rock rock = null!;

	public SARocketPunchState() : base("rocket_punch", "rocket_punch", "", "") {
		normalCtrl = true;
		attackCtrl = true;
		airMove = true;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		rock = character as Rock ?? throw new NullReferenceException();
	}

	public override void update() {
		base.update();

		if (character.frameIndex == 0 && !fired) {
			fired = true;

			var poi = character.currentFrame.POIs;
			Point? shootPos = character.getFirstPOI();
			if (shootPos != null) new SARocketPunchProj(
				rock, shootPos.Value, rock.getShootXDir(), player.getNextActorNetId(true), true, player
			);
		}

		if (character.isAnimOver()) {
			if (character.grounded) character.changeState(new Idle(), true);
			else character.changeState(new Fall(), true);
		}
	}
}
