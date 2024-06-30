using System;
using System.Collections.Generic;
using SFML.Graphics;

namespace MMXOnline;

public class BigBangStrikeProj : Projectile {
	public BigBangStrikeProj(
		Point pos, int xDir, Player player, ushort? netId, bool rpc = false
	) : base(
		ProtoBuster.netWeapon, pos, xDir, 0, 3, player, "big_bang_strike_proj",
		Global.halfFlinch, 0.5f, netId, player.ownedByLocalPlayer
	) {
		projId = (int)BluesProjIds.BigBangStrike;
		maxTime = 1.5f;
		shouldShieldBlock = false;
		reflectable = false;

		if (rpc) {
			rpcCreate(pos, player, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new BigBangStrikeProj(
			args.pos, args.xDir, args.player, args.netId
		);
	}

	public override void update() {
		base.update();
		if (System.MathF.Abs(vel.x) < 1000) {
			vel.x += Global.spf * xDir * 250f;
			if (System.MathF.Abs(vel.x) >= 1000) {
				vel.x = (float)xDir * 1000;
			}
		}
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		if (ownedByLocalPlayer) {
			destroySelf();
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		if (ownedByLocalPlayer) {
			var proj = new BigBangStrikeExplosionProj(
				pos, xDir, damager.owner, damager.owner.getNextActorNetId(true), true
			);
			proj.playSound("danger_wrap_explosion", true, true);
		}
	}
}


public class BigBangStrikeExplosionProj : Projectile {
	float radius = 38;

	public BigBangStrikeExplosionProj(
		Point pos, int xDir, Player player, ushort? netId, bool rpc = false
	) : base(
		ProtoBuster.netWeapon, pos, xDir, 0, 2, player, "big_bang_strike_explosion",
		Global.halfFlinch, 0.5f, netId, player.ownedByLocalPlayer
	) {
		projId = (int)BluesProjIds.BigBangStrikeExplosion;
		maxTime = 2f;
		fadeSprite = "big_bang_strike_fade";
		destroyOnHit = false;
		fadeOnAutoDestroy = true;

		if (rpc) {
			rpcCreate(pos, player, netId, xDir);
		}

		projId = (int)BluesProjIds.BigBangStrike;
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new BigBangStrikeExplosionProj(
			args.pos, args.xDir, args.player, args.netId
		);
	}

	public override void update() {
		base.update();

		foreach (var gameObject in Global.level.getGameObjectArray()) {
			if (gameObject is Actor actor &&
				actor.ownedByLocalPlayer &&
				gameObject is IDamagable damagable &&
				damagable.canBeDamaged(damager.owner.alliance, damager.owner.id, null) &&
				actor.getCenterPos().distanceTo(pos) <= radius
			) {
				damager.applyDamage(damagable, false, weapon, this, projId);
			}
		}
	}
}


public class BigBangStrikeStart : CharState {
	float shieldLossCD = 3;
	float shootTimer = 120;
	Blues blues = null!;

	public BigBangStrikeStart() : base("idle_chargeshield") {
		superArmor = true;
	}

	public override void update() {
		base.update();
		blues.coreAmmo = blues.coreMaxAmmo;
		blues.coreAmmoDecreaseCooldown = 10;
		blues.healShieldHPCooldown = 180;
		if (shieldLossCD <= 0 && blues.shieldHP > 0) {
			blues.playSound("tick", true);
			blues.shieldHP--;
			if (blues.shieldHP <= 0) {
				blues.shieldHP = 0;
				blues.shieldDamageDebt = 0;
			}
			shieldLossCD = 3;
			shootTimer -= 2;
		} else {
			shieldLossCD -= Global.speedMul;
		}
		if (shootTimer <= 0) {
			character.changeState(new BigBangStrikeState(), true);
		}
		shootTimer -= Global.speedMul;
	}

	public override void onEnter(CharState oldState) {
		blues = character as Blues ?? throw new NullReferenceException();
		character.stopMovingWeak();
		blues.isShieldActive = false;

		if (character.ownedByLocalPlayer && player == Global.level.mainPlayer) {
			new BigBangStrikeBackwall(character.pos, character);
		}
	}
}


public class BigBangStrikeState : CharState {
	bool fired;
	Blues blues = null!;

	public BigBangStrikeState() : base("strikeattack") {
		superArmor = true;
	}

	public override void update() {
		base.update();
		blues.coreAmmo = blues.coreMaxAmmo;
		blues.coreAmmoDecreaseCooldown = 10;

		if (blues != null) blues.coreAmmo = blues.coreMaxAmmo;

		if (!fired && character.frameIndex >= 3) {
			int shootDir = character.getShootXDir();
			new BigBangStrikeProj(
				character.getShootPos().addxy(shootDir * 10, 0), shootDir,
				player, player.getNextActorNetId(), true
			);
			fired = true;
			character.playSound("buster3", sendRpc: true);
		}
		if (stateFrames >= 60) {
			character.changeState(new OverheatShutdownStart(), true);
		}
	}

	public override void onEnter(CharState oldState) {
		blues = character as Blues ?? throw new NullReferenceException();
	}
}

public class BigBangStrikeBackwall : Effect {
	public Character rootChar;
	public int effectFrames;

	public BigBangStrikeBackwall(Point pos, Character character) : base(pos) {
		rootChar = character;
	}

	public override void update() {
		base.update();

		if (effectTime >= 2.2) {
			destroySelf();
		}
	}

	public override void render(float offsetX, float offsetY) {
		float transparecy = 100;
		if (effectTime < 0.2) {
			transparecy = effectTime * 500f;
		}
		if (effectTime > 2.2) {
			transparecy = 100f - ((effectTime - 2.2f) * 500f);
		}

		DrawWrappers.DrawRect(
			Global.level.camX, Global.level.camY,
			Global.level.camX + 1000, Global.level.camY + 1000,
			true, new Color(0, 0, 0, (byte)System.MathF.Round(transparecy)), 1, ZIndex.Backwall
		);
	}
}
