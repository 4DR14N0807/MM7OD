using System;
using System.Collections.Generic;

namespace MMXOnline;

public class Bass : Character {
	// Weapons.
	public float weaponCooldown;
	public CopyVisionClone? cVclone;
	public SpreadDrillProj? sDrill;
	public SpreadDrillMediumProj? sDrillM;
	public RemoteMineProj? rMine;
	public float wBurnerAngle;
	public int wBurnerAngleMod = 1;
	public float tBladeDashCooldown;

	// Modes.
	public bool isSuperBass;
	
	// AI Stuff.
	public float aiWeaponSwitchCooldown = 120;

	public Bass(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer,
		bool isWarpIn = true
	) : base(
		player, x, y, xDir, isVisible, netId, ownedByLocalPlayer, isWarpIn, false, false
	) {
		charId = CharIds.Bass;
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

	public override void update() {
		base.update();
		Helpers.decrementFrames(ref weaponCooldown);
		Helpers.decrementFrames(ref tBladeDashCooldown);

		// Shoot controls.
		bool shootPressed;
		if (player.weapon.isStream) {
			shootPressed = player.input.isHeld(Control.Shoot, player);
		} else {
			shootPressed = player.input.isPressed(Control.Shoot, player);
		};
		if (shootPressed) {
			lastShootPressed = Global.frameCount;
		}
		player.changeWeaponControls();

		if (player.weapon is not WaveBurner || !player.input.isHeld(Control.Shoot, player)) {
			wBurnerAngleMod = 1;
			wBurnerAngle = 0;
		} 
	}

	public override Projectile? getProjFromHitbox(Collider hitbox, Point centerPoint) {
		int meleeId = getHitboxMeleeId(hitbox);
		if (meleeId == -1) {
			return null;
		}
		Projectile? proj = getMeleeProjById(meleeId, centerPoint);
		if (proj == null) {
			return null;
		}
		// Assing data variables.
		proj.meleeId = meleeId;
		proj.owningActor = this;

		return proj;
	}

	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"bass_tblade_dash" => MeleeIds.TenguBladeDash,
			_ => MeleeIds.None
		});
	}

	public Projectile? getMeleeProjById(int id, Point? pos = null, bool addToLevel = true) {
		Point projPos = pos ?? new Point(0, 0);
		Projectile? proj = id switch {
			/*(int)MeleeIds.TenguBladeDash => new GenericMeleeProj(
				new TenguBlade(), projPos, ProjIds.TenguBladeDash, player, 2, 0, 0.375f,
				addToLevel: addToLevel

			),*/
			(int)MeleeIds.TenguBladeDash => new TenguBladeMelee(
				projPos, player
			),
			
			_ => null
		};
		return proj;

	}

	public enum MeleeIds {
		None = -1,
		TenguBladeDash,
	}

	public bool canUseTBladeDash() {
		return player.weapon is TenguBlade tb && tb.ammo > 0 &&
		grounded && tBladeDashCooldown <= 0;
	}

	public override bool normalCtrl() {
		bool dashPressed = player.input.isPressed(Control.Dash, player);
		if (dashPressed && canUseTBladeDash()) {
			changeState(new TenguBladeDash(), true);
			return true;
		} 

		return base.normalCtrl();
	}

	public override bool attackCtrl() {
		int framesSinceLastShootPressed = Global.frameCount - lastShootPressed;
		if (framesSinceLastShootPressed <= 6) {
			if (weaponCooldown <= 0 && player.weapon.canShoot(0, player)) {
				shoot();
				return true;
			}
		}
		return base.attackCtrl();
	}

	public void shoot() {
		turnToInput(player.input, player);
		if (player.weapon is not TenguBlade) {
			if (charState is LadderClimb or BassShootLadder) changeState(new BassShootLadder(), true);
			else changeState(new BassShoot(), true);
		}
		player.weapon.shoot(this, 0);
		weaponCooldown = player.weapon.fireRateFrames;
		player.weapon.addAmmo(-player.weapon.getAmmoUsage(0), player);
	}

	public int getShootYDir() {
		int dir = player.input.getYDir(player);
		int multiplier = 2;
		if (dir == 2 || player.input.getXDir(player) != 0) {
			multiplier = 1;
		}
		if (dir * multiplier == 2) return 1;

		return dir * multiplier;
	}

	public int getShootAngle() {
		int baseAngle = 0;
		if (xDir == -1) {
			baseAngle = 128;
		}
		return getShootYDir() * xDir * 32 + baseAngle;
	}

	public static List<Weapon> getAllWeapons() {
		return new List<Weapon>() {
			new BassBuster(),
			new IceWall(),
			new CopyVision(),
			new SpreadDrill(),
			new WaveBurner(),
			new RemoteMine(),
			new LightingBolt(),
			new TenguBlade(),
			new MagicCard(),
		};
	}

	public override string getSprite(string spriteName) {
		return "bass_" + spriteName;
	}

	public override bool canCrouch() {
		return false;
	}

	public override bool canMove() {
		if (shootAnimTime > 0 && grounded) {
			return false;
		}
		return base.canMove();
	}

	public override bool canShoot() {
		if (weaponCooldown > 0 ||
			charState is Dash
		) {
			return false;
		}
		return base.canShoot();
	}

	public override bool canAirJump() {
		return dashedInAir == 0 && rootTime <= 0 && charState is not BassShootLadder;
	}

	public override bool canAirDash() {
		return false;
	}

	public override bool canWallClimb() {
		return false;
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
			return;
		}
		if (canShoot()) {
			shoot();
		}
	}

	public override List<ShaderWrapper> getShaders() {
		List<ShaderWrapper> baseShaders = base.getShaders();
		List<ShaderWrapper> shaders = new();
		ShaderWrapper? palette = null;

		int index = player.weapon.index;
		palette = player.bassPaletteShader;

		palette?.SetUniform("palette", index);
		palette?.SetUniform("paletteTexture", Global.textures["bass_palette_texture"]);

		if (palette != null) {
			shaders.Add(palette);
		}
		if (shaders.Count == 0) {
			return baseShaders;
		}

		shaders.AddRange(baseShaders);
		return shaders;
	}

	public override List<byte> getCustomActorNetData() {
		// Get base arguments.
		List<byte> customData = base.getCustomActorNetData() ?? new();

		// Per-character data.
		int weaponIndex = player.weapon.index;
		if (weaponIndex == (int)WeaponIds.HyperBuster) {
			weaponIndex = player.weapons[player.hyperChargeSlot].index;
		}
		customData.Add((byte)weaponIndex);
		customData.Add((byte)MathF.Ceiling(player.weapon?.ammo ?? 0));
	
		bool[] flags = [
			isSuperBass,
		];
		customData.Add(Helpers.boolArrayToByte(flags));

		return customData;
	}

	public override void updateCustomActorNetData(byte[] data) {
		// Update base arguments.
		base.updateCustomActorNetData(data);
		data = data[data[0]..];

		// Per-character data.
		player.changeWeaponFromWi(data[0]);
		if (player.weapon != null) {
			player.weapon.ammo = data[1];
		}

		bool[] flags = Helpers.byteToBoolArray(data[2]);
		isSuperBass = flags[0];
	}
}

