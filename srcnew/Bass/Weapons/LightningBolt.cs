using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace MMXOnline;

public class LightningBolt : Weapon {
	public static LightningBolt netWeapon = new();
	public static float cooldown = 45;

	public LightningBolt() : base() {
		iconSprite = "hud_weapon_icon_bass";
		index = (int)BassWeaponIds.LightningBolt;
		displayName = "LIGHTNING BOLT";
		maxAmmo = 14;
		ammo = maxAmmo;
		weaponSlotIndex = index;
		weaponBarBaseIndex = index;
		weaponBarIndex = index;
		fireRate = 60;
		switchCooldown = 45;
		hasCustomAnim = true;
		descriptionV2 = [
			[ "Powerfull attack able to pierce defenses.\n" + 
			"Press LEFT or RIGTH to aim.\n" + 
			"Press SHOOT again for a faster but weaker attack."],
		];
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);

		character.changeState(new LBoltBassCharge(), true);
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 0;
	}
}

public class LightningBoltSpawn : Anim {
	int pieces;
	float spriteHeight;
	Sprite spr;

	public LightningBoltSpawn(
		Point pos, int xDir, ushort? netId, bool destroyOnEnd, bool rpc = false
	) : base(
		pos, "lightning_bolt_body", xDir, netId, destroyOnEnd, rpc
	) {
		zIndex = ZIndex.Character - 10;
		spr = new Sprite(base.sprite.name);
		spriteHeight = spr.animData.frames[0].rect.h();
	}

	public override void render(float x, float y) {
		base.render(x,y);

		for (int i = 1; i < 4; i ++) {
			int dirX = i % 2 != 0 ? xDir * -1 : xDir;
			float offset = dirX != xDir ? -2 * xDir : 0;

			Global.sprites[sprite.name].draw(
				frameIndex, pos.x + offset, pos.y - (i * spriteHeight),
				dirX, 1, null, 1, 1, 1, zIndex
			);
		}
	}
}


public class LBoltBassCharge : BassState {
	private float minTime = 8;
	private float maxTime = 60;
	private Anim? anim;
	public Anim? aim;
	public Point? customPos;
	public bool chargeEffect;
	public bool chargeEffect2;

	public LBoltBassCharge(Point? customPos = null) : base("lbolt_charge") {
		enterSound = "lightningbolt";
		useGravity = false;
		this.customPos = customPos;
	}

	public override void update() {
		base.update();

		if (aim != null) {
			float moveX = character.player.input.getXDir(character.player) * 4f;
			float moveY = character.player.input.getYDir(character.player) * 4f;
			if (moveX != 0 || moveY != 0) {
				aim.moveXY(moveX, moveY);
			}
			aim.incPos(bass.deltaPos);

			if (aim.pos.x < bass.pos.x - 256) { aim.changePosX(bass.pos.x - 256); }
			if (aim.pos.x > bass.pos.x + 256) { aim.changePosX(bass.pos.x + 256); }
			if (aim.pos.y < bass.pos.y - 160) { aim.changePosY(bass.pos.y - 160); }
			if (aim.pos.y > bass.pos.y + 160) { aim.changePosY(bass.pos.y + 160); }

			int newDir = Math.Sign(aim.pos.x - bass.pos.x);
			if (newDir != 0) { bass.xDir = newDir; }
		}

		if (!chargeEffect && stateFrames >= 28) {
			chargeEffect = true;
			new LightningBoltSpawn(
				character.getCenterPos(), character.xDir,
				character.player.getNextActorNetId(), true, true
			).host = bass;;
			anim = new Anim(
				character.pos.addxy(3 * character.xDir, -23),
				"lightning_bolt_anim", character.xDir,
				character.player.getNextActorNetId(), true, sendRpc: true, host: bass
			);
			bass.playSound("lightningbolt", sendRpc: true);
		}
		if (!chargeEffect2 && stateFrames >= 56) {
			chargeEffect2 = true;
			new LightningBoltSpawn(
				character.getCenterPos(), character.xDir,
				character.player.getNextActorNetId(), true, true
			).host = bass;
		}

		if (stateFrames >= maxTime ||
			stateFrames >= minTime && player.input.isPressed(Control.Shoot, player)
		) {
			int soundDelay = MathInt.Floor(stateFrames);
			if (stateFrames > 28) {
				soundDelay -= 28;
			}
			bass.changeState(new LBoltBassShoot(
				MathInt.Floor(stateFrames / 28f),
				aim?.pos ?? bass.pos,
				soundDelay >= 8
			));
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		Point startAimPos = customPos ?? new Point(character.pos.x + 5 * character.xDir, character.pos.y - 25);

		if (character.vel.y < 0) {
			character.yPushVel = character.vel.y / 2f / 60;
		}
		else if (character.grounded) {
			character.grounded = false;
			character.yPushVel = -2f;
		}
		else if (character.vel.y / 2f < 2 * 60) {
			character.yPushVel = character.vel.y / 2f / 60;
		}
		else {
			character.yPushVel = 2;
		}
		if (oldState is Dash || oldState.wasDashing) {
			character.xPushVel += character.xDir * character.getDashSpeed();
		}
		else if (oldState.normalCtrl && player.input.getXDir(player) == character.xDir) {
			character.xPushVel += character.xDir * character.getRunSpeed();
		}

		character.stopMoving();
		anim = new Anim(
			character.pos.addxy(3 * character.xDir, -23),
			"lightning_bolt_anim", character.xDir,
			character.player.getNextActorNetId(), true, sendRpc: true, host: bass
		);
		aim = new Anim(
			startAimPos, "lightning_bolt_aim", character.xDir,
			character.player.getNextActorNetId(), false, zIndex: ZIndex.HUD
		);
		aim.alpha /= 2;
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		aim?.destroySelf();
	}
}

public class LBoltBassShoot : BassState {
	public int chargeLevel;
	public Point shootPos;
	public bool shotPressed;
	private Weapon? weapon;
	private bool playSound;

	public LBoltBassShoot(int chargeLevel, Point shootPos, bool playSound = true) : base("lbolt") {
		this.chargeLevel = chargeLevel;
		this.shootPos = shootPos;
		this.playSound = playSound;

		useGravity = false;
	}

	public override void update() {
		base.update();

		if (!shotPressed && weapon?.ammo > 0 && player.input.isPressed(Control.Shoot, player) && stateFrames >= 20) {
			shotPressed = true;
		}
		if (shotPressed && stateFrames >= 45 && bass.currentWeapon is LightningBolt) {
			character.changeState(new LBoltBassCharge(shootPos), true);
			return;
		}
		if (stateFrames >= 45) {
			bass.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		if (playSound) {
			character.playSound("lightningbolt", true);
		}
		new LightningBoltProj(
			character, shootPos.addxy(0, -112), character.xDir, 
			character.player.getNextActorNetId(), 2 + chargeLevel, sendRpc: true
		);
		new Anim(
			character.pos.addxy(3 * character.xDir, -23),
			"lightning_bolt_anim", character.xDir,
			character.player.getNextActorNetId(), true, sendRpc: true, host: bass
		);

		weapon = character.weapons.FirstOrDefault(w => w is LightningBolt { ammo: >0 });
		if (weapon != null) {
			weapon.addAmmo(-1, player);
			weapon.shootCooldown = LightningBolt.cooldown;
		}
	}
}

public class LightningBoltProj : Projectile {
	float spawnPosY;
	float mainProjHeight;
	float bodySpriteHeight;
	Sprite bodySprite = new("lightning_bolt_body");
	int timeInFrames;
	bool once;

	public LightningBoltProj(
		Actor owner, Point pos, int xDir, ushort? netProjId, 
		float overrideDamage = 0, bool sendRpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "lightning_bolt_bottom", netProjId, altPlayer
	) {
		projId = (int)BassProjIds.LightningBolt;
		maxTime = 0.5f;
		setIndestructableProperties();
		bodySpriteHeight = bodySprite.animData.hitboxes[0].shape.getRect().h();
		mainProjHeight = sprite.animData.frames[0].hitboxes[0].shape.getRect().h();

		damager.damage = overrideDamage > 0 ? overrideDamage : 3;
		damager.flinch = Global.halfFlinch;
		damager.hitCooldown = 60;

		spawnPosY = pos.y;
		vel.y = 600;
		frameSpeed = 0;

		if (sendRpc) rpcCreate(pos, owner, ownerPlayer, netProjId, xDir, (byte)overrideDamage);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new LightningBoltProj(
			arg.owner, arg.pos, arg.xDir, arg.netId, arg.extraData[0], altPlayer: arg.player
		);
	}

	public override void update() {
		base.update();

		if (timeInFrames >= 20) {
			frameSpeed = 1;
			stopMoving();
		}
		
		if (isAnimOver()) destroySelf();
		
		timeInFrames++;

		if (frameIndex > 0 && !once) {
			releaseSparks();
			once = true;
		}

		if (collider == null) return;
		List<Point> points = new() {
			new Point(collider.shape.getRect().x1, pos.y - getHeight()),
			new Point(collider.shape.getRect().x2, pos.y - getHeight()),
			new Point(collider.shape.getRect().x2, pos.y),
			new Point(collider.shape.getRect().x1, pos.y),
		};
		globalCollider = new Collider(points, true, null!, false, false, 0, Point.zero);
	}

	public override void render(float x, float y) {
		base.render(x,y);

		int pieces = getPiecesAmount();

		for (int i = 0; i < pieces; i++) {
			int dirX = i % 2 != 0 ? xDir * -1 : xDir;
			float offset = dirX != xDir ? -2 * xDir : 0;

			bodySprite.draw(
				frameIndex, pos.x + offset, pos.y - (i * bodySpriteHeight) - mainProjHeight,
				dirX, 1, null, 1, 1, 1, zIndex
			);
		}

	}

	private int getPiecesAmount() {
		float yDistFromOrigin = MathF.Abs(pos.y - spawnPosY);
		int pieces = MathInt.Floor(yDistFromOrigin / bodySpriteHeight);
		if (pieces > 6) pieces = 6;
		return pieces;
	}

	private float getHeight() {
		return (
			(getPiecesAmount() * bodySpriteHeight) + mainProjHeight
		);
	}

	void releaseSparks() {
		int sparks = Helpers.randomRange(4, 8);
		for (int i = 0; i < sparks; i++) {
			int leftOrRight = i % 2 == 0 ? -1 : 1;
			float space = getHeight() / sparks;
			int[] possiblePositions = new int[] {8, 16, 32};
			int position = Helpers.randomRange(0, possiblePositions.Length - 1);

			float sPosX = possiblePositions[position] * leftOrRight;
			float sPosY = (space * i) + (space / 2);
			Point sparkPos = pos.addxy(sPosX, -sPosY);

			float sVelX = Helpers.randomRange(60, 90) * leftOrRight;
			if (position == 2) sVelX *= -1;
			float sVelY = Helpers.randomRange(60, 90);
			if (i % 2 == 0) sVelY *= -1;

			new Anim(sparkPos, "lightning_bolt_spark", Math.Sign(sVelX), null, true) 
			{ xPushVel = sVelX / 60, yPushVel = sVelY / 60 };
		}
	}
}
