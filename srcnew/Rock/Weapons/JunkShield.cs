using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMXOnline;

public class JunkShield : Weapon {
	public static JunkShield netWeapon = new();

	public JunkShield() : base() {
		shootSounds = new string[] { "", "", "", "" };
		fireRate = 75;
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

		return rock?.junkShieldProjs.Count <= 0 && base.canShoot(chargeLevel, player);
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		int chargeLevel = args[0];

		if (character.charState is LadderClimb lc) {
				character.changeState(new ShootAltLadder(lc.ladder, this, chargeLevel), true);
		} else {
			character.changeState(new ShootAltRock(this, chargeLevel), true);
		}
	}

	public override void getProjs(Rock rock, params int[] args) {
		for (int i = 0; i < 3; i++) {
			new JunkShieldMagnet(
				rock.getCenterPos(), rock.xDir, rock,
				rock.player.getNextActorNetId(), i * 85	
			);
		}
	}
}

public class JunkShieldMagnet : Anim {

	Rock rock = null!;
	float timer;
	float startAng;
	float ang;
	float radius;
	bool once;

	public JunkShieldMagnet(
		Point pos, int xDir, Rock character, ushort? netId, float ang
	) : base(
		pos, "junk_shield_magnet", xDir, netId, false, true
	) {
		rock = character;
		rock.junkShieldProjs.Add(this);
		this.ang = ang;
		startAng = ang;
	}

	public override void update() {
		base.update();

		if (radius < 30) radius += 0.5f;
		ang += 5;

		changePos(rock.getCenterPos().add(Point.createFromByteAngle(ang % 256).times(radius)));

		timer += Global.speedMul;
		if (timer >= 30 && !once && startAng == 0) {
			once = true;
			for (int i = 0; i < 8; i++) {
				new JunkShieldPiece(
					rock.getCenterPos(), rock.xDir, rock,
					rock.player.getNextActorNetId(), i * 32, this
				);
			};
		} else if (timer >= 60) destroySelf();
	}

	public override void onDestroy() {
		base.onDestroy();
		rock.junkShieldProjs.Remove(this);
	}
}

public class JunkShieldPiece : Anim {

	Rock rock = null!;
	Anim magnet;
	float startAng;
	float ang;
	float radius = 80;
	public JunkShieldPiece(
		Point pos, int xDir, Character character, ushort? netId, float ang, Anim magnet
	) : base(
		pos, "junk_shield_pieces", xDir, netId, false, true
	) {
		frameSpeed = 0;
		frameIndex = Helpers.randomRange(0, 3);
		this.rock = character as Rock ?? throw new NullReferenceException();
		rock.junkShieldProjs.Add(this);
		this.magnet = magnet;
		startAng = ang;
		this.ang = ang;
		changePos(rock.getCenterPos().add(Point.createFromByteAngle(ang).times(radius)));
	}

	public override void update() {
		base.update();

		if (magnet == null || magnet.destroyed) {
			destroySelf();
			return;
		}

		if (radius > 30) radius -= 2;
		ang += 5;
		changePos(rock.getCenterPos().add(Point.createFromByteAngle(ang).times(radius)));
	}

	public override void onDestroy() {
		base.onDestroy();
		if (!ownedByLocalPlayer) return;
		
		rock.junkShieldProjs.Remove(this);

		if (startAng != 0) return;

		Point pos = rock.getCenterPos();
		int xDir = rock.xDir;
		Player player = rock.player;

		for (int i = 0; i < 3; i++) {
			//Main pices
			float ang = 85 * i;
			var parent = new JunkShieldProj(pos, xDir, player, ang, player.getNextActorNetId(), true)
			{ frameIndex = 5, isParent = true };

			for (int j = 0; j < 2; j++) {
				//Smol pieces
				float angs = ang + (j * 42.5f);
				bool small = j == 1;
				int frame = small ? Helpers.randomRange(0, 1) : Helpers.randomRange(2, 4);
				if (MathF.Ceiling(angs) % 85 == 0 || angs == 0) angs -= 12;

				var son = new JunkShieldProj(pos, xDir, player, angs, player.getNextActorNetId(), true)
				{ frameIndex = frame, smallestSon = small };

				son.parent = parent;
				parent.sons.Add(son);
			}
		}
	}
}


public class JunkShieldProj : Projectile {

	public JunkShieldProj? parent;
	public bool isParent;
	public List<JunkShieldProj?> sons = new();
	public bool smallestSon;
	bool threw;
	Player player;
	Rock rock = null!;
	float ang;
	float radius = 30;
	bool sound;

	public JunkShieldProj(
		Point pos, int xDir, Player player, 
		float ang, ushort netProjId, bool rpc = false
	) : base(
		JunkShield.netWeapon, pos, xDir, 0, 1, 
		player, "junk_shield_pieces", 0, 0.25f, 
		netProjId, player.ownedByLocalPlayer) 
	{
		projId = (int)RockProjIds.JunkShield;
		rock = player.character as Rock ?? throw new NullReferenceException();
		rock.junkShieldProjs.Add(this);
		
		destroyOnHit = false;
		frameSpeed = 0;
		
		this.player = player;
		this.ang = ang;
		changePos(rock.getCenterPos().add(Point.createFromByteAngle(ang).times(radius)));
		canBeLocal = false;

		if (rpc) {
			byte[] extraArgs = new byte[] { (byte)ang };

			rpcCreate(pos, player, netProjId, xDir, extraArgs);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new JunkShieldProj(
			arg.pos, arg.xDir, arg.player, arg.extraData[0], arg.netId
		);
	}

	public override void update() {
		base.update();

		if (parent != null && parent.destroyed && smallestSon) destroySelf();
		if (threw) return;

		ang += 5;
		changePos(rock.getCenterPos().add(Point.createFromByteAngle(ang % 256).times(radius)));

		if (!ownedByLocalPlayer) return;

		if (rock == null || rock.charState is Die || (rock.player.weapon is not JunkShield)) {
			destroySelfNoEffect();
			return;
		}

		if (time >= (Global.speedMul * 15) / 60 && player.input.isPressed(Control.Shoot, player)) {
			shootProjs();
			rock.weaponCooldown = 60;
		}
	}


	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);

		if (damagable.canBeDamaged(damager.owner.alliance, damager.owner.id, projId)) {
			if (damagable.projectileCooldown.ContainsKey(projId + "_" + owner.id) &&
				damagable.projectileCooldown[projId + "_" + owner.id] >= damager.hitCooldown
			) {
				destroySelf();
			}
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		if (!ownedByLocalPlayer) return;
		
		rock.junkShieldProjs.Remove(this);
	}

	public void shootProjs() {
		if (parent != null && parent.destroyed && !smallestSon) {
			threw = true;
			changePos(rock.getCenterPos());
			shoot(ang + 12);
			playSound("thunder_bolt");
		}
		else if (isParent) {
			threw = true;
			changePos(rock.getCenterPos());
			float a = ang;
			shoot(a);
			playSound("thunder_bolt");

			foreach(var son in sons) {
				if (son == null) continue;
				int i = 1;
				son.threw = true;
				son.changePos(rock.getCenterPos());
				Global.level.delayedActions.Add(
					new DelayedAction(() => {
						son?.shoot(a);
					},
					0.15f * i

					)
				);
				i++;
			}
		}
	}

	public void shoot(float a) {
		updateDamager(2);
		damager.hitCooldown = 1;
		vel = Point.createFromByteAngle(a).times(180);
		time = 0;
		maxTime = 0.75f;
		destroyOnHit = true;
		rock.junkShieldProjs.Remove(this);
	}
}
