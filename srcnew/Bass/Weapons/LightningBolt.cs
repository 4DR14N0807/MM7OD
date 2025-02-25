using System;
using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;

public class LightningBolt : Weapon {
	public static LightningBolt netWeapon = new();
	public static float cooldown = 45;

	public LightningBolt() : base() {
		index = (int)BassWeaponIds.LightningBolt;
		displayName = "LIGHTNING BOLT";
		maxAmmo = 10;
		ammo = maxAmmo;
		weaponSlotIndex = index;
		weaponBarBaseIndex = index;
		weaponBarIndex = index;
		fireRate = cooldown;
		hasCustomAnim = true;
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Point shootPos = character.getShootPos();
		Player player = character.player;

		character.changeState(new LightningBoltState(), true);
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

public class LightningBoltState : CharState {
	int phase = 0;
	Anim? aim;
	Anim? anim;
	Point animOffset;
	const float spawnYPos = -128;
	Point lightningPos;
	float endLagFrames = 0;
	float overrideDamage;
	Bass bass = null!;
	float minTime = 6;
	float maxTime = 60;
	float chargeTime = 30;

	public LightningBoltState() : base("lbolt") {
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		Point startAimPos = new Point(character.pos.x + 5 * character.xDir, character.pos.y - 25);
		animOffset = new Point(7 * character.xDir, -23);
		bass = character as Bass ?? throw new NullReferenceException();

		character.useGravity = false;
		if (character.vel.y < 0) {
			character.yPushVel = character.vel.y / 2f;
		}
		else if (character.grounded) {
			character.grounded = false;
			character.yPushVel = -2f * 60;
		}
		else if (character.vel.y / 2f < 2 * 60) {
			character.yPushVel = character.vel.y / 2f;
		}
		else {
			character.yPushVel = 2 * 60;
		}
		character.stopMovingWeak();
		character.frameSpeed = 0;
		anim = new Anim(character.pos.add(animOffset), "lightning_bolt_anim", character.xDir,
			character.player.getNextActorNetId(), true); 

		aim = new Anim(
			startAimPos, "lightning_bolt_anim", character.xDir,
			character.player.getNextActorNetId(), true
		);
		aim.frameSpeed = 0;
		aim.alpha /= 2;

		charge();
	}

	public override void update() {
		base.update();

		anim?.changePos(character.pos.add(animOffset));

		if (phase == 0) {
			float moveX = character.player.input.getXDir(character.player) * 180;
			aim?.move(new Point(moveX, 0));
			if (aim != null) {
				aim.pos.y = character.pos.y - 25;
			}

			if (stateFrames % chargeTime == 0 && stateFrames >= minTime && stateFrames != maxTime) charge();

			if (stateFrames >= maxTime || stateFrames >= minTime && player.input.isPressed(Control.Shoot, player)) {
				character.frameSpeed = 1;

				float xPos = aim?.pos.x ?? character.pos.x;
				lightningPos = new Point(xPos, character.pos.y + spawnYPos);
				aim?.destroySelf();
				overrideDamage = MathF.Floor((stateFrames / chargeTime)) + 2;

				phase = 1;
			}
		}

		if (phase == 1) {
			new LightningBoltProj(
				character, lightningPos, character.xDir, 
				character.player.getNextActorNetId(), overrideDamage, true
			);
			character.playSound("lightningbolt", true);
			Weapon? bolt = character.weapons.FirstOrDefault(w => w is LightningBolt { ammo: >0 });
			if (bolt != null) {
				bolt.addAmmo(-1, player);
				once = true;
			}
			phase = 2;
		}

		if (phase == 2) {
			endLagFrames += character.speedMul;
		}

		if (endLagFrames >= 45) {
			character.changeToIdleOrFall();
		}
		
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
		character.stopMovingWeak();
		character.useGravity = true;
		aim?.destroySelf();
		bass.weaponCooldown = LightningBolt.cooldown;

	}

	void charge() {
		new LightningBoltSpawn(
			character.getCenterPos(), character.xDir,
			character.player.getNextActorNetId(), true, true
		);

		anim = new Anim(character.pos.add(animOffset), "lightning_bolt_anim", character.xDir,
			character.player.getNextActorNetId(), true);

		character.playSound("lightningbolt", true);
	}
}


public class LightningBoltProj : Projectile {
	float spawnPosY;
	float bodySpriteHeight;
	string bodySprite = "lightning_bolt_body";
	int timeInFrames;

	public LightningBoltProj(
		Actor owner, Point pos, int xDir, ushort? netProjId, 
		float? overrideDamage = 0, bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "lightning_bolt_proj", netProjId, altPlayer
	) {
		projId = (int)BassProjIds.LightningBolt;
		maxTime = 0.5f;
		setIndestructableProperties();
		bodySpriteHeight = new Sprite(bodySprite).animData.frames[0].rect.h();

		damager.damage = overrideDamage ?? 2;
		damager.flinch = Global.halfFlinch;
		damager.hitCooldown = 60;

		spawnPosY = pos.y;
		base.vel.y = 600;
		frameSpeed = 0;

		if (rpc) rpcCreate(pos, owner, ownerPlayer, netProjId, xDir);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new LightningBoltProj(
			arg.owner, arg.pos, arg.xDir, arg.netId, altPlayer: arg.player
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

		Rect rect = collider._shape.getRect();
		rect.y1 = -bodySpriteHeight;
		rect.y2 = getPiecesAmount() * bodySpriteHeight;
		collider._shape = rect.getShape();
	}

	public override void render(float x, float y) {
		base.render(x,y);

		int pieces = getPiecesAmount();

		for (int i = 0; i < pieces; i++) {
			int dirX = i % 2 != 0 ? xDir * -1 : xDir;
			float offset = dirX != xDir ? -2 * xDir : 0;

			Global.sprites[bodySprite].draw(
				frameIndex, pos.x + offset, pos.y - (i * bodySpriteHeight),
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
}
