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
	
	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		int xDir = character.getShootXDir();
		Player player = character.player;
		int chargeLevel = args[0];

		if (player.character is Rock rock) {

			if (chargeLevel <= 1) rock.setShootAnim();
			Point shootPos = character.getShootPos();

			if (chargeLevel >= 2) {
				if (character.grounded) character.changeState(new SARocketPunchState(), true);
				else new SARocketPunchProj(shootPos, xDir, player, player.getNextActorNetId(), true);
				character.playSound("super_adaptor_punch", sendRpc: true);

			} else if (chargeLevel == 1) {
				new RockBusterMidChargeProj(shootPos, xDir, player, 0, player.getNextActorNetId(), true);
				character.playSound("buster2", sendRpc: true);

			} else {
				var proj = new RockBusterProj(shootPos, xDir, player, player.getNextActorNetId(), true);
				lemonsOnField.Add(proj);
				rock.lemons++;
				character.playSound("buster", sendRpc: true);

				rock.timeSinceLastShoot = 0;
				rock.lemonTime += 20f * rock.lemons;
				if (rock.lemonTime >= 60f) {
					rock.lemonTime = 0;
					rock.weaponCooldown = 30;
				}
			}
		}
	}
}


public class SARocketPunchProj : Projectile {

	public bool reversed;
	public bool returned;
	Character shooter = null!;
	Player player;
	Rock? rock;
	public float maxReverseTime;
	public float minTime;
	public Actor? target;
	public SARocketPunch? saRocketPunch;

	public SARocketPunchProj(
		Point pos, int xDir, Player player, 
		ushort netProjId, bool rpc = false
	) : base(
			SARocketPunch.netWeapon, pos, xDir, 240, 3,
			player, "sa_rocket_punch", Global.halfFlinch, 0.5f,
			netProjId, player.ownedByLocalPlayer
	) {

		projId = (int)RockProjIds.SARocketPunch;
		minTime = 0.2f;
		rock = player.character as Rock;
		if (rock != null) rock.saRocketPunchProj = this;
		maxReverseTime = 0.5f;
		this.player = player;
		shooter = player.character ?? throw new NullReferenceException();
		destroyOnHit = false;
		canBeLocal = false;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new SARocketPunchProj(
			arg.pos, arg.xDir, arg.player, arg.netId
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
			move(pos.directionToNorm(targetPos).times(speed));
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

			move(pos.directionToNorm(returnPos).times(speed));
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

	public SARocketPunchState() : base("rocket_punch", "rocket_punch", "", "") {
		normalCtrl = true;
		attackCtrl = true;
		airMove = true;
	}

	public override void update() {
		base.update();

		if (character.frameIndex == 0 && !fired) {
			fired = true;

			var poi = character.currentFrame.POIs;
			Point? shootPos = character.getFirstPOI();
			if (shootPos != null) new SARocketPunchProj(shootPos.Value, character.getShootXDir(), player, player.getNextActorNetId(true), true);
		}

		if (character.isAnimOver()) {
			if (character.grounded) character.changeState(new Idle(), true);
			else character.changeState(new Fall(), true);
		}
	}
}
