using System;
using System.Collections.Generic;
using System.Linq;


namespace MMXOnline;

public class Rock : Character {

	public float lemonTime;
	public int lemons;
	public JunkShieldProj? junkShield;
	public ScorchWheelSpawn? sWellSpawn;
	public ScorchWheelProj? sWell;
	public UnderwaterScorchWheelProj sWellU;
	public UnderwaterScorchWheelProj? underwaterScorchWheel;
	public Projectile? sWheel;
	public SARocketPunchProj? saRocketPunchProj;
	public bool spawnedSWheelHitbox;
	public bool hasChargedNoiseCrush = false;
	public float noiseCrushAnimTime;
	public bool usedDoubleJump;
	public bool boughtSuperAdaptorOnce;
	public float arrowSlashCooldown;
	public float legBreakerCooldown;
	public float timeSinceLastShoot;
	public bool isSlideColliding;
	public Rush rush;
	public RushWeapon rushWeapon;

	// AI Stuff.
	public float aiWeaponSwitchCooldown = 120;

	public Rock(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer,
		bool isWarpIn = true
	) : base(
		player, x, y, xDir, isVisible, netId, ownedByLocalPlayer, isWarpIn, false, false
	) {
		charId = CharIds.Rock;

		spriteToCollider["burning"] = null;
		spriteToCollider["sa_activate_air"] = null;
		spriteToCollider["sa_activate"] = null;
		spriteToCollider["sa_activate_end"] = null;
		spriteToCollider["sa_activate_end_air"] = null;

		charge2Time = 80;

		foreach (var w in player.weapons) {
			if (w is RushWeapon rw) rushWeapon = rw;
		}
	}

	public override void update() {
		base.update();

		Helpers.decrementTime(ref lemonTime);
		Helpers.decrementTime(ref arrowSlashCooldown);
		Helpers.decrementTime(ref legBreakerCooldown);

		timeSinceLastShoot += Global.spf;
		if (timeSinceLastShoot >= Global.spf * 30) lemons = 0;

		if (player.weapon.ammo >= player.weapon.maxAmmo) {
			weaponHealAmount = 0;
		}

		if (player.weapon is not NoiseCrush) hasChargedNoiseCrush = false;

		//================================UNDERWATER SCORCH WHEEL ==============================================

		if (underwaterScorchWheel != null && !spawnedSWheelHitbox) {
			var center = getCenterPos();
			sWheel = new GenericMeleeProj(new ScorchWheel(), center, ProjIds.ScorchWheelUnderwater, player, 2, 0, 1);

			var rect = new Rect(0, 0, 32, 39);
			sWheel.globalCollider = new Collider(rect.getPoints(), false, sWheel, false, false, 0, new Point());
			spawnedSWheelHitbox = true;
		}

		if (sWheel != null) {
			sWheel.pos = getCenterPos();
			if (!sprite.name.Contains("shoot2") &&
			!sprite.name.Contains("shoot2_air") && !sprite.name.Contains("ladder_shoot2")) {
				sWheel.destroySelfNoEffect();
				spawnedSWheelHitbox = false;
			}
		}

		//======================================================================================================


		if (weaponHealAmount > 0 && player.health > 0) {
			weaponHealTime += Global.spf;
			if (weaponHealTime > 0.05) {
				weaponHealTime = 0;
				weaponHealAmount--;
				player.weapon.ammo = Helpers.clampMax(player.weapon.ammo + 1, player.weapon.maxAmmo);
				playSound("heal", forcePlay: true);
			}
		}

		if (shootAnimTime > 0 && !charState.isGrabbing) {
			shootAnimTime -= Global.spf;
			if (shootAnimTime <= 0) {
				shootAnimTime = 0;
				changeSpriteFromName(charState.sprite, false);
			}
		}

		Point inputDir = player.input.getInputDir(player);

		bool shootPressed = player.input.isPressed(Control.Shoot, player);
		bool shootHeld = player.input.isHeld(Control.Shoot, player);
		bool specialPressed = player.input.isPressed(Control.Special1, player);

		if (specialPressed && rushWeapon != null && rushWeapon.canShoot(getChargeLevel(), player)
			&& Options.main.rushSpecial) {
			
			rushWeapon.shoot(this, 0);
		}

		player.busterWeapon.update();

		var oldWeapon = player.weapon;

		if (shootPressed) {
			lastShootPressed = Global.frameCount;
		}

		int framesSinceLastShootPressed = Global.frameCount - lastShootPressed;
		int framesSinceLastShootReleased = Global.frameCount - lastShootReleased;

		bool offCooldown = oldWeapon.shootTime == 0 && shootTime == 0;

		bool shootCondition = (
			shootPressed ||
			(framesSinceLastShootPressed < Global.normalizeFrames(6) &&
			framesSinceLastShootReleased > Global.normalizeFrames(30)) ||
			(shootHeld && player.weapon.isStream && chargeTime < charge1Time)
		);
		if (offCooldown &&
			shootCondition && canShoot()) {
			shoot(false);
		}

		rockCharge();

		player.changeWeaponControls();
		//changeSprite("rock_" + charState.shootSprite, true);
		chargeGfx();

		if (player.dashPressed(out string slideControl) && canSlide()
			&& charState is not Slide && charState is not RockChargeShotState
			&& charState is not ShootAlt
		) {
			changeState(new Slide(slideControl), true);
		}

		quickAdaptorUpgrade();

		/*if (!grounded && player.hasSuperAdaptor() &&
				player.input.isPressed(Control.Jump, player) &&
				canJump() && flag == null && !usedDoubleJump
			) {
				changeState(new RockDoubleJump(), true);
				usedDoubleJump = true;
			}*/

		if (player.hasSuperAdaptor()) superAdaptorControls();
	}

	public void superAdaptorControls() {
		if (!grounded) {

			if (!usedDoubleJump && canJump() && flag == null && player.input.isPressed(Control.Jump, player) && charState is not LadderClimb) {
				changeState(new RockDoubleJump(), true);
				usedDoubleJump = true;
			}

		} else {
			usedDoubleJump = false;
			bool arrowSlashInput = player.input.checkHadoken(player, xDir, Control.Shoot);

			if (arrowSlashInput && arrowSlashCooldown <= 0 && charState is not LadderClimb) {
				changeState(new SAArrowSlashState(), true);
				arrowSlashCooldown = 90f / 60f;
			}

			bool legBreakerInput = player.input.isHeld(Control.Down, player);

			if (legBreakerInput && legBreakerCooldown <= 0 && canSlide() && player.dashPressed(out string slideControl)) {
				changeState(new LegBreakerState(slideControl), true);
				legBreakerCooldown = 90f / 60f;
			}
		}
	}


	public void quickAdaptorUpgrade() {
		if (!player.input.isHeld(Control.Special2, player)) {
			hyperProgress = 0;
			return;
		}
		if (player.hasSuperAdaptor()) {
			hyperProgress = 0;
			return;
		}
		if (charState is CallDownRush) {
			hyperProgress = 0;
			return;
		}
		if (player.health <= 0) {
			hyperProgress = 0;
			return;
		}
		if (!(charState is WarpIn) && player.canGoSuperAdaptor()) {
			//hyperProgress += Global.spf;

			if (!boughtSuperAdaptorOnce) {
				player.currency -= Player.superAdaptorCost;
				boughtSuperAdaptorOnce = true;
			}
			player.character.changeState(new CallDownRush(), true);

			return;
		}
		if (hyperProgress < 1) {
			return;
		}
		hyperProgress = 0;
		/*if (player.canGoSuperAdaptor()) {
			if (!player.character.boughtSuperAdaptorOnce) {
				player.currency -= Player.superAdaptorCost;
				player.character.boughtSuperAdaptorOnce = true;
			}
			player.character.changeState(new CallDownRush(), true);
			//player.setSuperAdaptor(true);
			return;
		}*/
	}

	public override void render(float x, float y) {
		base.render(x, y);

		if (hasChargedNoiseCrushBS.getValue()) drawChargedNoiseCrush(x, y);
	}

	public void drawChargedNoiseCrush(float x, float y) {
		addRenderEffect(RenderEffectType.ChargePink, 0.05f, 0.1f);
		noiseCrushAnimTime += Global.spf * 20;
		if (noiseCrushAnimTime >= 12) noiseCrushAnimTime = 0;
		float posMove = -4 * noiseCrushAnimTime;
		float sx = pos.x + x;
		float sy = pos.y + posMove;

		var sprite1 = Global.sprites["noise_crush_charge_part"];
		//float distFromCenter = Helpers.randomRange(-16, 16);
		float posOffset = noiseCrushAnimTime * 50;
		int hyperChargeAnimFrame = MathInt.Floor((noiseCrushAnimTime / 12) * sprite1.frames.Count);
		sprite1.draw(hyperChargeAnimFrame, sx + Helpers.randomRange(-16, 16), sy, 1, 1, null, 1, 1, 1, zIndex + 1);
		sprite1.draw(hyperChargeAnimFrame, sx + Helpers.randomRange(-16, 16), sy, 1, 1, null, 1, 1, 1, zIndex + 1);
		sprite1.draw(hyperChargeAnimFrame, sx + Helpers.randomRange(-16, 16), sy, 1, 1, null, 1, 1, 1, zIndex + 1);
		sprite1.draw(hyperChargeAnimFrame, sx + Helpers.randomRange(-16, 16), sy, 1, 1, null, 1, 1, 1, zIndex + 1);
		sprite1.draw(hyperChargeAnimFrame, sx + Helpers.randomRange(-16, 16), sy, 1, 1, null, 1, 1, 1, zIndex + 1);
		sprite1.draw(hyperChargeAnimFrame, sx + Helpers.randomRange(-16, 16), sy, 1, 1, null, 1, 1, 1, zIndex + 1);
		sprite1.draw(hyperChargeAnimFrame, sx + Helpers.randomRange(-16, 16), sy, 1, 1, null, 1, 1, 1, zIndex + 1);
		sprite1.draw(hyperChargeAnimFrame, sx + Helpers.randomRange(-16, 16), sy, 1, 1, null, 1, 1, 1, zIndex + 1);
	}

	public override bool canMove() {
		if (charState is CallDownRush) return false;

		return base.canMove();
	}

	public override bool canJump() {
		if (isSlideColliding) return false;

		return base.canJump();
	}

	public override bool canDash() {
		return false;
	}

	public bool canSlide() {
		if (!grounded) return false;
		if (flag != null) return false;
		if (charState is CallDownRush) return false;
		if (charState is SAArrowSlashState) return false;
		if (charState is LegBreakerState) return false;
		if (charState is Burning) return false;
		if (rootTime > 0) return false;
		return true;
	}

	public override bool canAirDash() {
		return false;
	}

	public override bool canWallClimb() {
		return false;
	}

	public override bool canCrouch() {
		return false;
	}

	public override string getSprite(string spriteName) {
		return "rock_" + spriteName;
	}

	public override bool canShoot() {
		if (sWell != null) return false;
		if (sWellSpawn != null) return false;
		if (sWellU != null) return false;
		if (charState is Slide)
			return (player.weapon is RockBuster || player.weapon is WildCoil) && getChargeLevel() == 2;
		if (charState is CallDownRush) return false;
		if (charState is SAArrowSlashState) return false;
		if (isInvulnerableAttack()) return false;
		if (saRocketPunchProj != null) return false;

		return base.canShoot();
	}


	public override bool canChangeWeapons() {
		return base.canChangeWeapons();
	}

	public void shoot(bool doCharge) {
		int chargeLevel = getChargeLevel();

		if (!doCharge && chargeLevel >= 2) return;

		if (!player.weapon.canShoot(chargeLevel, player)) {
			return;
		}

		shootTime = player.weapon.rateOfFire;
		timeSinceLastShoot = 0;
		player.delayETank();

		if (player.weapon is NoiseCrush) {
			if (hasChargedNoiseCrush) {
				doCharge = true;
				chargeLevel = 2;
			}
		}

		bool hasShootSprite = !string.IsNullOrEmpty(charState.shootSprite);

		if (shootAnimTime == 0) {
			if (hasShootSprite) changeSprite(getSprite(charState.shootSprite), false);
		} else if (charState is Idle) {
			frameIndex = 0;
			frameTime = 0;
		}
		if (charState is LadderClimb) {
			if (player.input.isHeld(Control.Left, player)) {
				this.xDir = -1;
			} else if (player.input.isHeld(Control.Right, player)) {
				this.xDir = 1;
			}

			/* if (player.weapon is SlashClawWeapon || player.weapon is JunkShield || player.weapon is ScorchWheel || player.weapon is WildCoil) {
				changeState(new ShootAltLadder(player.weapon, chargeLevel), true);
			} */
		}

		//Sometimes transitions cause the shoot sprite not to be played immediately, so force it here
		if (currentFrame.getBusterOffset() == null) {
			if (hasShootSprite) changeSprite(getSprite(charState.shootSprite), false);
		}

		if (hasShootSprite) shootAnimTime = 0.3f;
		int xDir = getShootXDir();

		int cl = doCharge ? chargeLevel : 0;

		shootRpc(getShootPos(), player.weapon.index, xDir, cl, player.getNextActorNetId(), true);
	}

	public void shootRpc(Point pos, int weaponIndex, int xDir, int chargeLevel, ushort netProjId, bool sendRpc) {
		// Right before we shoot, change to the current weapon.
		// This ensures that the shoot RPC sent reflects the current weapon used
		if (!player.isAI) {
			player.changeWeaponFromWi(weaponIndex);
		}

		Weapon weapon = player.weapon;

		shoot(weapon, pos, xDir, player, chargeLevel, netProjId);

		if (ownedByLocalPlayer && sendRpc) {
			var playerIdByte = (byte)player.id;
			var xDirByte = (byte)(xDir + 128);
			var chargeLevelByte = (byte)chargeLevel;
			var netProjIdBytes = BitConverter.GetBytes(netProjId);
			var xBytes = BitConverter.GetBytes((short)pos.x);
			var yBytes = BitConverter.GetBytes((short)pos.y);
			var weaponIndexByte = (byte)weapon.index;

			RPC shootRpc = RPC.shoot;

			Global.serverClient?.rpc(shootRpc, playerIdByte, xBytes[0], xBytes[1], yBytes[0], yBytes[1], xDirByte, chargeLevelByte, netProjIdBytes[0], netProjIdBytes[1], weaponIndexByte);
		}
	}

	public void shoot(Weapon weapon, Point pos, int xDir, Player player, int chargeLevel, ushort netProjId) {
		float ammoUsage = 0;
		weapon.getProjectile(pos, xDir, player, chargeLevel, netProjId);
		// Lasto: Esta monda no sirve.
		/* if (weapon.soundTime == 0) {
			if (weapon.shootSounds != null && weapon.shootSounds.Count > 0) {
				playSound(weapon.shootSounds[chargeLevel]);
			}
		} */
		// Only deduct ammo if owned by local player
		if (ownedByLocalPlayer) {
			if (weapon is RockBuster buster || weapon is SARocketPunch rocketPunch) {
				ammoUsage = 0;
			} else {
				ammoUsage = weapon.getAmmoUsage(chargeLevel);
			}
			weapon.addAmmo(-ammoUsage, player);
		}
	}

	public override bool canCharge() {
		Weapon weapon = player.weapon;
		if (flag != null) return false;
		//if (isInvisibleBS.getValue()) return false;
		if (player.weapons.Count == 0) return false;
		if (isWarpIn()) return false;
		if (player.weapons.Count == 0) return false;
		if (invulnTime > 0) return false;
		if (junkShield != null) return false;
		if (sWell != null) return false;

		return base.canCharge();
	}

	public override bool chargeButtonHeld() {
		return player.input.isHeld(Control.Shoot, player);
	}

	public void rockCharge() {
		if (chargeButtonHeld() && canCharge() && (!player.isAI || chargeTime <= charge2Time + 0.4f)) {
			increaseCharge();
		} else {
			if (isCharging()) {
				if (shootTime == 0 && canShoot()) {
					shoot(true);
				}
				stopCharge();
				lastShootReleased = Global.frameCount;
			} else if (!(charState is Hurt)) {
				stopCharge();
			}
		}
	}

	public override List<ShaderWrapper> getShaders() {
		List<ShaderWrapper> baseShaders = base.getShaders();
		List<ShaderWrapper> shaders = new();
		ShaderWrapper? palette = null;

		int index = player.weapon.index;
		palette = player.rockPaletteShader;

		palette?.SetUniform("palette", index);
		palette?.SetUniform("paletteTexture", Global.textures["rock_palette_texture"]);

		if (palette != null) {
			shaders.Add(palette);
		}
		if (shaders.Count == 0) {
			return baseShaders;
		}

		shaders.AddRange(baseShaders);
		return shaders;
	}

	public override bool canAddAmmo() {
		if (player.weapon == null) { return false; }
		bool hasEmptyAmmo = false;
		foreach (Weapon weapon in player.weapons) {
			if (weapon.canHealAmmo && weapon.ammo < weapon.maxAmmo) {
				hasEmptyAmmo = true;
				break;
			}
		}
		return hasEmptyAmmo;
	}

	public override int maxChargeLevel() {
		return 2;
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);

		var wall = other.gameObject as Wall;
		var rush = other.gameObject as Rush;
		var isGHit = other.hitData?.normal != null && other.hitData.normal.Value.isGroundNormal();

		if (charState is RockDoubleJump && wall != null) {
			vel = new Point(RockDoubleJump.jumpSpeedX * xDir, RockDoubleJump.jumpSpeedY);
		}

		if (rush != null && (rush.rushState is RushIdle || rush.rushState is RushSleep ) && 
			isGHit && charState is Fall) {
				
			rush.changeState(new RushCoil());
			vel.y = getJumpPower() * -1.5f;
			changeState(new Jump(), true);
			rushWeapon.addAmmo(-1, player);
		}
	}

	public override Projectile? getProjFromHitbox(Collider hitbox, Point centerPoint) {
		int meleeId = getHitboxMeleeId(hitbox);
		if (meleeId == -1) {
			return null;
		}
		Projectile? proj = getMeleeProjById(meleeId, centerPoint);
		if (proj != null) {
			proj.meleeId = meleeId;
			proj.owningActor = this;
			return proj;
		}
		return null;
	}

	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"rock_slashclaw" => MeleeIds.SlashClaw,
			"rock_slashclaw_air" => MeleeIds.SlashClaw,
			"rock_ladder_slashclaw" => MeleeIds.SlashClaw,
			"rock_sa_legbreaker" => MeleeIds.LegBreaker,
			_ => MeleeIds.None
		});
	}

	public Projectile? getMeleeProjById(int id, Point? pos = null, bool addToLevel = true) {
		Point projPos = pos ?? new Point(0, 0);
		Projectile? proj = id switch {
			(int)MeleeIds.SlashClaw => new GenericMeleeProj(
				new SlashClawWeapon(player), projPos, ProjIds.SlashClaw, player, 3, 0, 0.25f,
				addToLevel: addToLevel

			),
			(int)MeleeIds.LegBreaker => new GenericMeleeProj(
				new LegBreaker(player), projPos, ProjIds.LegBreaker, player, 2, Global.halfFlinch, 0.5f,
				addToLevel: addToLevel
			),

		};
		return proj;

	}

	public enum MeleeIds {
		None = -1,
		SlashClaw,
		UnderWaterScorchWheel,
		LegBreaker,
	}

	public virtual Collider getSlidingCollider() {
		var rect = new Rect(0, 0, 36, 12);
		return new Collider(rect.getPoints(), false, this, false, false, HitboxFlag.Hurtbox, new Point(0, 0));
	}

	public virtual float getSlideSpeed() {
		if (flag != null) {
			return getRunSpeed();
		}
		float dashSpeed = 3.5f * 60;
		return dashSpeed * getRunDebuffs();
	}

	public void removeBusterProjs() {
		sWell = null;
	}
	public override void destroySelf(
		string spriteName = "", string fadeSound = "",
		bool disableRpc = false, bool doRpcEvenIfNotOwned = false,
		bool favorDefenderProjDestroy = false
	) {
		base.destroySelf(spriteName, fadeSound, disableRpc, doRpcEvenIfNotOwned, favorDefenderProjDestroy);

		if (player.hasSuperAdaptor()) {
			player.setSuperAdaptor(false);
		}

		//if (rush != null) rush.destroySelf();
	}

	public override void aiAttack(Actor target) {
		if (AI.trainingBehavior != 0) {
			return;
		}
		if (player.weapon == null) {
			return;
		}
		Helpers.decrementFrames(ref aiWeaponSwitchCooldown);
		if (aiWeaponSwitchCooldown == 0) {
			player.weaponRight();
			aiWeaponSwitchCooldown = 120;
		}
		if (!isFacing(target)) {
			if (canCharge() && shootAnimTime == 0) {
				increaseCharge();
			}
			return;
		}
		if (canShoot() && player.weapon.shootCooldown == 0 && player.weapon.canShoot(0, player)) {
			shoot(true);
			stopCharge();
		} else if (canCharge() && shootAnimTime == 0) {
			increaseCharge();
		}
	}

	public virtual void chargeGfx() {
		if (ownedByLocalPlayer) {
			chargeEffect.stop();
		}
		if (isCharging()) {
			chargeSound.play();
			chargeEffect.update(getChargeLevel(), 0);
		}
	}
}

