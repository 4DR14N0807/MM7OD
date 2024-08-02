using System;
using System.Collections.Generic;
using SFML.Graphics;

namespace MMXOnline;

public class GravityHold : Weapon {

	public static GravityHold netWeapon = new();

	public GravityHold() : base() {
		displayName = "GRAVITY HOLD";
		descriptionV2 = "";
		defaultAmmoUse = 4;

		index = (int)RockWeaponIds.GravityHold;
		fireRateFrames = 60;
		hasCustomAnim = true;
	}

	public override float getAmmoUsage(int chargeLevel) {
		return defaultAmmoUse;
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Point shootPos = character.getCenterPos();
		int xDir = character.getShootXDir();
		Player player = character.player;
		Blues blues = character as Blues ?? throw new NullReferenceException();
		blues.gHoldOwnerYDir *= -1;
		int yDir = blues.gHoldOwnerYDir;
		//character.changeState(new GravityHoldState(), true);

		if (args[1] == 1) {
			if (character.charState is not LadderClimb) character.changeState(new BluesShootAlt(this), true);
			else character.changeState(new BluesShootAltLadder(this), true);
		} else if (args[1] == 2) {
			new GravityHoldProj(shootPos, xDir, player, player.getNextActorNetId(), true);
		}
	}
}


public class GravityHoldProj : Projectile {

	bool fired;

	public GravityHoldProj(
		Point pos, int xDir, Player player, 
		ushort? netProjId, bool rpc = false
	) : base 
	(
		GravityHold.netWeapon, pos, xDir, 0, 0,
		player, "empty", 0, 0, netProjId,
		player.ownedByLocalPlayer
	) {
		projId = (int)BluesProjIds.GravityHold;
		maxTime = 0.1f;
		shouldShieldBlock = false;
		destroyOnHit = false;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters args) {
		return new GravityHoldProj(
			args.pos, args.xDir, args.player, args.netId
		);
	}

	public override void update() {
		base.update();

		Rect rect = new Rect(pos.x - 80, pos.y - 80, pos.x + 80, pos.y + 80);
		var hits = Global.level.checkCollisionsShape(rect.getShape(), new List<GameObject>() { this });
		
		new GravityHoldEffect(pos, damager.owner.character);

		foreach (var gameObject in Global.level.getGameObjectArray()) {
			if (gameObject is Actor actor && !fired &&
				actor.ownedByLocalPlayer &&
				gameObject is Character chr &&
				chr.canBeDamaged(damager.owner.alliance, damager.owner.id, null)
			) {
				if (chr != null && chr.player.alliance == damager.owner.alliance) continue;
				if (chr != null && chr.isCCImmune()) continue;
				if (chr != null && chr.grounded) return;
				if (chr != null && chr.gHoldOwner != null && chr.gHoldOwner != damager.owner) return;
				//if (actor.gHoldOwner != damager.owner) continue;

				if (chr != null && chr.pos.distanceTo(pos) <= 80) {
					chr.gHoldOwner = damager.owner;

					if (!chr.gHolded) {
						chr.gHoldStart();
					} else {
						chr.gHoldEnd(false);
					}
				}
				fired = true;
			}
		}

		/*foreach (CollideData other in hits) {
			var actor = other.gameObject as Actor;
			var chr = other.gameObject as Character;

			if (actor != null && actor.ownedByLocalPlayer && !fired) {
				if (chr != null && chr.player.alliance == damager.owner.alliance) continue;
				if (chr != null && chr.isCCImmune()) continue;
				if (chr != null && chr.grounded) return;
				if (chr != null && chr.gHoldOwner != null && chr.gHoldOwner != damager.owner) return;
				//if (actor.gHoldOwner != damager.owner) continue;

				if (chr != null) {
					chr.gHoldOwner = damager.owner;

					if (!chr.gHolded) {
						chr.gHoldStart();
					} else {
						chr.gHoldEnd(false);
					}
				}
				new GravityHoldEffect(pos, damager.owner.character);
				fired = true;
			}
		}*/
	}
}


public class GravityHoldEffect : Effect {

	Character rootChar;
	int effectFrames;
	Rect rect;

	public GravityHoldEffect(Point pos, Character character) : base(pos) {
		rootChar = character;
	}

	public override void update() {
		base.update();

		rect = new Rect(pos.x - 80, pos.y - 80, pos.x + 80, pos.y + 80);
		effectFrames++;

		if (effectFrames >= 30) destroySelf();
	}

	public override void render(float x, float y) {
		base.render(x, y);
		DrawWrappers.DrawCircle(pos.x, pos.y, 80, true, 
		new Color(255,255,255,255), 1, ZIndex.Backwall);
	}
}


public class GravityHoldState : CharState {

	Blues blues = null!;
	bool fired;

	public GravityHoldState() : base("strikeattack") {
		normalCtrl = false;
		attackCtrl = false;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		blues = character as Blues ?? throw new NullReferenceException();
		blues.gHoldOwnerYDir *= -1;
	}

	public override void update() {
		base.update();

		if (!fired && character.isAnimOver()) {
			new GravityHoldProj(character.getCenterPos(), character.getShootXDir(), 
				player, player.getNextActorNetId(), true);
			fired = true;
		}

		if (character.isAnimOver()) character.changeToIdleOrFall();
	}
}
