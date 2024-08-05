using System;
using System.Collections.Generic;

namespace MMXOnline;

public class RushState {
	public string sprite;
	public string defaultSprite;
	public string transitionSprite;
	public Rush rush = null!;
	public Character character = null!;
	public float stateTime;
	public float stateSeconds => stateTime / Global.secondsFrameDuration;

	public RushState(string sprite, string transitionSprite = "") {
		this.sprite = string.IsNullOrEmpty(transitionSprite) ? sprite : transitionSprite;
		this.transitionSprite = transitionSprite;
		defaultSprite = sprite;
	}

	public Player player => rush.player;

	public virtual void onExit(RushState newState) { }

	public virtual void onEnter(RushState oldState) { }

	public bool inTransition() {
		return (!string.IsNullOrEmpty(transitionSprite) &&
			sprite == transitionSprite &&
			rush?.sprite?.name != null &&
			rush.sprite.name.Contains(transitionSprite)
		);
	}

	public virtual void preUpdate() { }
	public virtual void update() { 
		if (inTransition()) {
			rush.frameSpeed = 1;
			if (rush.isAnimOver() && !Global.level.gameMode.isOver) {
				sprite = defaultSprite;
				rush.changeSprite(sprite, true);
			}
		} 
	}
	public virtual void postUpdate() { }

	public virtual bool canEnter(Rush rush) {
		return true;
	}

	public virtual bool canExit(Rush rush, RushState newState) {
		return true;
	}
}

public class RushWarpIn : RushState {
	public Point destY;
	public Point rockPos;
	public Anim warpAnim = null!;
	bool landed;
	bool warped;

	public RushWarpIn(bool addInvulnFrames = true) : base("rush_warp_beam") { }

	public override bool canEnter(Rush rush) {
		return rush.rushState is not RushWarpIn;
	}

	public override void onEnter(RushState oldState) {
		base.onEnter(oldState);
		rush.stopMoving();
		rush.useGravity = false;
		rush.frameSpeed = 1;
		rush.vel.y = 300;

		if (rush.netOwner != null) rockPos = rush.netOwner.character.pos;
		Point? checkGround = Global.level.getGroundPosNoKillzone(character.pos);
		//rush.pos = checkGround.GetValueOrDefault();
		//warpAnim = new Anim(new Point(rush.pos.x, rush.pos.y - 200), "rush_warp_beam", 1, null, false);
	}

	public override void onExit(RushState newState) {
		base.onExit(newState);
		rush.visible = true;
		rush.useGravity = true;
		rush.splashable = true;
		//warpAnim.destroySelf();
	}

	public override void update() {
		base.update();
		//warpAnim.move(warpAnim.pos.directionToNorm(rush.pos).times(300));

		/*if (warpAnim.pos.distanceTo(rush.pos) <= 32) {
			warpAnim.destroySelf();
			rush.changeSprite("rush_warp_in", true);
			landed = true;
		}*/
		if (canLand(rush)) {
			rush.vel.y = 0;
			rush.changeSprite("rush_warp_in", false);
			landed = true;
		}

		if (landed && rush.isAnimOver()) rush.changeState(new RushIdle());

		if (stateTime >= 120) rush.destroySelf();
	}

	public override void postUpdate() {
		base.postUpdate();
	}

	public bool canLand(Actor rActor) {
		if (Global.level.checkCollisionActor(rActor, rActor.xDir, 1) == null) {
			return false;
		}
		List<CollideData> hits = Global.level.getTriggerList(rActor, rActor.xDir, 1, null, new Type[] { typeof(KillZone) });
		if (hits.Count > 0) {
			return false;
		}
		return true;
	}
}

public class RushIdle : RushState {

	int count;
	bool otherAnim;
	public RushIdle() : base("rush_idle") {

	}

	public override void onEnter(RushState oldState) {
		base.onEnter(oldState);
		//rush.isPlatform = true;
		//rush.xDir = rush.character.xDir;
	}

	public override void update() {
		base.update();

		if (rush.type == 1 && stateTime >= 10) rush.changeState(new RushJetState());
		else if (rush.type == 2 && stateTime >= 10) rush.changeState(new RushSearchState());
		else {
			if (otherAnim && rush.isAnimOver()) {
				rush.changeSprite("rush_idle", true);
				otherAnim = false;
				stateTime = 0;
			}

			if (stateTime >= 180 && !otherAnim) {
				if (count < 2) rush.changeSprite("rush_look_around", true);
				else if (count < 4) rush.changeSprite("rush_yawn", true);
				else rush.changeState(new RushSleep());
				count++;
				otherAnim = true;
				stateTime = 0;
			}
		}
	}
}


public class RushSleep : RushState {

	int sleepTime;
	bool sleeping;
	
	public RushSleep() : base("rush_sleep_start") {

	}

	public override void update() {
		base.update();

		if (rush.isAnimOver()) {
			rush.changeSprite("rush_sleep", true);
			sleeping = true;
		}

		if (sleeping) sleepTime++;
		if (sleepTime >= 120) rush.changeState(new RushWarpOut());
	}
}

public class RushCoil : RushState {
	public RushCoil() : base("rush_coil") {
	}

	public override void onEnter(RushState oldState) {
		base.onEnter(oldState);
		Global.playSound("rush_coil");
	}

	public override void update() {
		base.update();
		if (rush.isAnimOver()) {
			rush.changeState(new RushWarpOut());
		}
	}
}

public class RushJetState : RushState {
	Rock? rock;
	bool isRiding;
	public float jetSpeedX;
	public float jetSpeedY;
	int decAmmoCooldown = 30;
	int maxDecAmmoCooldown = 30;
	int xDir;
	int yDir;
	bool rideOnce;
	bool playedSound;
	public RushJetState() : base("rush_jet_start") { }

	public override void onEnter(RushState oldState) {
		base.onEnter(oldState);
		rush.isPlatform = true;
		rush.globalCollider = rush.getJetCollider();
		rush.useGravity = false;
		rock = rush.character as Rock;
	}

	public override void onExit(RushState newState) {
		base.onExit(newState);
		rush.isPlatform = false;
		rush.stopMoving();
	}

	public override void update() {
		base.update();

		if (rush.character.charState is RushJetRide) {
			rideOnce = true;
			isRiding = true;
			rush.changeSprite("rush_jet", true);
			if (!playedSound) {
				Global.playSound("rush_jet");
				playedSound = true;
			}
		} else isRiding = false;

		xDir = player.input.getXDir(player);
		yDir = player.input.getYDir(player);
		
		if (isRiding) {
			maxDecAmmoCooldown = 30;
			if (xDir == rush.xDir * -1) jetSpeedX = 60;
			else jetSpeedX = 120;

			if (yDir != 0 && Global.level.checkCollisionActor(rush, 0, yDir * 48) == null) jetSpeedY = yDir * 60;
			else jetSpeedY = 0;
		} else {
			maxDecAmmoCooldown = 45;
			jetSpeedX = rideOnce ? 60 : 0;
			jetSpeedY = 0;
		}

		rush.vel = new Point(jetSpeedX * rush.xDir, jetSpeedY);

		if (rideOnce) decAmmoCooldown--;
		if (decAmmoCooldown <= 0) {
			rock?.rushWeapon.addAmmo(-1, player);
			decAmmoCooldown = maxDecAmmoCooldown;
		}

		if (rock?.rushWeapon.ammo <= 0) {
			rush.changeState(new RushWarpOut());
			rush.character.changeToIdleOrFall();
		} 
	}
}


public class RushSearchState : RushState {

	public int state;
	bool digging;
	int digTime;
	Rock? rock;
	Anim? pickup;
	Point pickupPos;
	int pickupTime;
	int sound;
	string soundStr = null!;
	public RushSearchState() : base("rush_dig_start", "rush_smell") {

	}

	public override void onEnter(RushState oldState) {
		base.onEnter(oldState);
		rock = rush.character as Rock;
		pickupPos = new Point(rush.pos.x + (rush.xDir * 10), rush.pos.y);
		sound = Helpers.randomRange(0, 1);
		soundStr = sound == 0 ? "rush_search_searching1" : "rush_search_searching2";
	}

	public override void update() {
		base.update();

		if (digging) digTime++;
		if (pickup != null) {
			pickupTime++;
			if (pickupTime >= 30) {
				pickup.destroySelf();
				pickup = null;
			}
		}

		if (inTransition() && stateTime % 15 == 0) Global.playSound("rush_search_start");
		if (state == 1 && stateTime % 12 == 0) {
			Global.playSound(soundStr);
		}

		switch (state) {
			case 0:
				if (!inTransition() && rush.isAnimOver()) {
					rush.changeSprite("rush_dig", true);
					rush.player.currency -= rock?.RushSearchCost ?? 5;
					digging = true;
					state = 1;
				} break;
			
			case 1:
				if (digTime >= 90) {
					rush.changeSprite("rush_find", true);
					state = 2;
				} break;
			
			case 2: 
				if (rush.isAnimOver()) {
					rush.changeSprite("rush_dig_end", true);

					//RNG starts here.
					int dice = Helpers.randomRange(1, 100);
					getRandomItem(dice);

					state = 3;
				} break;

			default:
				if (rush.isAnimOver()) rush.changeState(new RushWarpOut());
				break;
		}
	}

	void getRandomItem(int dice) {
		var pl = Global.level.mainPlayer;
		var clonePos = pickupPos.clone();

		// 20 Bolts.
		if (dice is > 95) {
			Global.playSound("upgrade");
			var pickups = new List<Pickup>() {
				new SmallBoltPickup(pl, clonePos, pl.getNextActorNetId(), 
				true, true),
				new SmallBoltPickup(pl, clonePos, pl.getNextActorNetId(), 
				true, true),
				new LargeBoltPickup(pl, clonePos, pl.getNextActorNetId(), 
				true, true),
			};
			foreach (var pickup in pickups) {
				float velX = Helpers.randomRange(1, 60) * Helpers.randomRange(-1, 1);
				pickup.vel = new Point(velX, -360);
			} return; 
		} else if (dice is >= 86 and <= 95) {
			// Large HP/Ammo capsule.
			Global.playSound("upgrade");
			if (dice % 2 == 0) {
				new LargeHealthPickup(pl, clonePos, pl.getNextActorNetId(), 
				true, true) {vel = new Point(0, -360)};
			} else {
				new LargeAmmoPickup(pl, clonePos, pl.getNextActorNetId(), 
				true, true) {vel = new Point(0, -360)};
			} return;
		} else if (dice is >= 71 and <= 85) {
			// Small HP/Ammo capsule.
			Global.playSound("rush_search_end");
			if (dice % 2 == 0) {
				new SmallHealthPickup(pl, clonePos, pl.getNextActorNetId(), 
				true, true) {vel = new Point(0, -360)};
			} else {
				new SmallAmmoPickup(pl, clonePos, pl.getNextActorNetId(), 
				true, true) {vel = new Point(0, -360)};
			} return;
		} else if (dice is >= 41 and <= 70) {
			//5 Bolts.
			Global.playSound("rush_search_end");
			new SmallBoltPickup(pl, clonePos, pl.getNextActorNetId(), 
			true, true)  {vel = new Point(0, -360)}; return;
		} else if (dice is >= 6 and <= 40) {
			// Trash.
			Global.playSound("rush_search_end");
			pickup = new Anim(pickupPos, "rush_pickups", 1, rush.player.getNextActorNetId(), false, true)
				{vel = new Point(0, -360), useGravity = true, frameSpeed = 0};

			pickup.frameIndex = Helpers.randomRange(0, pickup.sprite.frames.Count - 1);
			pickup.setzIndex(ZIndex.Default); return;
		} else {
			// Bomb.
			Global.playSound("rush_search_end");
			new RSBombProj(pickupPos, 1, rush.player, rush.player.getNextActorNetId(), true);
		} return;
	}
}

public class RushHurt : RushState {

	int hurtDir;
	float hurtMoveSpeed;
	public RushHurt(int dir) : base ("rush_hurt") {
		hurtDir = dir;
		hurtMoveSpeed = dir * 100;
	}

	public override void onEnter(RushState oldState) {
		base.onEnter(oldState);
		rush.useGravity = false;
		Global.playSound("hurt");
	}

	public override void update() {
		base.update();
		
		if (hurtMoveSpeed != 0) {
			hurtMoveSpeed = Helpers.toZero(hurtMoveSpeed, 400 * Global.spf, hurtDir);
			rush.move(new Point(-hurtMoveSpeed, -character.getJumpPower() * 0.125f));
		}

		if (stateTime >= 36) rush.changeState(new RushWarpOut());
	}
}


public class RushWarpOut : RushState {

	int time;
	bool beam;
	Rock? rock;

	public RushWarpOut() : base("rush_warp_beam", "rush_warp_out") {
		
	}

	public override void onEnter(RushState oldState) {
		base.onEnter(oldState);
		rock = rush.character as Rock;
		//rush.physicsCollider = null;
		rush.globalCollider = null;
	}

	public override bool canEnter(Rush rush) {
		return rush.rushState is not RushWarpOut &&
			rush.rushState is not RushWarpIn;
	}

	public override void update() {
		base.update();

		if (!inTransition()) {
			rush.vel.y = -240;
			beam = true;
		}

		if (beam) time++;
		
		if (time >= 60) {
			rush.destroySelf();
			if (rock != null) rock.rush = null!;
		} 
	}
}
