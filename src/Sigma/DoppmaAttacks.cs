﻿using System;

namespace MMXOnline;

public class SigmaShieldWeapon : Weapon {
	public static SigmaShieldWeapon netWeapon = new();

	public SigmaShieldWeapon() : base() {
		index = (int)WeaponIds.Sigma3Shield;
		killFeedIndex = 162;
	}
}

public class Sigma3FireWeapon : Weapon {
	public static Sigma3FireWeapon netWeapon = new();

	public Sigma3FireWeapon() : base() {
		index = (int)WeaponIds.Sigma3Fire;
		killFeedIndex = 161;
	}
}

public class SigmaShieldProj : Projectile {
	bool launched;
	public float angleDist = 0;
	public float turnDir = -1;
	float ang;
	bool returned;
	Point additiveVelDir;

	float turnSpeed = 600;
	float additiveVelMaxMag = 400;
	public float maxSpeed = 300;
	float travelDistance;
	float maxTravelDistance;
	bool returnedToOrigin;

	public SigmaShieldProj(
		Point pos, int xDir,
		Player player, ushort netProjId, bool sendRpc = false
	) : base(
		SigmaShieldWeapon.netWeapon, pos, xDir, 0, 3, player, "sigma3_proj_shield",
		Global.halfFlinch, 0.5f, netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.Sigma3Shield;
		setIndestructableProperties();
		isDeflectShield = true;
		isShield = true;
		canBeLocal = false;

		if (sendRpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void update() {
		base.update();
		if (!launched) return;
		if (!ownedByLocalPlayer) return;

		if (!returnedToOrigin && returned && travelDistance > maxTravelDistance) {
			returnedToOrigin = true;
			stopMoving();
		}

		if (returnedToOrigin) {
			if (owner.character != null) {
				moveToPos(owner.character.getCenterPos(), maxSpeed);
			} else {
				destroySelf();
			}
			return;
		}

		float additiveVelMag = additiveVelMaxMag * (1 - (angleDist / 180));
		//float additiveVelMag = (angleDist < 180 ? additiveVelMaxMag : -additiveVelMaxMag);
		Point additiveVel = additiveVelDir.times(additiveVelMag);

		if (!returned) {
			var angInc = turnDir * Global.spf * turnSpeed;
			ang += angInc;
			angleDist += MathF.Abs(angInc);
			if (angleDist > 180) {
				returned = true;
				ang = Helpers.to360(ang);
				maxTravelDistance = travelDistance;
				travelDistance = 0;
			}
		} else {
			var dTo = pos.directionTo(damager.owner.character!.getCenterPos()).normalize();
			var destAngle = MathF.Atan2(dTo.y, dTo.x) * 180 / MathF.PI;

			float distToDest = MathF.Abs(destAngle - ang);
			if (distToDest > 180) {
				if (destAngle < ang) destAngle += 360;
				else ang += 360;
			}

			int dirToDest = MathF.Sign(destAngle - ang);
			if (distToDest > 10) {
				var angInc = dirToDest * Global.spf * turnSpeed;
				ang += angInc;
				angleDist += MathF.Abs(angInc);
			} else {
				ang = destAngle;
			}
		}

		vel.x = additiveVel.x + (Helpers.cosd(ang) * maxSpeed);
		vel.y = additiveVel.y + (Helpers.sind(ang) * maxSpeed);

		travelDistance += vel.magnitude * Global.spf;
	}

	public void launch(Player player) {
		if (!ownedByLocalPlayer) return;

		bool upHeld = player.input.isHeld(Control.Up, player) || player.isAI;
		if (upHeld && !player.input.isXDirHeld(xDir, player)) {
			additiveVelDir = new Point(xDir, -3).normalize();
			ang = xDir == 1 ? 0 : 180;
		} else if (upHeld) {
			additiveVelDir = new Point(xDir, -1).normalize();
			ang = xDir == 1 ? 45 : 135;
		} else {
			additiveVelDir = new Point(xDir, 0);
			ang = 90;
		}
		turnDir = -xDir;

		time = 0;
		launched = true;
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (!ownedByLocalPlayer) return;
		if (other.gameObject is Character chr && chr == owner.character && time > 0.25f) {
			destroySelf();
		}
	}
}

public class SigmaThrowShieldState : CharState {
	SigmaShieldProj? proj;
	public SigmaThrowShieldState() : base("throw") {
	}

	public override void update() {
		base.update();

		if (proj?.destroyed == true) {
			character.changeToIdleOrFall();
			return;
		}

		if (proj == null && character.frameIndex >= 2) {
			proj = new SigmaShieldProj(
				character.getFirstPOIOrDefault(),
				character.xDir, player, player.getNextActorNetId(), sendRpc: true
			);
		}

		if (proj != null && !once) {
			proj.changePos(character.getFirstPOIOrDefault());
		}

		if (character.frameIndex >= 2 && !once) {
			once = true;
			proj?.launch(player);
		}
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		proj?.destroySelf();
	}
}

public class Sigma3FireProj : Projectile {
	int upDownDir;
	bool startedWallCrawl;
	int spriteXDir = 1;
	string spriteName;
	float smokeTime;
	public Sigma3FireProj(
		Point pos, float angle,
		int upDownDir, Player player, ushort netProjId, bool sendRpc = false
	) : base(
		Sigma3FireWeapon.netWeapon, pos, 1, 0, 2, player,
		"sigma3_proj_fire", 0, 0.01f, netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)ProjIds.Sigma3Fire;
		maxTime = 0.75f;
		this.angle = angle;
		this.vel = Point.createFromAngle(angle).times(250);
		this.upDownDir = upDownDir;
		spriteName = sprite.name;

		wallCrawlSpeed = 250;
		wallCrawlUpdateAngle = true;
		canBeLocal = false;
		if (sendRpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void update() {
		base.update();

		bool underwater = isUnderwater();
		if (underwater) {
			smokeTime += Global.spf;
			if (smokeTime > 0.1f) {
				smokeTime = 0;
				new Anim(pos, "torpedo_smoke", xDir, null, true);
			}
		}

		if (!ownedByLocalPlayer) return;

		string spriteNameToChangeTo = underwater ? "sigma3_proj_fire_underwater" : spriteName;
		changeSpriteIfDifferent(spriteNameToChangeTo, true);

		if (startedWallCrawl) {
			updateWallCrawl();
			if (spriteXDir == -1) {
				byteAngle -= 128;
			}
		} else if (upDownDir != 0) {
			if (owner.input.isHeld(Control.Down, owner)) {
				vel.y = 250;
			} else if (owner.input.isHeld(Control.Up, owner)) {
				vel.y = -250;
			} else {
				vel.y = 0;
			}
		}
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		if (Global.level.levelData.wallPathNodes.Count == 0) return;
		if (startedWallCrawl) return;
		if (!ownedByLocalPlayer) return;

		setupWallCrawl(deltaPos);
		stopMoving();
		startedWallCrawl = true;
		changePos(other.getHitPointSafe());

		if (deltaPos.x >= 0) {
			spriteXDir = 1;
			spriteName = "sigma3_proj_fire_ground";
			if (!isUnderwater()) changeSprite(spriteName, true);
		} else {
			spriteXDir = -1;
			spriteName = "sigma3_proj_fire_ground_left";
			if (!isUnderwater()) changeSprite(spriteName, true);
		}
	}
}

public class Sigma3Shoot : CharState {
	bool hasShot;
	public Doppma? doppma;

	public Sigma3Shoot(Point inputDir) : base(getShootSprite(inputDir)) {
		airMove = true;
		canStopJump = true;
		useDashJumpSpeed = true;
		landSprite = "shoot";
		airSprite = "jump_shoot";
		shootSprite = sprite;
	}

	public override void update() {
		base.update();
		Point? shootPOI = character.getFirstPOI();
		if (!hasShot && shootPOI != null && doppma != null) {
			hasShot = true;
			doppma.fireballWeapon.shootCooldown = 0.15f;
			int upDownDir = MathF.Sign(player.input.getInputDir(player).y);
			float ang = character.getShootXDir() == 1 ? 0 : 180;
			if (character.sprite.name.EndsWith("jump_shoot_downdiag")) {
				ang = character.getShootXDir() == 1 ? 45 : 135;
			}
			if (character.sprite.name.EndsWith("jump_shoot_down")) {
				ang = 90;
			}
			if (ang != 0 && ang != 180) {
				upDownDir = 0;
			}
			//character.playSound("sigma3shoot", sendRpc: true);
			new Sigma3FireProj(
				shootPOI.Value, ang, upDownDir, player,
				player.getNextActorNetId(), sendRpc: true
			);
		}
		if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		if (character.grounded && character.vel.y >= 0) {
			sprite = "shoot";
			defaultSprite = sprite;
			shootSprite = sprite;
			character.changeSpriteFromName(sprite, true);
		}
		doppma = character as Doppma ?? throw new NullReferenceException();
	}

	public static string getShootSprite(Point inputDir) {
		if (inputDir.x != 0 && inputDir.y > 0) return "jump_shoot_downdiag";
		else if (inputDir.x == 0 && inputDir.y > 0) return "jump_shoot_down";
		else return "jump_shoot";
	}
}
