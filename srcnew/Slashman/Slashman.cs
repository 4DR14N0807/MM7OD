using System;
using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;

public class Slashman : Character {
	public int skidDir = 0;
	public float runSpeed = 0.5f;

	public Slashman(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer,
		bool isWarpIn = true
	) : base(
		player, x, y, xDir, isVisible, netId, ownedByLocalPlayer, isWarpIn
	) {
		charId = CharIds.Slashman;
		charge1Time = 50;
		maxHealth -= 18;
	}

	public override void preUpdate() {
		base.preUpdate();
		//Non-local players end here.
		if (!ownedByLocalPlayer) return;

		int ixDir = player.input.getXDir(player);
		int mdDir = MathF.Sign(moveDelta.x);
		if (MathF.Abs(moveDelta.x) > 0.1f && ixDir == mdDir) {
			skidDir = 0;
			float mul = grounded ? 1 : 0.5f;
			runSpeed += SMPhysics.decel * mul;
			if (runSpeed > SMPhysics.maxSprint) { runSpeed = SMPhysics.maxSprint; }
		}
		else if (runSpeed > SMPhysics.minWalk) {
			if (ixDir != 0 && ixDir != mdDir) {
				runSpeed -= grounded ? SMPhysics.skidWalk : SMPhysics.accel;
				skidDir = ixDir;
			} else {
				runSpeed -= grounded ? SMPhysics.accel : SMPhysics.decel;
			}
			if (runSpeed < SMPhysics.minWalk) {
				runSpeed = SMPhysics.minWalk;
				if (skidDir != 0) {
					xDir = skidDir;
				}
				skidDir = 0;
			}
		}
		//Global.level.gameMode.setHUDDebugWarning(runSpeed.ToString());
	}

	public override void update() {
		base.update();
		//Non-local players end here.
		if (!ownedByLocalPlayer) return;
	}


	public override bool canCrouch() => false;
	public override bool canDash() => false;
	public override bool canWallClimb() => false;
	public override CharState getRunState(bool skipInto = false) => new SlashmanRun();
	public override bool canTurn() => MathF.Abs(runSpeed) < 0.5f;
	public override float getRunSpeed() => runSpeed * getRunDebuffs();
	public override float getJumpPower() {
		return (SMPhysics.baseJump + (runSpeed * 5 * SMPhysics.jumpMul)) * 60;
	}

	public override void airMove() {
		int xDpadDir = player.input.getXDir(player);
		if (!canTurn()) {
			xDpadDir = xDir;
		}
		bool wallKickMove = (wallKickTimer > 0);
		if (wallKickMove) {
			if (wallKickDir == xDpadDir || vel.y > 0) {
				wallKickMove = false;
				wallKickTimer = 0;
			} else {
				float kickSpeed;
				if (isDashing) {
					kickSpeed = 200 * (wallKickTimer / 12);
				} else {
					kickSpeed = 150 * (wallKickTimer / 12);
				}
				move(new Point(kickSpeed * wallKickDir, 0));
			}
			wallKickTimer -= 1 * Global.speedMul;
		}
		if (!wallKickMove && xDpadDir != 0) {
			Point moveSpeed = new Point();
			if (canMove()) { moveSpeed.x = getDashOrRunSpeed() * xDpadDir; }
			if (canTurn()) { xDir = xDpadDir; }
			if (moveSpeed.x != 0) { movePoint(moveSpeed); }
		}
	}

	public enum MeleeIds {
		None = -1,
		IdleSlash,
	}

	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"slashman_attack" => MeleeIds.IdleSlash,
			_ => MeleeIds.None
		});
	}

	public override Projectile? getMeleeProjById(int id, Point projPos, bool addToLevel = true) {
		return id switch {
			(int)MeleeIds.IdleSlash => new GenericMeleeProj(
				new Weapon(), projPos, ProjIds.SlashmanIdle, player,
				2, Global.halfFlinch,
				addToLevel: addToLevel
			),
			_ => null
		};
	}

	public override bool attackCtrl() {
		return base.attackCtrl();
	}

	public override (float, float) getGlobalColliderSize() {
		return (24, 30);
	}

	public override string getSprite(string spriteName) {
		return "rock_" + spriteName;
	}

	public override (string, int) getBaseHpSprite() {
		return ("hud_health_base", 2);
	}

	public override void render(float x, float y) {
		int ogXDir = xDir;
		if (skidDir != 0) {
			xDir = skidDir;
		}
		base.render(x, y);
		xDir = ogXDir;
	}

	public override List<byte> getCustomActorNetData() {
		// Get base arguments.
		List<byte> customData = base.getCustomActorNetData();
		return customData;
	}

	public override void updateCustomActorNetData(byte[] data) {
		// Update base arguments.
		if (data[0] > 1) { base.updateCustomActorNetData(data); }
		data = data[data[0]..];
	}
}

