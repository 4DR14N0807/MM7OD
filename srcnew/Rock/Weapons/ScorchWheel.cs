using System;
using System.Collections.Generic;

namespace MMXOnline;

public class ScorchWheel : Weapon {
	public static ScorchWheel netWeapon = new();
	public float ogCooldown = 60;
	public float uwCooldown = 45;
	public ScorchWheel() : base() {
		displayName = "SCORCH WHEEL";
		index = (int)RockWeaponIds.ScorchWheel;
		weaponSlotIndex = (int)RockWeaponSlotIds.ScorchWheel;
		weaponBarBaseIndex = (int)RockWeaponBarIds.ScorchWheel;
		weaponBarIndex = weaponBarBaseIndex;
		killFeedIndex = 0;
		maxAmmo = 16;
		ammo = maxAmmo;
		fireRate = ogCooldown;
		switchCooldown = 45;
		descriptionV2 = [
			[ "A weapon able to burn enemies.\n" + "Hold SHOOT to keep the barrier for longer." ]
		];
	}

	public override void shootRock(Rock rock, params int[] args) {
		base.shootRock(rock, args);
		int chargeLevel = args[0];

		fireRate = rock.isUnderwater() ? uwCooldown : ogCooldown;

		if (rock.charState is LadderClimb lc) {
			rock.changeState(new ShootAltLadder(lc.ladder, this, chargeLevel, rock.isUnderwater()), true);
		} else {
			rock.changeState(new ShootAltRock(this, chargeLevel, rock.isUnderwater()), true);
		}
	}

	public override void getProjs(Rock rock, params int[] args) {

		Player player = rock.player;

		if (rock.isUnderwater()) {
			new UnderwaterScorchWheelSpawn
			(
				rock, rock.getCenterPos(), rock.getShootXDir(), 
				player.getNextActorNetId(true), rpc: true, player
			);
		} else {
			new ScorchWheelSpawn
			(
				rock, this, rock.getCenterPos(), rock.getShootXDir(),
				player.getNextActorNetId(true), rpc: true, player
			);
		}
	}
}


public class ScorchWheelSpawn : Projectile {
	public Rock rock = null!;
	bool hasHeld = true;
	Actor ownChr = null!;
	Player? player;
	Weapon wep = null!;

	public ScorchWheelSpawn(
		Actor owner, Weapon wep, Point pos, int xDir, ushort? netProjId, 
		bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "scorch_wheel_spawn", netProjId, altPlayer
	) {
		projId = (int)RockProjIds.ScorchWheelSpawn;

		if (ownedByLocalPlayer) {
			this.player = altPlayer ?? throw new NullReferenceException();
			this.wep = wep;
			rock = (player.character as Rock ?? throw new NullReferenceException());
			rock.sWellSpawn = this;
		}
		
		useGravity = false;
		maxTime = 1;
		damager.hitCooldown = 6;
		destroyOnHit = false;
		canBeLocal = false;
		ownChr = owner;

		if (rpc) rpcCreate(pos, owner, ownerPlayer, netProjId, xDir);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new ScorchWheelSpawn(
			arg.owner, ScorchWheel.netWeapon, arg.pos, 
			arg.xDir, arg.netId, altPlayer: arg.player
		);
	}

	public override void update() {
		base.update();

		if (!ownedByLocalPlayer) return;

		//code that allow you to keep the wheel arround of you. Ruben: I swear if it doesnt work i kysm 
		//may be used as a base for future fixes
		if (!Options.main.wheelDoubleTap) {
			if (owner.input.isHeld(Control.Shoot, owner)) {
				hasHeld = true;
			} else {
				hasHeld = false;
			}
		} else {
			if (time > 2/60f && owner.input.isPressed(Control.Shoot, owner)) {
				hasHeld = true;
			}
		}
		if (isAnimOver() && hasHeld == true) {
			new ScorchWheelProj(ownChr, wep, pos, xDir, damager.owner.getNextActorNetId(), rpc: true, player);
			destroySelf();
			playSound("scorch_wheel", true, true);
		} else if (isAnimOver()
				&& hasHeld == false
		) {
			destroySelf();
			new ScorchWheelMoveProj(ownChr, pos, xDir, damager.owner.getNextActorNetId(), rpc: true, player);
			playSound("scorch_wheel", true, true);
			wep.shootCooldown = (wep as ScorchWheel)?.ogCooldown ?? 60;
			rock.weaponCooldown = 30;
		}

	}

	public override void postUpdate() {
		base.postUpdate();
		if (!ownedByLocalPlayer) return;
		if (destroyed) return;

		if (rock != null) {
			xDir = rock.getShootXDir();
			changePos(rock.pos.addxy(4 * xDir, -22));
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		if (rock != null && ownedByLocalPlayer) rock.sWellSpawn = null;
	}
}


public class ScorchWheelProj : Projectile {
	float projAngle;
	float secondAngle;
	Rock rock = null!;
	Player? player;
	Point centerPos;
	public List<Sprite> fireballs = new List<Sprite>();
	int radius = 15;
	float holdTime;
	bool hasHeld;
	Actor ownChr = null!;
	Weapon wep = null!;

	public ScorchWheelProj(
		Actor owner, Weapon wep, Point pos, int xDir, ushort? netProjId, 
		bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "scorch_wheel_proj", netProjId, altPlayer) {

		projId = (int)RockProjIds.ScorchWheel;
		destroyOnHit = false;

		if (ownedByLocalPlayer) {
			this.player = ownerPlayer;
			rock = ownerPlayer.character as Rock ?? throw new NullReferenceException();
			if (rock != null) rock.sWell = this;
			this.wep = wep;
		}
		
		canBeLocal = false;

		damager.damage = 1;
		damager.hitCooldown = 30;
		ownChr = owner;

		for (int i = 0; i < 4; i++) {
			Sprite fireball = new Sprite("scorch_wheel_fireball");
			fireballs.Add(fireball);
		}

		if (rpc) rpcCreate(pos, owner, ownerPlayer, netProjId, xDir);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new ScorchWheelProj(
			arg.owner, ScorchWheel.netWeapon, arg.pos, 
			arg.xDir, arg.netId, altPlayer: arg.player
		);
	}

	public override void update() {
		base.update();
		checkUnderwater();
		if (rock == null || destroyed) return;
		if (rock != null) {
			xDir = rock.getShootXDir();
		}
		projAngle += 3 * xDir;
		if (projAngle >= 256 || projAngle < 0) {
			projAngle += 256;
			projAngle %= 256;
		}
		byteAngle = projAngle;
		secondAngle += 16  * xDir;
		if (secondAngle >= 256 || secondAngle < 0) {
			secondAngle += 256;
			secondAngle %= 256;
		}

		if (ownedByLocalPlayer && player != null) {
			if (Options.main.wheelDoubleTap) {
				if (!player.input.isPressed(Control.Shoot, owner)) {
					hasHeld = true;
					holdTime += Global.spf;
				} else {
					hasHeld = false;
					holdTime = 0;
				}
			} else {
				if (player.input.isHeld(Control.Shoot, owner)) {
					hasHeld = true;
					holdTime += Global.spf;
				} else {
					hasHeld = false;
					holdTime = 0;
				}
			}
			if (rock != null) {
				xDir = rock.getShootXDir();
				changePos(rock.pos.addxy(4 * xDir, -22));
			}

			if (rock?.charState is Die) {
				destroySelf();
				return;
			}

			if (!hasHeld || holdTime >= 2 || rock?.currentWeapon is not ScorchWheel) {
				destroySelf();
				new ScorchWheelMoveProj(
					ownChr, pos, xDir, damager.owner.getNextActorNetId(true), rpc: true, player
				);
				playSound("scorch_wheel", true, true);
				wep.shootCooldown = (wep as ScorchWheel)?.ogCooldown ?? 60;
				if (rock != null) rock.weaponCooldown = 30;
				return;
			}
		}
	}


	public override void render(float x, float y) {
		base.render(x, y);
		if (rock != null) {
			centerPos = rock.pos.addxy(4 * rock.xDir, -22);
		} else {
			centerPos = pos;
		}

		//main pieces render
		for (var i = 0; i < 4; i++) {
			float extraAngle = projAngle + i * 64;
			long altZIndex = zIndex;
			if (extraAngle >= 256) extraAngle %= 256;
			float xPlus = Helpers.cosd(extraAngle * 1.40625f) * radius;
			float yPlus = Helpers.sind(extraAngle * 1.40625f) * radius;
			float drawAngle = MathF.Floor(secondAngle / 64f) * 64f;

			if (extraAngle >= 128) {
				altZIndex = ZIndex.Character - 100;
			}

			fireballs[i].draw(
				frameIndex, centerPos.x + xPlus,
				centerPos.y + yPlus, xDir, yDir,
				getRenderEffectSet(), 1, 1, 1, altZIndex,
				angle: Helpers.byteToDegree((byte)drawAngle)
			);
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

	public override void onDestroy() {
		base.onDestroy();
		if (rock != null && ownedByLocalPlayer) rock.sWell = null;
	}
}


public class ScorchWheelMoveProj : Projectile {
	public Rock rock = null!;
	float groundTime;

	public ScorchWheelMoveProj(
		Actor owner, Point pos, int xDir, ushort? netProjId, 
		bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "scorch_wheel_grounded_proj", netProjId, altPlayer
	) {
		projId = (int)RockProjIds.ScorchWheelMove;
		useGravity = true;
		maxTime = 1.25f;
		canBeLocal = false;

		vel.x = 240 * xDir;
		damager.damage = 1;
		damager.hitCooldown = 1;

		if (ownedByLocalPlayer) rock = owner as Rock ?? throw new NullReferenceException();

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netProjId, xDir);
		}

		if (collider != null) {
			//collider.isTrigger = false;
			collider.wallOnly = true;
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new ScorchWheelMoveProj(
			arg.owner, arg.pos, arg.xDir, arg.netId, altPlayer: arg.player
		);
	}

	public override void update() {
		base.update();
		checkUnderwater();

		if (grounded && !isUnderwater()) {
			groundTime += Global.speedMul;
			if (groundTime % 6 == 0) {
				new Anim(pos.addxy(0, 16), "scorch_wheel_trail", xDir, null, true);
				new Anim(pos.addxy(0, 16), "scorch_wheel_particle", xDir, null, false) 
				{ useGravity = true, gravityModifier = 0.5f, vel = new Point(-60 * xDir, -210), ttl = 0.5f };
			}
		} else {
			groundTime = 0;
		}
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		if (!ownedByLocalPlayer) return;
		
		if (other.isSideWallHit()) {
			destroySelf();
			Anim.createGibEffect(
				"scorch_wheel_fireball", pos, ownerPlayer, sendRpc: true,
				pieceOverdive: 4, gibPattern: GibPattern.SemiCircle, blink: true, alpha: 0.5f
			);
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


public class UnderwaterScorchWheelSpawn : Projectile {

	int bubblesAmount;
	int counter = 0;
	int bubblesSmallAmount;
	int counterSmall = 0;
	Rock rock = null!;

	public UnderwaterScorchWheelSpawn(
		Actor owner, Point pos, int xDir, ushort? netProjId, 
		bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "empty", netProjId, altPlayer
	) {
		projId = (int)RockProjIds.ScorchWheelUnderwaterSpawn;
		maxTime = 0.2f;
		destroyOnHit = false;
		setIndestructableProperties();

		if (ownedByLocalPlayer) {
			rock = owner as Rock ?? throw new NullReferenceException();
			if (rock != null) rock.sWellU = this;
		}
		
		damager.damage = 2;
		damager.hitCooldown = 60;

		bubblesAmount = Helpers.randomRange(2, 8);
		bubblesSmallAmount = Helpers.randomRange(2, 8);

		if (rpc) rpcCreate(pos, owner, ownerPlayer, netProjId, xDir);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new UnderwaterScorchWheelSpawn(
			arg.owner, arg.pos, arg.xDir, arg.netId, altPlayer: arg.player
		);
	}

	public override void onStart() {
		base.onStart();
		if (!ownedByLocalPlayer) return;

		for (int i = 0; i < 8; i++) {
			float extraAng = Helpers.randomRange(0, 16);
			new UnderwaterScorchWheelProj(
				rock, pos, xDir, damager.owner.getNextActorNetId(), i, extraAng, true
			);
		}
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
				new BubbleAnim(
					new Point(centerPos.x + xOffset, centerPos.y + yOffset), 
					"bubbles", damager.owner.getNextActorNetId(), sendRpc: true) { vel = new Point(0, -60) };
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
		if (rock != null && ownedByLocalPlayer) rock.sWellU = null!;
	}
}


public class UnderwaterScorchWheelProj : Projectile {

	int phase;
	float ang;
	float extraAng;

	public UnderwaterScorchWheelProj(
		Actor owner, Point pos, int xDir, ushort? netId, int type, 
		float extraAng, bool rpc = false, Player? player = null
	) : base(
		pos, xDir, owner, "scorch_wheel_proj_uw", netId, player
	) {
		projId = (int)RockProjIds.ScorchWheelUnderwaterProj;
		maxTime = 1.5f;
		destroyOnHit = false;

		damager.damage = 2;
		damager.hitCooldown = 60;

		this.extraAng = extraAng;
		if (type % 2 == 0) extraAng *= -1;
		if (extraAng < 0) extraAng += 256;

		ang = (MathF.Floor(type / 2) * 64) + 32 + extraAng;
		vel = Point.createFromByteAngle(ang).times(240);

		if (rpc) rpcCreate(pos, owner, ownerPlayer, netId, xDir, new[] { (byte)type, (byte)extraAng} );
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new UnderwaterScorchWheelProj(
			arg.owner, arg.pos, arg.xDir, arg.netId,
			arg.extraData[0], arg.extraData[1], player: arg.player
		);
	} 

	public override void update() {
		base.update();
		if (!isUnderwater()) {
			destroySelf();
			return;
		} 

		if (phase == 0) {
			vel = vel.subtract(Point.createFromByteAngle(ang).times(960 / 60));
			if (time >= 0.25f) {
				stopMoving();
				changeSprite("scorch_wheel_proj_uw2", true);
				phase = 1;
			}
		}
		else if (vel.y > -180) {
			vel.x += MathF.Round(1 * Helpers.cosb(ang));
			vel.y -= 8;
		}
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
		useGravity = false;
	}

	public override bool canEnter(Character character) {
		if (!base.canEnter(character) ||
			character.isStunImmune() ||
			character.isInvulnerable() ||
			character.charState.stunImmune ||
			character.grabInvulnTime > 0 ||
			character.charState.invincible
		) {
			return false;
		}
		return true;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		if (!character.ownedByLocalPlayer) return;
		if (character.vel.y < 0) character.vel.y = 0;
		character.stopMoving();
		if (character.bigBubble != null) character.bigBubble.destroySelf();
		character.isBurnState = true;
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		if (!character.ownedByLocalPlayer) return;
		character.burnStunStacks = 0;
		character.isBurnState = false;
	}

	public override void update() {
		base.update();
		if (!character.ownedByLocalPlayer) return;

		if (burnMoveSpeed != 0) {
			burnMoveSpeed = Helpers.toZero(burnMoveSpeed, 400 * Global.spf, burnDir);
			character.move(new Point(burnMoveSpeed, 0));
		}

		if (burnDamageCooldown > 0) burnDamageCooldown--;
        if (burnDamageCooldown <= 0) {
            character.applyDamage(
				1, attacker, character, (int)RockWeaponIds.ScorchWheel, (int)RockProjIds.ScorchWheelBurn
			);
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
