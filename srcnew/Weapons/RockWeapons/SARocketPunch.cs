using System;
using System.Collections.Generic;

namespace MMXOnline;

public class SARocketPunch : Weapon {
	public List<RockBusterProj> lemonsOnField = new List<RockBusterProj>();
	public static SARocketPunch netWeapon = new SARocketPunch();

	public SARocketPunch() : base() {
		index = (int)RockWeaponIds.SARocketPunch;
		killFeedIndex = 0;
		//shootSounds = new List<string>() {"buster", "buster2", "super_adaptor_punch", "super_adaptor_punch"};
		weaponSlotIndex = (int)RockWeaponSlotIds.SARocketPunch;
	}


	public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId) {
		if (player.character.ownedByLocalPlayer) {
			if (player.character is Rock rock) {
				if (chargeLevel >= 2) {
					if (player.character.grounded) player.character.changeState(new SARocketPunchState(), true);
					else new SARocketPunchProj(this, pos, xDir, player, netProjId, true);
					player.character.playSound("super_adaptor_punch", sendRpc: true);
				} else if (chargeLevel == 1) {
					new RockBusterMidChargeProj(new RockBuster(), pos, xDir, player, 0, netProjId, true);
					player.character.playSound("buster2", sendRpc: true);
				} else {
					var proj = new RockBusterProj(new RockBuster(), pos, xDir, player, netProjId, true);
					lemonsOnField.Add(proj);
					rock.lemons++;
					player.character.playSound("buster", sendRpc: true);

					rock.lemonTime += 20f * rock.lemons / 60f;
					if (rock.lemonTime >= 1f) {
						rock.lemonTime = 0;
						rock.shootTime = 30f / 60f;
					}
				}
			}
		}
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
}


public class SARocketPunchProj : Projectile {

	public bool reversed;
	public bool returned;
	Character shooter;
	Player player;
	Rock? rock;
	public float maxReverseTime;
	public float minTime;
	public Actor? target;
	public SARocketPunch? saRocketPunch;

	public SARocketPunchProj(
		Weapon weapon, Point pos, int xDir,
		Player player, ushort netProjId, bool rpc = false
	) : base(
			weapon, pos, xDir, 240, 3,
			player, "sa_rocket_punch", Global.halfFlinch, 0.5f,
			netProjId, player.ownedByLocalPlayer
	) {

		projId = (int)RockProjIds.SARocketPunch;
		minTime = 0.2f;
		rock = player.character as Rock;
		if (rock != null) rock.saRocketPunchProj = this;
		maxReverseTime = 0.5f;
		this.player = player;
		shooter = player.character;
		destroyOnHit = false;
		canBeLocal = false;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new SARocketPunchProj(
			SARocketPunch.netWeapon, arg.pos, arg.xDir, arg.player,
			arg.netId
		);
	}


	public override void update() {
		base.update();
		if (!locallyControlled) return;

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
				if (pois != null && pois.Count > 0) {
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
			if (shootPos != null) new SARocketPunchProj(new SARocketPunch(), shootPos.Value.addxy(0, 0), character.getShootXDir(), player, player.getNextActorNetId(true), true);
		}

		if (character.isAnimOver()) {
			if (character.grounded) character.changeState(new Idle(), true);
			else character.changeState(new Fall(), true);
		}
	}
}
