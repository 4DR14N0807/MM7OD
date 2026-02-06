using System;
using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;

namespace MMXOnline;


public class Rush : Actor, IDamagable {
	public Character character = null!;
	public Rock rock = null!;
	public Player player;
	public RushState rushState;
	public bool usedCoil;
	public int type;
	public float health = 3;
	public bool isJetAndRide;
	public bool isRushJet;

	// Object initalization happens here.
	public Rush(
		Point pos, Player owner, int xDir, ushort netId,
		bool ownedByLocalPlayer, int type = 0, bool rpc = false
	) : base(
		"rush_warp_beam", pos, netId, ownedByLocalPlayer, false
	) {
		// Normal variables.
		// Hopefully character is not null.
		// Character begin null only matters for the local player tho.
		netOwner = owner;
		player = owner;
		this.xDir = xDir;

		if (ownedByLocalPlayer) {
			this.character = owner.character ?? throw new NullReferenceException();
			rock = character as Rock ?? throw new NullReferenceException();
			this.xDir = character.xDir;
		}
		this.type = type;
		hasStateMachine = true;
		//syncs rush xdir with rock xdir
		spriteToCollider["empty"] = null;
		//spriteToCollider["warp_beam"] = null;
		// Forcefull change sprite to something before we crash.
		sprite = new Sprite("empty");
		// We do this to manually call the state change.
		// As oldState cannot be null because we do not want null crashes.
		rushState = new RushState("empty") {
			rush = this,
			character = character
		};
		// Then now that we set up a dummy state we call the actual changeState.
		// Only do this for the local player as we do not want other player to run state code.
		if (ownedByLocalPlayer) {
			changeState(new RushWarpIn());
		}

		netActorCreateId = NetActorCreateId.Rush;
		if (rpc) {
			createActorRpc(owner.id);
		}
	}

	public override Collider? getTerrainCollider() {
		if (physicsCollider == null) {
			return null;
		}
		if (sprite.name.Contains("rush_jet")) {
			return getJetCollider();
		}
		return new Collider(
			new Rect(0f, 0f, 34, 22).getPoints(),
			false, this, false, false,
			HitboxFlag.Hurtbox, new Point(0, 0)
		);
	}

	public virtual Collider getJetCollider() {
		var rect = new Rect(0, 0, 40, 15);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public override Collider getGlobalCollider() {
		int yHeight = 22;
		if (sprite.name.Contains("rush_jet")) {
			yHeight = 15;
		}
		var rect = new Rect(0, 0, 34, yHeight);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public virtual void changeState(RushState newState) {
		if (newState == null) {
			return;
		}
		// Set the character as soon as posible.
		newState.rush = this;
		newState.character = character;

		if (!rushState.canExit(this, newState)) {
			return;
		}
		if (!newState.canEnter(this)) {
			return;
		}

		string spriteName = sprite?.name ?? "";
		if (newState.sprite == newState.transitionSprite &&
			!Global.sprites.ContainsKey(getSprite(newState.transitionSprite))
		) {
			newState.sprite = newState.defaultSprite;
		}
		changeSprite(newState.sprite, true);
		if (Global.sprites.ContainsKey(getSprite(newState.sprite)) &&
			sprite != null && spriteName == sprite.name) {
				sprite.frameIndex = 0;
				sprite.frameTime = 0;
				sprite.time = 0;
				sprite.frameSpeed = 1;
				sprite.loopCount = 0;
				sprite.visible = true;
			}

		RushState oldState = rushState;
		oldState.onExit(newState);

		rushState = newState;
		newState.onEnter(oldState);
	}

	public override void preUpdate() {
		base.preUpdate();
		if (!ownedByLocalPlayer) return;

		updateProjectileCooldown();
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;

		if (rushState is RushWarpOut) spriteToCollider["warp_beam"] = null;
		if (character == null || character.charState is Die || character.flag != null) {
			changeState(new RushWarpOut());
		}

		//Rush Jet detection
		if (rushState is RushJetState && rock.canRideRushJet() ) {
			isJetAndRide = true;
		} else {
			isJetAndRide = false;
		}

		isRushJet = rushState is RushJetState;
	}

	public override void postUpdate() {
		base.postUpdate();
	}

	public override void statePreUpdate() {
		rushState.stateTime += Global.speedMul;
		rushState.preUpdate();
	}

	public override void stateUpdate() {
		rushState.update();
	}

	public override void statePostUpdate() {
		rushState.postUpdate();
	}

	public virtual string getSprite(string spriteName) {
		return spriteName;
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (!ownedByLocalPlayer) { return; }

		if (other.gameObject is Wall && rushState is RushJetState) {
			if (other.isSideWallHit()) {
				changeState(new RushWarpOut());
			}
		}

		if (rushState is RushWarpIn && other.isGroundHit()) changeState(new RushIdle());

		if (other.otherCollider.actor is not Rock chr || chr.charState is Die) return;

		if (chr == netOwner?.character && chr.vel.y > 0 && chr != null ) {
			//Rush Coil detection
			if (!usedCoil && rushState is RushIdle && type == 0) {
				changeState(new RushCoil());
				chr.vel.y = -chr.getJumpPower() * 1.5f;
				chr.changeState(new Jump() { canStopJump = false }, true);
				chr.rushWeapon.addAmmo(-4, chr.player);
				usedCoil = true;
			}
		}
	}

	public void applyDamage(float damage, Player owner, Actor? actor, int? weaponIndex, int? projId) {
		health -= damage;
		if (!ownedByLocalPlayer) { return; }
		if (health <= 0) {
			changeState(new RushHurt(xDir));
		}
	}

	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) {
		return player?.alliance != damagerAlliance && isRushJet;
	}

	public bool isInvincible(Player attacker, int? projId) {
		return false;
	}

	public bool canBeHealed(int healerAlliance) {
		return false;
	}

	public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) {
	}

	public override void onDestroy() {
		if (!ownedByLocalPlayer) { return; }
		if (character is Rock rock && rock.rush != null) rock.rush = null!; 
	}

	public bool isPlayableDamagable() {
		return true;
	}

	public override bool canBePlatform(GameObject other) { return false; }

	public override List<byte> getCustomActorNetData() {
		// Get base arguments.
		return [
			Helpers.boolArrayToByte([ isRushJet ]),
		];
	}

	public override void updateCustomActorNetData(byte[] data) {
		// Update base arguments.
		bool wasRushJet = isRushJet;
	
		bool[] boolData = Helpers.byteToBoolArray(data[0]);
		isRushJet = boolData[0];

		if (!wasRushJet && isRushJet) {
			isPlatform = true;
			globalCollider = getJetCollider();
			useGravity = false;
			grounded = false;
			canBeGrounded = false;
		} 
	}
}

