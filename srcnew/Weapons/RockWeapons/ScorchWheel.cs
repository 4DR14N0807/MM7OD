using System;
using System.Collections.Generic;

namespace MMXOnline;

public class ScorchWheel : Weapon {
	public static ScorchWheel netWeapon = new ScorchWheel();
	public ScorchWheel() : base() {
		index = (int)RockWeaponIds.ScorchWheel;
		weaponSlotIndex = (int)RockWeaponSlotIds.ScorchWheel;
		weaponBarBaseIndex = (int)RockWeaponBarIds.ScorchWheel;
		weaponBarIndex = weaponBarBaseIndex;
		killFeedIndex = 0;
		maxAmmo = 14;
		ammo = maxAmmo;
		fireRate = 60;
		description = new string[] { "A weapon able to burn enemies.", "Hold SHOOT to keep the barrier for longer." };
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		int chargeLevel = args[0];

		if (character.charState is LadderClimb lc) {
			character.changeState(new ShootAltLadder(lc.ladder, this, chargeLevel, character.isUnderwater()), true);
		} else {
			character.changeState(new ShootAlt(this, chargeLevel, character.isUnderwater()), true);
		}
	}

	public override void getProjs(Character character, params int[] args) {
		Player player = character.player;

		if (character.isUnderwater()) {
			new UnderwaterScorchWheelProj
			(
				character.getCenterPos(), character.getShootXDir(), 
				player, player.getNextActorNetId(true), rpc: true
			);
		} else {
			new ScorchWheelSpawn
			(
				character.getCenterPos(), character.getShootXDir(), player, 
				player.getNextActorNetId(true), rpc: true
			);
		}
	}
}


public class ScorchWheelSpawn : Projectile {

	public Rock? rock;
	bool hasHeld;
	public ScorchWheelSpawn(
		Point pos, int xDir, Player player, 	
		ushort netProjId, bool rpc = false
	) : base(
		ScorchWheel.netWeapon, pos, xDir, 0, 0,
		player, "scorch_wheel_spawn", 0, 0,
		netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)RockProjIds.ScorchWheelSpawn;
		rock = (player.character as Rock);
		if (rock != null) rock.sWellSpawn = this;
		useGravity = false;
		maxTime = 1;
		destroyOnHit = false;

		if (rpc) rpcCreate(pos, player, netProjId, xDir);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new ScorchWheelSpawn(
			arg.pos, arg.xDir, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();

		if (!ownedByLocalPlayer) return;

		//code that allow you to keep the wheel arround of you. Ruben: I swear if it doesnt work i kysm 
		//may be used as a base for future fixes
		if (owner.input.isHeld(Control.Shoot, owner)) {
			hasHeld = true;
		} else {
			hasHeld = false;
		}

		if (isAnimOver() && hasHeld == true) {
			new ScorchWheelProj(pos, xDir, damager.owner, damager.owner.getNextActorNetId(), rpc: true);
			destroySelf();
			playSound("scorch_wheel", true, true);
		} else if (isAnimOver()
				&& hasHeld == false
		) {
			destroySelf();
			new ScorchWheelMoveProj(pos, xDir, damager.owner, damager.owner.getNextActorNetId(), rpc: true);
			playSound("scorch_wheel", true, true);
			if (rock != null) rock.weaponCooldown = 60;
		}

	}

	public override void postUpdate() {
		base.postUpdate();
		if (!ownedByLocalPlayer) return;
		if (destroyed) return;

		if (rock != null) {
			xDir = rock.getShootXDir();
			pos = rock.getCenterPos();
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		if (rock != null) rock.sWellSpawn = null;
	}
}


public class ScorchWheelProj : Projectile {

	float projAngle;
	Rock? rock;
	Player player;
	Point centerPos;
	public List<Sprite> fireballs = new List<Sprite>();
	int radius = 15;
	float holdTime;
	bool hasHeld;

	public ScorchWheelProj(
		Point pos, int xDir, Player player, 
		ushort netProjId, bool rpc = false
	) : base(
		ScorchWheel.netWeapon, pos, xDir, 0, 1, 
		player, "scorch_wheel_proj", 0, 0.5f, 
		netProjId, player.ownedByLocalPlayer) {

		projId = (int)RockProjIds.ScorchWheel;
		destroyOnHit = false;
		this.player = player;
		rock = player.character as Rock;
		if (rock != null) rock.sWell = this;
		canBeLocal = false;

		for (int i = 0; i < 4; i++) {
			Sprite fireball = new Sprite("scorch_wheel_fireball");
			fireballs.Add(fireball);
		}

		if (rpc) rpcCreate(pos, player, netProjId, xDir);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new ScorchWheelProj(
			arg.pos, arg.xDir, arg.player, arg.netId
		);
	}


	public override void update() {
		base.update();
		if (projAngle >= 256) projAngle = 0;
		projAngle += 3;

		if (ownedByLocalPlayer) {
			if (player.input.isHeld(Control.Shoot, owner)) {
				hasHeld = true;
				holdTime += Global.spf;
			} else {
				hasHeld = false;
				holdTime = 0;
			}
			if (rock != null) {
				xDir = rock.getShootXDir();
				pos = rock.getCenterPos();
			}

			if (rock == null || rock.charState is Die || (rock.player.weapon is not ScorchWheel)) {
				destroySelf();
				return;
			}


			if (!hasHeld || holdTime >= 2) {
				destroySelf();
				new ScorchWheelMoveProj(pos, xDir, damager.owner, damager.owner.getNextActorNetId(true), rpc: true);
				playSound("scorch_wheel", true, true);
				rock.weaponCooldown = 60;
				return;
			}
		}
	}


	public override void render(float x, float y) {
		base.render(x, y);

		if (rock != null) centerPos = rock.getCenterPos();

		//main pieces render
		for (var i = 0; i < 4; i++) {
			float extraAngle = projAngle + i * 64;
			if (extraAngle >= 256) extraAngle -= 256;
			float xPlus = Helpers.cosd(extraAngle * 1.40625f) * radius;
			float yPlus = Helpers.sind(extraAngle * 1.40625f) * radius;
			if (rock != null) xDir = rock.getShootXDir();
			fireballs[i].draw(frameIndex, centerPos.x + xPlus, centerPos.y + yPlus, xDir, yDir, getRenderEffectSet(), 1, 1, 1, zIndex);
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		if (rock != null) rock.sWell = null;
	}
}


public class ScorchWheelMoveProj : Projectile {
	public Rock? rock;
	public ScorchWheelMoveProj(
		Point pos, int xDir, Player player, 
		ushort netProjId, bool rpc = false
	) : base(
		ScorchWheel.netWeapon, pos, xDir, 240, 1,
		player, "scorch_wheel_grounded_proj", 0, 1,
		netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)RockProjIds.ScorchWheelMove;
		useGravity = true;
		maxTime = 1.25f;
		canBeLocal = false;

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new ScorchWheelMoveProj(
			arg.pos, arg.xDir, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;

		if (collider != null) {
			collider.isTrigger = false;
			collider.wallOnly = true;
		}
		checkUnderwater();
	}

	public override void onHitWall(CollideData other) {
		if (!ownedByLocalPlayer) return;

		var normal = other.hitData.normal ?? new Point(0, -1);

		if (normal.isSideways()) {
			destroySelf();
		}
	}

	public void checkUnderwater() {
		if (isUnderwater()) {
			new BubbleAnim(pos, "bubbles") { vel = new Point(0, -60) };
			Global.level.delayedActions.Add(new DelayedAction(() => { new BubbleAnim(pos, "bubbles_small") { vel = new Point(0, -60) }; }, 0.1f));

			if (rock == null || rock.charState is Die || (rock.player.weapon is not JunkShield)) {
				destroySelf();
				return;
			}
		}
	}
}


public class UnderwaterScorchWheelProj : Projectile {

	int bubblesAmount;
	int counter = 0;
	int bubblesSmallAmount;
	int counterSmall = 0;
	Rock? rock;

	public UnderwaterScorchWheelProj(
		Point pos, int xDir, Player player, 
		ushort netProjId, bool rpc = false
	) : base(
		ScorchWheel.netWeapon, pos, xDir, 0, 2,
		player, "empty", 0, 1,
		netProjId, player.ownedByLocalPlayer
	) {
		maxTime = 1.5f;
		destroyOnHit = false;
		rock = player.character as Rock;
		if (rock != null) rock.sWellU = this;
		if (rock != null) rock.underwaterScorchWheel = this;

		bubblesAmount = Helpers.randomRange(2, 8);
		bubblesSmallAmount = Helpers.randomRange(2, 8);

		if (rpc) rpcCreate(pos, player, netProjId, xDir);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new UnderwaterScorchWheelProj(
			arg.pos, arg.xDir, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;

		Point centerPos = rock?.getCenterPos() ?? new Point(0, 0);

		if (counter < bubblesAmount) {
			int xOffset = Helpers.randomRange(-12, 12);
			int yOffset = Helpers.randomRange(-12, 12);
			centerPos = rock?.getCenterPos() ?? new Point(0, 0);
			Global.level.delayedActions.Add(new DelayedAction(() => {
				new BubbleAnim(new Point(centerPos.x + xOffset, centerPos.y + yOffset), "bubbles") { vel = new Point(0, -60) };
			},
				0.1f * counter));
			counter++;
		}

		if (counterSmall < bubblesSmallAmount) {
			int xOffset = Helpers.randomRange(-12, 12);
			int yOffset = Helpers.randomRange(-12, 12);
			centerPos = rock?.getCenterPos() ?? new Point(0, 0);
			Global.level.delayedActions.Add(new DelayedAction(() => {
				new BubbleAnim(new Point(centerPos.x + xOffset, centerPos.y + yOffset), "bubbles_small") { vel = new Point(0, -60) };
			},
				0.1f * counterSmall));
			counterSmall++;
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		if (rock != null) rock.sWellU = null!;
	}
}

public class Burning : CharState {
	public float burningTime = 120;
	public const int maxStacks = 4;
	public int burnDir;
	public float burnMoveSpeed;
	public int burnDamageCooldown = 45;
	Player attacker;

	public Burning(int dir, Player attacker) : base("burning") {
		superArmor = true;
		burnDir = dir;
		burnMoveSpeed = dir * 100;
		this.attacker = attacker;
		normalCtrl = false;
		attackCtrl = false;
	}

	public override bool canEnter(Character character) {
		if (!base.canEnter(character) ||
			character.burnInvulnTime > 0 ||
			character.isInvulnerable() ||
			character.charState.stunResistant ||
			character.grabInvulnTime > 0 ||
			character.charState.invincible ||
			character.isCCImmune()
		) {
			return false;
		}
		return true;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		if (!character.ownedByLocalPlayer) return;
		if (character.vel.y < 0) character.vel.y = 0;
		character.useGravity = false;
		character.stopMoving();
		player.delayETank();
		player.delayLTank();
		character.isBurnState = true;
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		if (!character.ownedByLocalPlayer) return;
		character.burnInvulnTime = 120;
		character.burnStunStacks = 0;
		character.useGravity = true;
		player.delayETank();
		player.delayLTank();
		character.isBurnState = false;
	}

	public override void update() {
		base.update();
		if (!character.ownedByLocalPlayer) return;

		if (burnMoveSpeed != 0) {
			burnMoveSpeed = Helpers.toZero(burnMoveSpeed, 400 * Global.spf, burnDir);
			character.move(new Point(burnMoveSpeed, -character.getJumpPower() * 0.125f));
		}

		if (burnDamageCooldown > 0) burnDamageCooldown--;
        if (burnDamageCooldown <= 0) {
            character.applyDamage(1, attacker, character, (int)RockWeaponIds.ScorchWheel, (int)RockProjIds.ScorchWheelBurn);
            Global.playSound("hurt");
            burnDamageCooldown = 45;
        }

		burningTime--;
		if (burningTime <= 0) {
			burningTime = 0;
			character.changeToIdleOrFall();
		}
	}
}
