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
	public bool once;

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

		if (rush.netOwner?.character != null) {
			rockPos = rush.netOwner.character.pos;
		}
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
		if (Global.level.checkTerrainCollisionOnce(rActor, rActor.xDir, 1) == null) {
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
	Rock rock = null!;
	public float jetSpeedX;
	public float jetSpeedY;
	int decAmmoCooldown = 30;
	int maxDecAmmoCooldown = 30;
	int xDir;
	Point input;
	bool rideOnce;
	bool playedSound;
	public RushJetState() : base("rush_jet_start") { }

	public override void onEnter(RushState oldState) {
		base.onEnter(oldState);
		rush.isPlatform = true;
		rush.globalCollider = rush.getJetCollider();
		rush.useGravity = false;
		rush.grounded = false;
		rush.canBeGrounded = false;
		rock = rush.character as Rock ?? throw new NullReferenceException();
		Global.level.modifyObjectGridGroups(rush, isActor: true, isTerrain: true);
	}

	public override void onExit(RushState newState) {
		base.onExit(newState);
		rush.canBeGrounded = true;
		rush.isPlatform = false;
		rush.stopMoving();
	}

	public override void update() {
		base.update();

		if (rock.isUsingRushJet()) {
			if (!once) {
				rush.changeSprite("rush_jet", true);
				rush.playSound("rush_jet", true);
			}
			xDir = player.input.getXDir(player);
			input.y = player.input.getYDir(player);
			if (xDir != 0) input.x = xDir;

			if (input.x == rush.xDir) {
				jetSpeedX = 120;
			} else {
				jetSpeedX = 60;
			}
			jetSpeedY = input.y * 60;
			once = true;
			player.delayETank();
		} else {
			maxDecAmmoCooldown = 45;
			jetSpeedX = once ? 60 : 0;
			jetSpeedY = 0;
		}

		//rush.vel = new Point(jetSpeedX * rush.xDir, jetSpeedY);
		rush.vel.x = jetSpeedX * rush.xDir;
		rush.vel.y = jetSpeedY;

		if (once) {
			decAmmoCooldown--;
		}
		if (decAmmoCooldown <= 0) {
			rock.rushWeapon.addAmmo(-1, player);
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
	Rock rock = null!;
	Point pickupPos;
	int pickupTime;
	int sound;
	string soundStr = null!;
	double dice;
	public RushSearchState() : base("rush_dig_start", "rush_smell") {

	}

	public override void onEnter(RushState oldState) {
		base.onEnter(oldState);
		rock = rush.character as Rock ?? throw new NullReferenceException();
		pickupPos = new Point(rush.pos.x + (rush.xDir * 10), rush.pos.y - 16);
		sound = Helpers.randomRange(0, 1);
		soundStr = sound == 0 ? "rush_search_searching1" : "rush_search_searching2";
	}

	public override void update() {
		base.update();

		if (digging) digTime++;
		//not needed animore
		/*if (pickup != null) {
			pickupTime++;
			if (pickupTime >= 30) {
				pickup.destroySelf();
				pickup = null;
			}
		}*/

		if (inTransition() && stateTime % 15 == 0) Global.playSound("rush_search_start");
		if (state == 1 && stateTime % 12 == 0) {
			Global.playSound(soundStr);
		}

		switch (state) {
			case 0:
				if (!inTransition() && rush.isAnimOver()) {
					rush.changeSprite("rush_dig", true);
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
					dice = Helpers.randomRange(1, 1000);
					getRandomItem();
					rock.rushWeapon.addAmmo(-4, player);

					state = 3;
				} break;

			default:
				if (rush.isAnimOver()) rush.changeState(new RushWarpOut());
				break;
		}
	}

	void getRandomItem() {
		if (!rush.ownedByLocalPlayer) {
			return;
		}
		string text = "";
		string sound = "";
		FontType font;
		Point pickupVel = new Point(0, -300);
		dice /= 10;

		// Full Heal.
		if (dice > 98.5){
			sound = "upgrade";
			text = "FULL HEAL";
			font = FontType.Blue;

			new GiantHealthPickup(player, pickupPos, player.getNextActorNetId(), true, true) 
			{ vel = pickupVel, teamOnly = true };
		}
		// Full Ammo refill. 
		else if (dice > 97) {
			sound = "upgrade";
			text = "FULL AMMO REFILL";
			font = FontType.Green;

			new GiantAmmoPickup(player, pickupPos, player.getNextActorNetId(), true, true) 
			{ vel = pickupVel, teamOnly = true };	
		}
		// 100 bolts 
		else if (dice > 95.5) {
			sound = "upgrade";
			text = "100 BOLTS!!!";
			font = FontType.Yellow;

			new GiantBoltPickup(player, pickupPos, player.getNextActorNetId(), true, true) 
			{ vel = pickupVel, teamOnly = true };	
		}
		// Bomb
		else if (dice > 94) {
			sound = "rush_search_end";
			text = "KA-BOOM!!!";
			font = FontType.Red;

			new RSBombProj(rock, pickupPos, 1, rush.player?.getNextActorNetId(), true, rush.player);
		}
		// Big HP
		else if (dice > 86) {
			sound = "upgrade";
			text = "BIG HEALTH CAPSULE!";
			font = FontType.Blue;

			new LargeHealthPickup(player, pickupPos, player.getNextActorNetId(), true, true) 
			{ vel = pickupVel, teamOnly = true };
		}
		// Big ammo 
		else if (dice > 78) {
			sound = "upgrade";
			text = "BIG AMMO CAPSULE!";
			font = FontType.Green;

			new LargeAmmoPickup(player, pickupPos, player.getNextActorNetId(), true, true) 
			{ vel = pickupVel, teamOnly = true };
		} 
		// 40 Bolts
		else if (dice > 70) {
			sound = "upgrade";
			text = "40 BOLTS!";
			font = FontType.Yellow;

			for (int i = 0; i < 5; i++) {
				new LargeBoltPickup(player, pickupPos, player.getNextActorNetId(), true, true) 
				{ xPushVel = Helpers.randomRange(-2, 2) * 0.5f, vel = new Point(0, pickupVel.y / 2), teamOnly = true };
			}
		}
		// Small HP 
		else if (dice > 55) {
			sound = "rush_search_end";
			text = "SMALL HEALTH CAPSULE";
			font = FontType.Blue;

			new SmallHealthPickup(player, pickupPos, player.getNextActorNetId(), true, true) 
			{ vel = pickupVel, teamOnly = true };
		}
		//Small ammo
		else if (dice > 40) {
			sound = "rush_search_end";
			text = "SMALL AMMO CAPSULE";
			font = FontType.Green;

			new SmallAmmoPickup(player, pickupPos, player.getNextActorNetId(), true, true) 
			{ vel = pickupVel, teamOnly = true };
		}
		// 10 Bolts
		else if (dice > 25) {
			sound = "rush_search_end";
			text = "10 BOLTS";
			font = FontType.Yellow;

			for (int i = 0; i < 5; i++) {
				new SmallBoltPickup(player, pickupPos, player.getNextActorNetId(), true, true) 
				{ xPushVel = Helpers.randomRange(-2, 2) * 0.5f, vel = new Point(0, pickupVel.y / 2), teamOnly = true };
			}
		}
		// Trash
		else {
			sound = "rush_search_end";
			text = "TRY AGAIN";
			font = FontType.Red;

			new Trash(pickupPos, player.getNextActorNetId(), true, true);
		}

		rush.playSound(sound, true);
		Global.level.gameMode.setHUDErrorMessage(
			player, text, false, overrideFont: font
		);
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
			//if (rock != null) rock.rush = null!;
		} 
	}
}

public class Trash : Anim {
	public Trash(Point pos, ushort? netId = null, bool sendRpc = false, bool ownedByLocalPlayer = true) :
		base(pos, "rush_pickups", 1, netId, false, sendRpc, ownedByLocalPlayer) {
		setzIndex(ZIndex.Default);
		frameSpeed = 0;
		frameIndex = Helpers.randomRange(0, sprite.totalFrameNum - 1);
		vel = new Point(0, -300);
		useGravity = true;
		if (collider != null) collider.wallOnly = true;
	}

	public override void update() {
		base.update();
		//add a fade anim on destroy
		if(time >= 2 || checkCollision(0, 1) != null) {
			destroySelf();
			new Anim(pos,"dust", xDir, netId, true, sendRpc: true);
		}
	}
}
