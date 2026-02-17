using System;
using System.Collections.Generic;

namespace MMXOnline;

public class Met : NeutralEnemy {
	
	public float maxCooldown = 60;
	public float cooldown = 60;
	public float distance = 80;
	public int shotCount;
	public Met(
		Point pos, int xDir, Player player, ushort netId, bool isLocal, 
		int alliance = GameMode.stageAlliance, bool rpc = false, bool addToLevel = true
	) : base(
		pos, netId, isLocal, alliance, addToLevel
	) {
		netOwner = player;
		base.xDir = xDir;
		maxHealth = 3;
		health = maxHealth;
		changeState(new MetIdle());

		netActorCreateId = NetActorCreateId.Met;
		if (rpc) {
			createActorRpc(netOwner.id);
		}
	}

	public override string getSprite(string spriteName) {
		return "met_" + spriteName;
	}

	public override void update() {
		base.update();

		invincibleFlag = state is MetIdle;

		if (!ownedByLocalPlayer) return;

		Helpers.decrementFrames(ref cooldown);

	}

	public void shoot() {
		if (!ownedByLocalPlayer) return;

		if (shotCount >= 2) {
			shotCount = 0;
			changeState(new MetIdle());
			cooldown = 30;
		} else {
			changeState(new MetShoot());
			for (int i = 0; i < 3; i++) {
				new MetLemon(this, pos.addxy(xDir * 13, -3), xDir, i, Player.stagePlayer.getNextActorNetId(), true);
			}
			shotCount++;
			playSound("buster", sendRpc: true);
		}

		
	}

	public override void onDestroy() {
		base.onDestroy();
		if (!ownedByLocalPlayer) return;

		new Anim(pos, "generic_explosion", xDir, Player.stagePlayer.getNextActorNetId(), true, true);
		playSound("danger_wrap_explosion", sendRpc: true);
	}
}

public class MetIdle : NeutralEnemyState {

	Met? met;
	public MetIdle() : base("idle") {
		normalCtrl = true;
		attackCtrl = true;
	}

	public override void onEnter(NeutralEnemyState oldState) {
		base.onEnter(oldState);
		chara.frameSpeed = 0;
		chara.frameIndex = 0;
		met = chara as Met;
	}

	public override void update() {
		base.update();

		if (met != null && met.cooldown <= 0) {
			met.cooldown = met.maxCooldown;
			Actor? target = Global.level.getClosestTarget(met.pos, met.alliance, false, met.distance);
			if (target != null) {
				met.turnToPos(target.pos);
				met.shoot();
			} 
		}
	}

	public override void onExit(NeutralEnemyState newState) {
		base.onExit(newState);
	}
}

public class MetShoot : NeutralEnemyState {

	Met? met;
	public MetShoot() : base("idle") {
		
	}

	public override void onEnter(NeutralEnemyState oldState) {
		base.onEnter(oldState);
		chara.frameIndex = 1;
		chara.frameSpeed = 0;
		met = chara as Met;
	}

	public override void update() {
		base.update();

		if (met != null && met.cooldown <= 0) {
			met.cooldown = met.maxCooldown;
			Actor? target = Global.level.getClosestTarget(met.pos, met.alliance, false, met.distance);
			if (target != null) {
				met.turnToPos(target.pos);
				met.shoot();
			} else {
				met.changeState(new MetIdle());
			}
		}
	}
}

	
public class MetLemon : Projectile {
	public MetLemon(
		Actor owner, Point pos, int xDir, int type,
		ushort? netId, bool rpc = false, Player? player = null
	) : base(
		pos, xDir, owner, "rock_buster_proj", netId, player
	) {
		projId = (int)NeutralEnemyProjIds.MetLemon;
		maxTime = 20 / 60f;

		float ang = -32 + (type * 32);
		if (xDir < 0) ang = -ang + 128;
		vel = Point.createFromByteAngle(ang).times(240);

		damager.owner = Player.stagePlayer;
		damager.damage = 1;
		damager.flinch = Global.defFlinch;
		damager.hitCooldown = 30;

		
		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)type };

			rpcCreate(pos, owner, ownerPlayer, netId, xDir, extraArgs);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new MetLemon(
			arg.owner, arg.pos, arg.xDir, 
			arg.extraData[0], arg.netId, player: arg.player
		);
	}
}	