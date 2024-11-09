using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline;

public class JunkShield : Weapon {

	public static JunkShield netWeapon = new JunkShield();
	public JunkShield() : base() {
		shootSounds = new string[] { "", "", "", "" };
		fireRate = 60;
		index = (int)RockWeaponIds.JunkShield;
		weaponBarBaseIndex = (int)RockWeaponBarIds.JunkShield;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = (int)RockWeaponSlotIds.JunkShield;
		killFeedIndex = 0;
		maxAmmo = 10;
		ammo = maxAmmo;
		description = new string[] { "Defenseless barrier that gets", "damaged after rough use.", "Can be fired in up to 3 directions." };
	}

	public override bool canShoot(int chargeLevel, Player player) {
		Rock? rock = player.character as Rock;

		return rock?.junkShield == null && base.canShoot(chargeLevel, player);
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		int chargeLevel = args[0];

		if (character.charState is LadderClimb) {
				character.changeState(new ShootAltLadder(this, chargeLevel), true);
		} else {
			character.changeState(new ShootAlt(this, chargeLevel), true);
		}
	}
}


public class JunkShieldProj : Projectile {
	public int HP = 6;
	public float healthDecCooldown;
	Player player;
	Rock? rock;
	Point centerPos;
	public List<Sprite> mainProjs = new List<Sprite>();
	int mainProjsCount = 3;
	public List<Sprite> otherProjs = new List<Sprite>();
	int otherProjsCount = 6;
	float projAngle;
	float radius = 30;
	public List<int> randomPieces = new List<int>();
	LoopingSound sound;

	public JunkShieldProj(
		Point pos, int xDir, Player player, 
		ushort netProjId, bool rpc = false
	) : base(
		JunkShield.netWeapon, pos, xDir, 0, 1, 
		player, "junk_shield_proj", 0, 0.25f, 
		netProjId, player.ownedByLocalPlayer) 
	{
		projId = (int)RockProjIds.JunkShield;
		destroyOnHit = false;
		rock = player.character as Rock;
		if (rock != null) rock.junkShield = this;
		sound = new LoopingSound("charge_start", "charge_loop", this);
		this.player = player;
		canBeLocal = false;

		for (int i = 0; i < mainProjsCount; i++) {
			Sprite mainProjSprite = new Sprite("junk_shield_pieces");
			mainProjSprite.frameIndex = 5;
			mainProjSprite.frameSpeed = 0;
			mainProjs.Add(mainProjSprite);
		}

		for (int i = 0; i < otherProjsCount; i++) {
			Sprite otherProjSprite = new Sprite("junk_shield_pieces");
			randomPieces.Add(Helpers.randomRange(0, 4));
			otherProjSprite.frameSpeed = 0;
			otherProjs.Add(otherProjSprite);
		}

		if (rpc) rpcCreate(pos, player, netProjId, xDir);
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new JunkShieldProj(
			arg.pos, arg.xDir, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();

		if (projAngle >= 256) projAngle = 0;
		projAngle += 5;

		if (rock != null) {
			xDir = rock.getShootXDir();
			pos = rock.getCenterPos().round();
		}

		if (sound != null) sound.play();

		if (healthDecCooldown > 0) {
			healthDecCooldown += Global.spf;
			if (healthDecCooldown > damager.hitCooldown) healthDecCooldown = 0;
		}

		if (HP <= 0 || rock == null || rock.charState is Die || (rock.player.weapon is not JunkShield)) {
			destroySelfNoEffect();
			return;
		}

		if (time >= Global.spf * 15 && player.input.isPressed(Control.Shoot, player)) {
			shootProjs();
			rock.weaponCooldown = 60;
		}
	}

	public override void render(float x, float y) {
		base.render(x, y);
		if (rock != null) centerPos = rock.getCenterPos().round();
		float hpCount = HP;
		float extra = HP % 2;
		mainProjsCount = (int)(hpCount + extra) / 2;

		//main pieces render
		for (var i = 0; i < mainProjsCount; i++) {
			float extraAngle = projAngle + i * 85;
			if (extraAngle >= 256) extraAngle -= 256;
			float xPlus = Helpers.cosd(extraAngle * 1.40625f) * radius;
			float yPlus = Helpers.sind(extraAngle * 1.40625f) * radius;
			if (rock != null) xDir = rock.getShootXDir();
			mainProjs[i].draw(5, centerPos.x + xPlus, centerPos.y + yPlus, xDir, yDir, getRenderEffectSet(), 1, 1, 1, zIndex);
		}

		//small pieces render
		for (var i = 0; i < HP; i++) {
			float extraAngle = (projAngle + i * 42.5f) - 10;
			if (extraAngle >= 256) extraAngle -= 256;
			float xPlus = Helpers.cosd(extraAngle * 1.40625f) * radius;
			float yPlus = Helpers.sind(extraAngle * 1.40625f) * radius;
			if (rock != null) xDir = rock.getShootXDir();
			otherProjs[i].draw(randomPieces[i], centerPos.x + xPlus, centerPos.y + yPlus, xDir, yDir, getRenderEffectSet(), 1, 1, 1, zIndex);
		}
	}


	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);

		if (damagable.canBeDamaged(damager.owner.alliance, damager.owner.id, projId)) {
			if (damagable.projectileCooldown.ContainsKey(projId + "_" + owner.id) &&
				damagable.projectileCooldown[projId + "_" + owner.id] >= damager.hitCooldown
			) {
				HP--;
			}
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		sound.stop();
		if (rock != null) rock.junkShield = null;
	}

	public void shootProjs() {
		int hpCount = HP;
		int extra = HP % 2;
		int actualCount = (hpCount + extra) / 2;
		destroySelfNoEffect();

		for (var i = 0; i < actualCount; i++) {
			var angleToShoot = (int)projAngle + (85 * i);
			if (angleToShoot >= 256) angleToShoot -= 256;
			if (rock != null) new JunkShieldShootProj(rock.getCenterPos(), rock.getShootXDir(), damager.owner, angleToShoot, damager.owner.getNextActorNetId(true), true);
		}
		Global.playSound("thunder_bolt");
	}
}


public class JunkShieldShootProj : Projectile {

	int shootAngle;
	const int projSpeed = 180;
	Rock? rock;

	public JunkShieldShootProj(
		Point pos, int xDir, Player player, 
		int angle, ushort netProjId, bool rpc = false
	) : base(
		JunkShield.netWeapon, pos, xDir, 0, 2, 
		player, "junk_shield_shoot_proj", 0, 0.5f, 
		netProjId, player.ownedByLocalPlayer) 
	{
		projId = (int)RockProjIds.JunkShieldPiece;
		rock = player.character as Rock;
		maxTime = 0.75f;
		shootAngle = angle;
		rock = player.character as Rock;
		if (rock != null) base.xDir = rock.getShootXDir();
		canBeLocal = false;
		fadeOnAutoDestroy = true;

		if (base.xDir < 0) {
			int angleFix;
			if (shootAngle <= 128) angleFix = 128 - shootAngle;
			else angleFix = (256 - shootAngle) + 128;

			shootAngle = angleFix;
		}

		frameIndex = (int)shootAngle / 16;
		frameSpeed = 0;

		base.vel = Point.createFromByteAngle(angle) * projSpeed;

		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)angle };

			rpcCreate(pos, player, netProjId, xDir, extraArgs);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new JunkShieldShootProj(
			arg.pos, arg.xDir, arg.player, arg.extraData[0], arg.netId
		);
	}
}
