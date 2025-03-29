using System;
using System.Collections.Generic;
using SFML.Graphics;

namespace MMXOnline;

public class BigBangStrikeProj : Projectile {
	public BigBangStrikeProj(
		Point pos, int xDir, Actor owner, ushort? netId,
		bool sendRpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "big_bang_strike_proj", netId, altPlayer
	) {
		projId = (int)BluesProjIds.BigBangStrike;
		damager.damage = 3;
		damager.flinch = Global.superFlinch;
		damager.hitCooldown = 30;

		maxTime = 0.925f;
		shouldShieldBlock = false;
		reflectable = false;
		canBeLocal = false;

		if (sendRpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new BigBangStrikeProj(
			args.pos, args.xDir, args.owner, args.netId, altPlayer: args.player
		);
	}

	public override void update() {
		base.update();
		if (System.MathF.Abs(vel.x) < 1000) {
			vel.x += time * 40 * Global.speedMul * xDir;
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
		if (ownedByLocalPlayer && ownerActor != null) {
			var proj = new BigBangStrikeExplosionProj(
				pos, xDir, ownerActor, damager.owner.getNextActorNetId(), true
			);
			proj.playSound("danger_wrap_explosion", true, true);
		}
	}
}

public class BigBangStrikeExplosionProj : Projectile {
	float radius = 38;
	float absorbRadius = 120;

	public BigBangStrikeExplosionProj(
		Point pos, int xDir, Actor owner, ushort? netId,
		bool sendRpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "big_bang_strike_explosion", netId, altPlayer
	) {
		// Damage.
		projId = (int)BluesProjIds.BigBangStrikeExplosion;
		damager.damage = 2;
		damager.flinch = Global.miniFlinch;
		damager.hitCooldown = 30;
		// Etc
		maxTime = 3f;
		destroyOnHit = false;

		if (sendRpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}

		projId = (int)BluesProjIds.BigBangStrike;
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new BigBangStrikeExplosionProj(
			args.pos, args.xDir, args.owner, args.netId, altPlayer: args.player
		);
	}

	public override void update() {
		base.update();

		foreach (var gameObject in Global.level.getGameObjectArray()) {
			if (gameObject is Actor actor &&
				actor.ownedByLocalPlayer &&
				gameObject is IDamagable damagable && gameObject is not CrackedWall && 
				damagable.canBeDamaged(damager.owner.alliance, damager.owner.id, null)
			) {
				if (actor.getCenterPos().distanceTo(pos) <= absorbRadius) {
					float direction = MathF.Sign(pos.x - actor.pos.x);
					actor.move(new Point(direction * 30, 0));
				}
				if (actor.getCenterPos().distanceTo(pos) <= radius) {
					damager.applyDamage(damagable, false, weapon, this, projId);
				}
			}
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		if (ownedByLocalPlayer && ownerActor != null) {
			var proj = new StrikeAttackPushProj(
				pos, 0, xDir, ownerActor, ownerPlayer.getNextActorNetId(), sendRpc: true
			);
		}
	}
}

public class ProtoStrikeProj : Projectile {
	Character? ownerChar;
	float radius = 48; //38
	float absorbRadius = 80;

	public ProtoStrikeProj(
		Point pos, int xDir, Actor owner, ushort? netId,
		bool sendRpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "big_bang_strike_explosion", netId, altPlayer
	) {
		// Damage.
		projId = (int)BluesProjIds.ProtoStrike;
		damager.damage = 2;
		damager.flinch = Global.miniFlinch;
		damager.hitCooldown = 30;
		// Etc.
		maxTime = 3f;
		destroyOnHit = false;
		canBeLocal = false;
		ownerChar = owner as Character;

		if (sendRpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new ProtoStrikeProj(
			args.pos, args.xDir, args.owner, args.netId, altPlayer: args.player
		);
	}

	public override void update() {
		base.update();
		if (ownedByLocalPlayer) {
			if (ownerChar?.charState is not ProtoStrike) {
				destroySelf();
				return;
			}
		}

		if (ownerChar != null) {
			changePos(ownerChar.getShootPos());
		}

		foreach (var gameObject in Global.level.getGameObjectArray()) {
			if (gameObject is Actor actor &&
				actor.ownedByLocalPlayer &&
				gameObject is IDamagable damagable && gameObject is not CrackedWall && 
				damagable.canBeDamaged(damager.owner.alliance, damager.owner.id, null)
			) {
				if (actor.getCenterPos().distanceTo(pos) <= absorbRadius) {
					float direction = MathF.Sign(pos.x - actor.pos.x);
					actor.move(new Point(direction * 60, 0));
				}
				if (actor.getCenterPos().distanceTo(pos) <= radius) {
					damager.applyDamage(damagable, false, weapon, this, projId);
				}
			}
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		if (ownedByLocalPlayer && ownerActor != null) {
			var proj = new StrikeAttackPushProj(
				pos, 1, xDir, ownerActor, ownerPlayer.getNextActorNetId(), sendRpc: true
			);
			proj.playSound("danger_wrap_explosion", true, true);
		}
	}

	public override void render(float x, float y) {
		long lastZIndex = zIndex;
		alpha = 0.5f;
		addRenderEffect(RenderEffectType.ChargeOrange, 0, 600);
		base.render(x, y);
		alpha = 1;
		zIndex = ZIndex.Character - 1000;
		addRenderEffect(RenderEffectType.ChargeOrange, 0, 600);
		base.render(x, y);
		removeRenderEffect(RenderEffectType.ChargeOrange);
		zIndex = lastZIndex;
	}
}

public class StrikeAttackPushProj : Projectile {
	public int type;
	public float radius = 45;
	public float pushPower = 200;
	public int flinchPower = Global.defFlinch;

	public StrikeAttackPushProj(
		Point pos, int type, int xDir, Actor owner, ushort? netId,
		bool sendRpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "big_bang_strike_fade", netId, altPlayer
	) {
		// Damage.
		projId = (int)BluesProjIds.BigBangStrike;
		damager.damage = 1;
		damager.flinch = Global.miniFlinch;
		damager.hitCooldown = 30;
		// Etc.
		projId = (int)BluesProjIds.ProtoStrikePush;
		destroyOnHit = false;
		canBeLocal = false;

		if (sendRpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
		}
		this.type = type;

		if (type == 1) {
			addRenderEffect(RenderEffectType.ChargeOrange, 0, 600);
		}
		else if (type == 3) {
			addRenderEffect(RenderEffectType.ChargeOrange, 0, 600);
			pushPower = 150;
			flinchPower = Global.halfFlinch;
			projId = (int)BluesProjIds.ProtoLandPush;
			zIndex = ZIndex.Backwall + 10000;
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new StrikeAttackPushProj(
			args.pos, args.extraData[0], args.xDir, args.owner, args.netId, altPlayer: args.player
		);
	}

	public override void update() {
		base.update();
		if (isAnimOver()) {
			destroySelf(doRpcEvenIfNotOwned: true);
		}
		foreach (var gameObject in Global.level.getGameObjectArray()) {
			if (gameObject is Actor actor &&
				actor.ownedByLocalPlayer &&
				gameObject is IDamagable damagable && gameObject is not CrackedWall && 
				damagable.canBeDamaged(damager.owner.alliance, damager.owner.id, null)
			) {
				if (actor.getCenterPos().distanceTo(pos) <= radius) {
					damager.applyDamage(damagable, false, weapon, this, projId);

					float direction = MathF.Sign(pos.x - actor.pos.x);
					actor.stopMovingWeak();
					actor.xPushVel = xDir * pushPower;
					if (actor is Character chara) {
						chara.setHurt(xDir, Global.defFlinch, false);
					}
				}
			}
		}
	}

	public override List<ShaderWrapper>? getShaders() {
		if (type == 2) {
			return [RedStrikeProj.redStrikePalette];
		}
		return base.getShaders();
	}
}

public class RedStrikeProj : Projectile {
	public static ShaderWrapper redStrikePalette = Helpers.cloneGenericPaletteShader("redstrike_palette");

	public RedStrikeProj(
		Point pos, int xDir, Actor owner, ushort? netId,
		bool sendRpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "big_bang_strike_proj", netId, altPlayer
	) {
		// Damage.
		projId = (int)BluesProjIds.RedStrike;
		damager.damage = 3;
		damager.flinch = Global.defFlinch;
		damager.hitCooldown = 30;
		// Etc.
		maxTime = 0.6f;
		shouldShieldBlock = false;
		reflectable = false;

		if (sendRpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new RedStrikeProj(
			args.pos, args.xDir, args.owner, args.netId, altPlayer: args.player
		);
	}

	public override void update() {
		base.update();
		if (System.MathF.Abs(vel.x) < 1000) {
			vel.x += time * 40 * Global.speedMul * xDir;
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
		if (ownedByLocalPlayer && ownerActor != null) {
			var proj = new RedStrikeExplosionProj(
				pos, xDir, ownerActor, damager.owner.getNextActorNetId(true), true
			);
			proj.playSound("danger_wrap_explosion", true, true);
		}
	}

	public override List<ShaderWrapper>? getShaders() {
		return [RedStrikeProj.redStrikePalette];
	}
}

public class RedStrikeExplosionProj : Projectile {
	float radius = 38;
	float absorbRadius = 120;

	public RedStrikeExplosionProj(
		Point pos, int xDir, Actor owner, ushort? netId,
		bool sendRpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "big_bang_strike_explosion", netId, altPlayer
	) {
		// Damage.
		projId = (int)BluesProjIds.RedStrikeExplosion;
		damager.damage = 1;
		damager.flinch = Global.miniFlinch;
		damager.hitCooldown = 30;
		// Etc.
		maxTime = 1f;
		destroyOnHit = false;
		fadeOnAutoDestroy = true;

		if (sendRpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
		projId = (int)BluesProjIds.RedStrike;
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new RedStrikeExplosionProj(
			args.pos, args.xDir, args.owner, args.netId, altPlayer: args.player
		);
	}

	public override void update() {
		base.update();

		foreach (var gameObject in Global.level.getGameObjectArray()) {
			if (gameObject is Actor actor &&
				actor.ownedByLocalPlayer &&
				gameObject is IDamagable damagable && gameObject is not CrackedWall && 
				damagable.canBeDamaged(damager.owner.alliance, damager.owner.id, null)
			) {
				if (actor.getCenterPos().distanceTo(pos) <= absorbRadius) {
					float direction = MathF.Sign(pos.x - actor.pos.x);
					actor.move(new Point(direction * 30, 0));
				}
				if (actor.getCenterPos().distanceTo(pos) <= radius) {
					damager.applyDamage(damagable, false, weapon, this, projId);
				}
			}
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		if (ownedByLocalPlayer && ownerActor != null) {
			var proj = new StrikeAttackPushProj(
				pos, 2, xDir, ownerActor, ownerPlayer.getNextActorNetId(), sendRpc: true
			);
		}
	}

	public override List<ShaderWrapper>? getShaders() {
		return [RedStrikeProj.redStrikePalette];
	}
}

public class BigBangStrikeStart : CharState {
	float shieldLossCD = 3;
	float coreHeatCD = 2;
	float shootTimer = 120;
	Blues blues = null!;
	BigBangStrikeBackwall bgEffect = null!;

	public BigBangStrikeStart() : base("idle_charge") {
		superArmor = true;
		stunResistant = true;
		immuneToWind = true;
	}

	public override void update() {
		base.update();
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
		if (coreHeatCD <= 0 && blues.coreAmmo < blues.coreMaxAmmo) {
			blues.coreAmmo = Helpers.clampMax(blues.coreAmmo + 1, blues.coreMaxAmmo);
			coreHeatCD = 2;
		} else {
			coreHeatCD -= Global.speedMul;
		}
		if (shootTimer <= 0) {
			blues.coreAmmo = blues.coreMaxAmmo;
			character.changeState(new BigBangStrikeState(), true);
		}
		shootTimer -= Global.speedMul;
	}

	public override void onEnter(CharState oldState) {
		blues = character as Blues ?? throw new NullReferenceException();
		character.stopMovingWeak();
		bgEffect = new BigBangStrikeBackwall(character.pos, character);
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		if (bgEffect.effectTime < 2) {
			bgEffect.effectTime = 2;
		}
		blues.isShieldActive = false;
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

		if (!fired && character.frameIndex >= 3 && player.ownedByLocalPlayer) {
			int shootDir = character.getShootXDir();
			new BigBangStrikeProj(
				character.getShootPos().addxy(shootDir * 10, 0), shootDir,
				character, player.getNextActorNetId(), true
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

		if (effectTime >= 3.2) {
			destroySelf();
		}
	}

	public override void render(float offsetX, float offsetY) {
		float transparecy = 100;
		if (effectTime < 0.2) {
			transparecy = effectTime * 500f;
		}
		if (effectTime > 3f) {
			transparecy = 100f - ((effectTime - 3f) * 500f);
		}

		DrawWrappers.DrawRect(
			Global.level.camX, Global.level.camY,
			Global.level.camX + 1000, Global.level.camY + 1000,
			true, new Color(0, 0, 0, (byte)System.MathF.Round(transparecy)), 1, ZIndex.Backwall
		);
	}
}
