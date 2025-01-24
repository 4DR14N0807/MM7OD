using System;
using System.Collections.Generic;

namespace MMXOnline;

public class LightningBolt : Weapon {

	public LightningBolt() : base() {
		index = (int)BassWeaponIds.LightningBolt;
		displayName = "LIGHTNING BOLT";
		maxAmmo = 10;
		ammo = maxAmmo;
		weaponSlotIndex = index;
		weaponBarBaseIndex = index;
		weaponBarIndex = index;
		fireRate = 120;
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Point shootPos = character.getShootPos();
		Player player = character.player;

		character.changeState(new LightningBoltState(), true);
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
	const float spawnYPos = -128;
	Point lightningPos;
	public LightningBoltState() : base("lbolt") {
		superArmor = true;
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		Point startAimPos = new Point(character.pos.x, Global.level.camY + 64);

		character.useGravity = false;
		character.stopMoving();
		character.frameSpeed = 0;
		new Anim(character.pos, "lightning_bolt_anim", character.xDir,
			character.player.getNextActorNetId(), true); 

		aim = new Anim(startAimPos, "lightning_bolt_anim", character.xDir,
			character.player.getNextActorNetId(), true);
		aim.frameSpeed = 0;
		aim.alpha /= 2;

		new LightningBoltSpawn(character.getCenterPos(), character.xDir,
		character.player.getNextActorNetId(), true, true);
	}

	public override void update() {
		base.update();

		if (phase == 0) {
			float moveX = character.player.input.getXDir(character.player) * 180;
			aim?.move(new Point(moveX, 0));

			if (stateFrames >= 45) {
				character.frameSpeed = 1;
				
				float xPos = aim?.pos.x ?? character.pos.x;
				lightningPos = new Point(xPos, character.pos.y + spawnYPos);
				aim?.destroySelf();

				phase = 1;
			}
		}

		if (character.isAnimOver()) {
			if (phase == 1) {
				new LightningBoltProj(lightningPos, character.xDir, character.player,
					character.player.getNextActorNetId(), true);
				character.playSound("lightningbolt", true);
				
				phase = 2;
			}

			if (stateFrames >= 120) character.changeToIdleOrFall();
		}
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);

		character.useGravity = true;
	}
}


public class LightningBoltProj : Projectile {

	float spawnPosY;
	float bodySpriteHeight;
	string bodySprite = "lightning_bolt_body";
	int timeInFrames;

	public LightningBoltProj(
		Point pos, int xDir, Player player,
		ushort? netProjId, bool rpc = false
	) : base(
		LightningBolt.netWeapon, pos, xDir, 0, 4,
		player, "lightning_bolt_proj", Global.halfFlinch, 1,
		netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)BassProjIds.LightningBolt;
		maxTime = 0.5f;
		setIndestructableProperties();
		bodySpriteHeight = new Sprite(bodySprite).animData.frames[0].rect.h();

		spawnPosY = pos.y;
		base.vel.y = 600;
		frameSpeed = 0;

		if (rpc) rpcCreate(pos, player, netProjId, xDir);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new LightningBoltProj(
			arg.pos, arg.xDir, arg.player, arg.netId
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
