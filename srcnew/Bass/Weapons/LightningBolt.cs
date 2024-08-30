using System;
using System.Collections.Generic;

namespace MMXOnline;

public class LightningBolt : Weapon {

	public static LightningBolt netWeapon = new();
	public LightningBolt() : base() {
		index = (int)BassWeaponIds.LightningBolt;
		displayName = "LIGHTNING BOLT";
		weaponSlotIndex = index;
		weaponBarBaseIndex = index;
		weaponBarIndex = index;
		fireRateFrames = 120;
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Point shootPos = character.getShootPos();
		Player player = character.player;

		character.changeState(new LightningBoltState(), true);
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
	int bodySpriteHeight = 24;
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

		spawnPosY = pos.y;
		base.vel.y = 600;
		frameSpeed = 0;
	}

	public override void update() {
		base.update();

		if (timeInFrames >= 20) {
			frameSpeed = 1;
			stopMoving();
		}
		
		if (isAnimOver()) destroySelf();
		
		timeInFrames++;
	}

	public override void render(float x, float y) {
		base.render(x,y);

		int pieces = getPiecesAmount();

		for (int i = 0; i < pieces; i++) {
			int dirX = i % 2 != 0 ? -1 : 1;

			Global.sprites[bodySprite].draw(
				frameIndex, pos.x, pos.y - (i * bodySpriteHeight),
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
