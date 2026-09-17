using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SFML.System;

namespace MMXOnline;

public class JunkShield : Weapon {
	public static JunkShield netWeapon = new();
	public static float cooldown = 60;
	public List<JunkShieldGroup> groups = new();

	public JunkShield() : base() {
		displayName = "JUNK SHIELD";
		shootSounds = ["", "", "", ""];
		fireRate = cooldown;
		switchCooldown = 45;
		index = (int)RockWeaponIds.JunkShield;
		weaponBarBaseIndex = (int)RockWeaponBarIds.JunkShield;
		weaponBarIndex = weaponBarBaseIndex;
		weaponSlotIndex = (int)RockWeaponSlotIds.JunkShield;
		killFeedIndex = 0;
		maxAmmo = 12;
		ammo = maxAmmo;
		hasCustomAnim = true;
		descriptionV2 = [[
			"Offensive barrier that gets\n" +
			"damaged after rough use.\n" +
			"Can be aimen in 8 directions."
		]];
	}

	public override bool canShoot(int chargeLevel, Character character) {
		return base.canShoot(chargeLevel, character) || groups.Count > 0;
	}

	public override float getAmmoUsage(int chargeLevel) {
		if (groups.Count == 0) return base.getAmmoUsage(chargeLevel);
		return 0;
	}

	public override void charLinkedUpdate(Character character, bool isAlwaysOn) {
		base.charLinkedUpdate(character, isAlwaysOn);

		for (int i = 0; i < groups.Count; i++) {
			if (groups[i].destroyed) {
				groups.RemoveAt(i);
			}
		}
	}

	public override void shootRock(Rock rock, params int[] args) {
		base.shoot(rock, args);
		int chargeLevel = args[0];
		Player player = rock.player;

		if (groups.Count == 0) {
			if (rock.charState is LadderClimb lc) {
			rock.changeState(new ShootAltLadder(lc.ladder, this, chargeLevel), true);
			} else {
				rock.changeState(new ShootAltRock(this, chargeLevel), true);
			}
		} else {
			Point input = player.input.getInputDir(player);
			float ang = input.byteAngle;
			float dif = 15;
			float[] angles = new float[3] {ang, ang - dif, ang + dif};
			if (rock.xDir < 0 && input == Point.zero) {
				ang = -ang + 128;
			}

			int i = 0;
			foreach (var group in groups) {
				if (input != Point.zero) {
					group.shootProjs((angles[i]));
				} else {
					group.shootProjs(ang + (i * 85));
				}

				i++;
			}
		}
	}

	public override void getProjs(Rock rock, params int[] args) {
		for (int i = 0; i < 3; i++) {
			new JunkShieldMagnet(
				this, rock.getCenterPos(), rock.xDir, rock,
				rock.player.getNextActorNetId(), i * 85
			);
		}
	}
}

public class JunkShieldMagnet : Anim {
	Rock? rock;
	float timer;
	float startAng;
	float ang;
	float radius;
	bool once;
	JunkShield? wep;
	float t1 = 15;
	float t2 = 30;

	public JunkShieldMagnet(
		JunkShield wep, Point pos, int xDir, Rock rock, ushort? netId, float ang
	) : base(
		pos, "junk_shield_magnet", xDir, netId, false, true
	) {
		this.rock = rock;

		this.wep = wep;
		if (rock.ownedByLocalPlayer) {
			rock.junkShieldProjs.Add(this);
		}
		this.ang = ang;
		startAng = ang;
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer || rock == null) return;

		//if (radius < 30) radius += 1;
		radius += 30 / t2;
		ang += 5;

		timer += Global.speedMul;
		if (timer >= t1 && !once && startAng == 0) {
			once = true;
			if (wep != null) {
				for (int i = 0; i < 8; i++) {
					new JunkShieldPiece(
						wep, rock.getCenterPos(), rock.xDir, rock,
						rock.player.getNextActorNetId(), i * 32, this
					);
				}
				;
			}
		} else if (timer >= t2) {
			destroySelf();
		}
	}

	public override void postUpdate() {
		base.postUpdate();
		if (rock == null) return;

		changePos(rock.getCenterPos().add(Point.createFromByteAngle(ang % 256).times(radius)));
	}

	public override void onDestroy() {
		base.onDestroy();
		if (!ownedByLocalPlayer || rock == null) return;
		rock.junkShieldProjs.Remove(this);
	}
}

public class JunkShieldPiece : Anim {
	Rock? rock;
	Anim magnet;
	float startAng;
	float ang;
	float radius = 80;
	JunkShield? wep;

	public JunkShieldPiece(
		JunkShield wep, Point pos, int xDir, Character character, ushort? netId, float ang, Anim magnet
	) : base(
		pos, "junk_shield_pieces", xDir, netId, false, true
	) {
		frameSpeed = 0;
		frameIndex = Helpers.randomRange(0, 3);
		rock = character as Rock;
		this.magnet = magnet;
		startAng = ang;
		this.ang = ang;
		this.wep = wep;
		if (rock != null) {
			rock.junkShieldProjs.Add(this);
			changePos(rock.getCenterPos().add(Point.createFromByteAngle(ang).times(radius)));
		}

		alpha = 0;
	}

	public override void update() {
		base.update();

		if (magnet == null || magnet.destroyed) {
			destroySelf();
			return;
		}

		if (radius > 30) radius -= 50 / 15f;
		ang += 5;
		alpha += 1 / 15f;

		if (rock != null) {
			changePos(rock.getCenterPos().add(Point.createFromByteAngle(ang).times(radius)));
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		if (!ownedByLocalPlayer ||
			rock == null || wep == null
		) {
			return;
		}
		rock.junkShieldProjs.Remove(this);

		if (startAng != 0) { return; }

		Point pos = rock.getCenterPos();
		int xDir = rock.xDir;
		Player player = rock.player;

		for (int i = 0; i < 3; i++) {
			float ang = 85 * i;

			var group = new JunkShieldGroup(rock, pos, xDir, wep,player);
			group.ang = ang;
			wep.groups.Add(group);

			//Main pices
			
			var parent = new JunkShieldRmProj(
				rock, pos, xDir, player.getNextActorNetId(), 5, i, ang, false, true, player
			) {
				isMain = true
			};

			group.projs.Add(parent);
			rock.junkShieldProjs.Add(parent);

			for (int j = 0; j < 2; j++) {
				//Smol pieces
				float angs = ang + (j * 42.5f);
				bool small = j == 1;
				int frame = small ? Helpers.randomRange(0, 1) : Helpers.randomRange(2, 4);
				if (MathF.Ceiling(angs) % 85 == 0 || angs == 0) angs -= 12;

				var son = new JunkShieldRmProj(
					rock, pos, xDir, player.getNextActorNetId(), frame, i, angs, small,true, player
				);

				group.projs.Add(son);
				rock.junkShieldProjs.Add(son);
			}

			group.updateMainProj();
		}
	}
}

public class JunkShieldGroup : Projectile {
	JunkShield wep;
	public List<JunkShieldRmProj> projs = new();
	public JunkShieldRmProj? mainProj;
	public float ang;
	bool shoot;
	public JunkShieldGroup(
		Actor owner, Point pos, int xDir, JunkShield wep, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "empty", null, altPlayer
	) {
		this.wep = wep;
	}

	public override void update() {
		base.update();

		if (!shoot) {
			ang += 5;
		}
		changePos(ownerActor?.pos ?? pos);

		for (int i = 0; i < projs.Count; i++) {
			if (projs[i].destroyed) {
				projs.Remove(projs[i]);
				(ownerActor as Rock)?.junkShieldProjs.Remove(projs[i]);
			}
		}

		updateMainProj();

		if (projs.Count == 1) {
			for (int i = 0; i < projs.Count; i++) {
				projs[i].destroySelf();
			}
			destroySelf();
		}

		if (ownerPlayer.weapon is not JunkShield) {
			wep?.shootCooldown = JunkShield.cooldown;
			(ownerActor as Rock)?.weaponCooldown = 20;

			shootProjs(ang);
		}
	}

	public void updateMainProj() {
		mainProj = projs[0];
		foreach (var proj in projs) {
			if (proj.hierarchy < mainProj.hierarchy) {
				mainProj = proj;
				proj.isMain = true;
			}
		}
	}

	public void shootProjs(float a) {
		ownerActor?.playSound("thunder_bolt", sendRpc: true);
		shoot = true;
		Point shootPos = ownerActor?.getCenterPos() ?? pos;
		ang = a;

		mainProj?.shoot(shootPos, ang);
		if (mainProj != null) {
			projs.Remove(mainProj);
		}

		int i = 1;
		foreach (var proj in projs) {
			proj.threw = true;
			proj.changePos(ownerActor?.getCenterPos() ?? pos);

			Global.level.delayedActions.Add(
				new DelayedAction(() => { proj.shoot(shootPos, ang); }, 
				(4 / 60f) * i));

			i++;
		}
	}
}

public class JunkShieldRmProj : Projectile {
	public int hierarchy = 0;
	public bool isMain;
	public int type;
	public float ang;
	float radius = 30;
	public bool threw;
	public JunkShieldRmProj(
		Actor owner, Point pos, int xDir, ushort? netId, int fi, 
		int type, float ang, bool small, bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "junk_shield_pieces", netId, altPlayer
	) {
		projId = (int)RockProjIds.JunkShield;

		damager.damage = 1;
		damager.hitCooldown = 15;
		destroyOnHit = false;

		this.type = type;
		this.ang = ang;

		frameSpeed = 0;
		frameIndex = fi;

		if (small) {
			alpha = 0.5f;
		}

		changePos(ownerActor?.getCenterPos().add(Point.createFromByteAngle(ang).times(radius)) ?? pos);

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, new byte[] 
				{ (byte)fi, (byte)type, (byte)ang, Helpers.boolToByte(small) }
			);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new JunkShieldRmProj(
			arg.owner, arg.pos, arg.xDir, arg.netId, arg.extraData[0], arg.extraData[1], 
			arg.extraData[2], Helpers.byteToBool(arg.extraData[3]), altPlayer: arg.player
		);
	}

	public override void update() {
		base.update();

		if (threw) return;

		ang += 7;
		changePos(ownerActor?.getCenterPos().add(Point.createFromByteAngle(ang).times(radius)) ?? pos);
	}

	public override void afterDamage(IDamagable damagable, bool wasHit) {
		base.afterDamage(damagable, wasHit);
		if (!ownedByLocalPlayer) return;

		if (wasHit) {
			if (isMain) {
				new Anim(pos, "generic_explosion", xDir, damager.owner.getNextActorNetId(), true, true);
				playSound("explosion", sendRpc: true);
			} else {
				new Anim(pos, sprite.name, xDir, null, false, true) {
					useGravity = true,
					vel = new Point(0, -180),
					ttl = 1.25f,
					frameSpeed = 0,
					frameIndex = frameIndex
				};
			}

			destroySelf();
		}
	}

	public void shoot(Point p, float a) {
		if (ownerActor == null || !ownedByLocalPlayer) return;
		destroySelf();

		new JunkShieldRmProj2(
			ownerActor, p, ownerActor.xDir, 
			damager.owner.getNextActorNetId(), frameIndex, a, alpha * 100, true
		);
	}
}



public class JunkShieldRmProj2 : Projectile {
	public JunkShieldRmProj2(
		Actor owner, Point pos, int xDir, ushort? netId,
		int fi, float ang, float alp, bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "junk_shield_pieces", netId, altPlayer
	) {
		projId = (int)RockProjIds.JunkShield2;
		maxTime = 0.35f;

		frameIndex = fi;
		frameSpeed = 0;
		damager.damage = 2;
		damager.flinch = Global.halfFlinch;
		damager.hitCooldown = 30;

		vel = Point.createFromByteAngle(ang).times(360);
		alpha = alp / 100;

		if (rpc) rpcCreate(pos, owner, ownerPlayer, netId, xDir, new byte[] { (byte)fi, (byte)ang, (byte)alp });
	}

	public override void onDestroy() {
		base.onDestroy();

		new Anim(pos, sprite.name, xDir, null, false, true) {
			useGravity = true,
			vel = new Point(0, -180),
			ttl = 0.5f,
			frameSpeed = 0,
			frameIndex = frameIndex,
			alpha = 0.5f,
			blink = true
		};
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new JunkShieldRmProj2(
			arg.owner, arg.pos, arg.xDir, arg.netId, arg.extraData[0], 
			arg.extraData[1], arg.extraData[2], altPlayer: arg.player
		);
	}
}
