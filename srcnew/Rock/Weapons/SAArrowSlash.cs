using System;
using System.Collections.Generic;

namespace MMXOnline;

public class ArrowSlash : Weapon {
	public static ArrowSlash netWeapon = new();

	public ArrowSlash() : base() {
		index = (int)RockWeaponIds.SAArrowSlash;
		killFeedIndex = 0;
		hasCustomAnim = true;
	}
}


public class ArrowSlashProj : Projectile {
	float timeMoving;

	public ArrowSlashProj(
		Actor owner, Point pos, int xDir, ushort? netProjId,
		bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "slash_claw_proj", netProjId, altPlayer
	) {
		maxTime = 1f;
		projId = (int)RockProjIds.SAArrowSlash;

		damager.damage = 1;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netProjId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new ArrowSlashProj(
			arg.owner, arg.pos, arg.xDir, arg.netId, altPlayer: arg.player
		);
	}


	public override void update() {
		base.update();

		if (isAnimOver()) {
			timeMoving += Global.spf;
			base.vel.x = 240 * xDir;
		}

		if (timeMoving >= Global.spf * 8 && base.vel.y > -120) base.vel.y -= 5;

		damager.damage = getDamageIncrease();

	}

	int getDamageIncrease() {
		int finalDamage;
		finalDamage = (int)(time / (20f / 60f)) + 1;
		if (finalDamage >= 3) damager.flinch = Global.halfFlinch;
		return finalDamage;
	}
}

public class SAArrowSlashState : CharState {

	bool fired;
	Rock rock = null!;

	public SAArrowSlashState() : base("sa_arrowslash", "", "", "") {
		airMove = true;
	}

	
	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		rock = character as Rock ?? throw new NullReferenceException();
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		rock.triggerCooldown((int)Rock.AttackIds.ArrowSlash);
	}

	public override void update() {
		base.update();

		if (!fired) {
			new ArrowSlashProj(rock, rock.getCenterPos(), rock.xDir, player.getNextActorNetId(), true);
			fired = true;
			rock.playSound("slash_claw", sendRpc: true);
		}

		if (rock.isAnimOver()) rock.changeToIdleOrFall();
	}
}
