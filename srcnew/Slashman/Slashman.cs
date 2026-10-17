using System;
using System.Collections.Generic;
using System.Linq;
using static MMXOnline.GameMode;

namespace MMXOnline;

public class Slashman : Character {
	public int skidDir = 0;
	public float runSpeed = 0.5f;
	public float dustTimer = 0;
	public int bounces = 0;
	public decimal rageAmmo = 0;
	public decimal rageMaxAmmo = 20;
	public bool onRage;
	public float rageTimer = 0;
	public float rageMaxTimer = 9;
	public Anim? rageAnim;
	public float diveTime;
	public float[] diveAfterimageTimes = new float[] { 1, 4, 8 };
	public bool isBlocking;

	public Slashman(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer,
		bool isWarpIn = true
	) : base(
		player, x, y, xDir, isVisible, netId, ownedByLocalPlayer, isWarpIn
	) {
		charId = CharIds.Slashman;
		charge1Time = 50;
		maxHealth = 18;
		checkWallClimb = true;
	}

	public override void preUpdate() {
		base.preUpdate();
		//Non-local players end here.
		if (!ownedByLocalPlayer) return;

		Helpers.decrementFrames(ref dustTimer);

		if (charState is SlashMDive) {
			diveTime += Global.speedMul;
		} else {
			if (diveTime >= diveAfterimageTimes[1]) {
				diveTime = diveAfterimageTimes[1] - 1;
			}
			Helpers.decrementFrames(ref diveTime);
		}

		if (onRage) {
			if (rageAnim == null) {
				rageAnim = new Anim(
					getCenterPos(), getSprite("rageanim"), xDir, player.getNextActorNetId(), 
					false, ownedByLocalPlayer, host: this
				);
			}
			Helpers.decrementFrames(ref rageTimer);
			if (rageTimer <= 0) {
				rageAmmo--;
				rageTimer = rageMaxTimer;
			}
			if (rageAmmo <= 0) {
				onRage = false;
			}
		} else {
			rageAnim?.destroySelf();
			rageAnim = null;
		}

		// Directions.
		int ixDir = player.input.getXDir(player);
		int mdDir = MathF.Sign(moveDelta.x);
		// Wallslide gives a speed boost.
		if (charState is SlashMWallSlide or SlashMWallKick && runSpeed < SMPhysics.maxRun) {
			runSpeed = SMPhysics.maxRun;
		}
		// Aceleration.
		if ((MathF.Abs(moveDelta.x) > 0.1f && ixDir == mdDir)) {
			skidDir = 0;
			float mul = grounded ? 1 : 0.5f;
			runSpeed += SMPhysics.decel * mul;
			if (runSpeed > SMPhysics.maxSprint) { runSpeed = SMPhysics.maxSprint; }
		}
		// De-aceleration and Skid.
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
	}

	public override void update() {
		base.update();
		//Non-local players end here.
		if (!ownedByLocalPlayer) return;

		spawnDust();
	}


	public override bool canCrouch() => false;
	public override bool canDash() => false;
	public override bool canWallClimb() => base.canWallClimb();
	public override CharState getRunState(bool skipInto = false) => new SlashMRun();
	public override CharState getWallSlideState(int wallDir, Collider wallCollider) {
		return new SlashMWallSlide(wallDir, wallCollider);
	}
	public override bool inWallSlideState() {
		return charState is SlashMWallSlide;
	}
	public override CharState getWallKickState() {
		return new SlashMWallKick();
	}

	// Sonic physics start here.
	public override bool canTurn() {
		if (inWallSlideState()) return false;

		return MathF.Abs(runSpeed) < 0.5f;
	} 
	public override float getRunSpeed() {
		if (charState is SlashMWallKick) return base.getRunSpeed();
		return runSpeed * getRunDebuffs();
	} 
	public override float getJumpPower() {
		return (SMPhysics.baseJump + (runSpeed * 5 * SMPhysics.jumpMul)) * 60;
	}
	public override void landingCode(bool useSound = true) {
		base.landingCode(useSound);

		bounces = 0;
	}
	public override int getShootXDir() {
		if (skidDir != 0) {
			return skidDir;
		}
		return base.getShootXDir();
	}

	public override Point getPoiDir() {
		int dir = skidDir != 0 ? skidDir : xDir;
		return new Point(dir * xScale, yDirMod() * yScale);
	}
	public override void airMove() {
		int xDpadDir = player.input.getXDir(player);
		if (!canTurn()) {
			xDpadDir = xDir;
		}
		bool wallKickMove = (wallKickTimer > 0 && player.input.getXDir(player) != 0);
		if (wallKickMove) {
			if (wallKickDir == xDpadDir || vel.y > 0) {
				wallKickTimer = 0;
			} else {
				float kickSpeed;
				if (isDashing) {
					kickSpeed = runSpeed * 60 * (wallKickTimer / (maxWallKickTime + 10));
				} else {
					kickSpeed = runSpeed * 60 * (wallKickTimer / (maxWallKickTime + 10));
				}
				move(new Point(kickSpeed * wallKickDir, 0));
			}
			wallKickTimer -= 1 * Global.speedMul;
		}
		if (!wallKickMove && xDpadDir != 0) {
			Point moveSpeed = new Point();
			if (canMove()) { moveSpeed.x = getRunSpeed() * xDpadDir; }
			if (canTurn() && !inWallSlideState()) { xDir = xDpadDir; }
			if (moveSpeed.x != 0) { movePoint(moveSpeed); }
		}
	}
	// Sonic physics end here.

	public override void onDamageDeal(decimal ogDamage, decimal fDamage) {
		base.onDamageDeal(ogDamage, fDamage);

		addRageAmmo(ogDamage);
	}

	public override void onDamageTaken(decimal ogDamage, decimal fDamage) {
		base.onDamageTaken(ogDamage, fDamage);

		addRageAmmo(ogDamage / 2);
	}

	public void addRageAmmo(decimal amount) {
		if (onRage) return;

		rageAmmo += amount;
		if (rageAmmo > rageMaxAmmo) {
			rageAmmo = rageMaxAmmo;
		}
	}

	public void setRageMode() {
		if (onRage) return;

		onRage = true;
		rageTimer = rageMaxTimer;
		playSound("CloudMCharge1", sendRpc: ownedByLocalPlayer);
	}

	void spawnDust() {
		if (dustTimer > 0) return;

		if (grounded && (runSpeed >= SMPhysics.maxSprint || sprite.name.Contains("land"))) {
			new Anim(pos, "dust", xDir, player.getNextActorNetId(), true, ownedByLocalPlayer)
			{ xPushVel = runSpeed * -xDir, useGravity = true, gravityModifier = -0.5f, yDir = -1 };
		}
		dustTimer = 6;
	}

	bool canDiveAttack() {
		return (
			charState is not SlashMDiveStart && 
			charState is not SlashMDive
		);
	}

	public enum MeleeIds {
		None = -1,
		IdleSlash,
		Spin,
		Dive,
		Block,
	}

	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"slashman_attack" => MeleeIds.IdleSlash,
			"slashman_spin" => MeleeIds.Spin,
			"slashman_dive" or "slashman_dive2" => MeleeIds.Dive,
			"slashman_block" or "slashman_block_air" => MeleeIds.Block,
			_ => MeleeIds.None
		});
	}

	public override Projectile? getMeleeProjById(int id, Point projPos, bool addToLevel = true) {
		return id switch {
			(int)MeleeIds.IdleSlash => new SlashMClawMelee(
				projPos, player, onRage, addToLevel
			),
			(int)MeleeIds.Spin => new GenericMeleeProj(
				new Weapon(), projPos, ProjIds.SlashMSpin, player,
				2, Global.halfFlinch, 20, addToLevel: addToLevel
			),
			(int)MeleeIds.Dive => new SlashMDiveMelee(
				projPos, player, onRage, addToLevel
			),
			/* (int)MeleeIds.Block => new GenericMeleeProj(
				new Weapon(), projPos, ProjIds.SlashMBlock, player, 0, 0, 0,
				isShield: true, addToLevel: addToLevel
			) {
				highPiority = true
			}, */
			_ => null
		};
	}

	public override bool normalCtrl() {
		bool altRPressed = player.input.isPressed(Control.WeaponRight, player);
		bool blockPressed = player.input.isPressed(Control.WeaponLeft, player);

		if (altRPressed && rageAmmo >= 1) {
			setRageMode();
		}

		if (blockPressed) {
			changeState(new SlashMBlock(), false);
		}

		return base.normalCtrl();
	}

	public override bool attackCtrl() {
		bool attackPressed = player.input.isPressed(Control.Shoot, player);
		bool specialPressed = player.input.isPressed(Control.Special1, player);
		bool spinPressed = player.input.isHeld(Control.Dash, player);
		bool downPressed = player.input.isHeld(Control.Down, player);

		//Spindash
		if (attackPressed && spinPressed && grounded && charState is not SlashMSpinAttack) {
			changeState(new SlashMSpinAttack(runSpeed), false);
			return true;
		}

		//Dive
		if (attackPressed && downPressed && canDiveAttack()) {
			if (!inWallSlideState() && charState is not SlashMFlip) {
				changeState(new SlashMDiveStart(grounded), false);
			} else {
				//xDir *= -1;
				changeState(new SlashMDive(), false);
			}
			return true;
		}

		// Basic Attacks
		if ((attackPressed || specialPressed) && charState is not SlashMSlash) {
			if (charState is LadderClimb lc) {
				int inputXDir = player.input.getXDir(player);
				if (inputXDir != 0) {
					xDir = inputXDir;
				}
				changeState(new SlashMLadderSlash(lc.ladder, specialPressed), false);
				return true;
			}
			else if (inWallSlideState()) {
				changeState(new SlashMWallSlash(specialPressed), false);
				return true;
			} else {
				changeState(new SlashMSlash(specialPressed), false);
				return true;
			}
		}
		return base.attackCtrl();
	}

	public override (float, float) getGlobalColliderSize() {
		return (24, 30);
	}

	public override string getSprite(string spriteName) {
		return "slashman_" + spriteName;
	}

	public override (string, int) getBaseHpSprite() {
		return ("hud_health_base", 2);
	}

	public override void render(float x, float y) {
		int ogXDir = xDir;
		if (skidDir != 0 && !inWallSlideState() && charState is not SlashMDive) {
			xDir = skidDir;
		}
		base.render(x, y);
		xDir = ogXDir;

		for (int i = 0; i < diveAfterimageTimes.Length; i++) {
			if (diveTime >= diveAfterimageTimes[i]) {
				if (i > 0 && charState is not SlashMDive) {
					continue;
				}
				Point aPos = pos.subtract(deltaPos.times(diveAfterimageTimes[i]));
				Global.sprites[sprite.name].draw(
				frameIndex, aPos.x, aPos.y, xDir, yDir, null, alpha / (i + 1),
				xScale, yScale, zIndex - 2, getShaders(),
				useFrameOffsets: true
			);
			}
		}
	}

	public override void renderAmmo(Point offset, HUDHealthPosition position, Weapon? weaponOverride = null) {
		Point hudHealthPosition = getHUDHealthPosition(position, false);
		float baseX = hudHealthPosition.x + offset.x;
		float baseY = hudHealthPosition.y + offset.y;

		GameMode.renderAmmo(
			baseX, baseY, -2, 0, MathF.Floor((float)rageAmmo),
			maxAmmo: (float)rageMaxAmmo, barSprite: "hud_weapon_full_blues"
		);
	}

	public override List<ShaderWrapper> getShaders() {
		List<ShaderWrapper> shaders = new();
		List<ShaderWrapper> baseShaders = base.getShaders();
		ShaderWrapper? palette = null;

		palette = player.slashManShader;
		palette?.SetUniform("palette", onRage ? 1 : 0);

		if (palette != null && Global.frameCount % 6 is 1 or 2 or 3) {
			shaders.Add(palette);
		}
		shaders.AddRange(baseShaders);

		return shaders;
	}

	public override List<byte> getCustomActorNetData() {
		// Get base arguments.
		List<byte> customData = base.getCustomActorNetData();
		bool[] flags = [
			charState is SlashMBlock
		];
		customData.Add(Helpers.boolArrayToByte(flags));
		return customData;
	}

	public override void updateCustomActorNetData(byte[] data) {
		// Update base arguments.
		base.updateCustomActorNetData(data);
		data = data[data[0]..];

		// Per-character data.
		bool[] flags = Helpers.byteToBoolArray(data[0]);
		isBlocking = flags[0];
		
	}
}

