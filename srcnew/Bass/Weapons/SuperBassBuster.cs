using System;
using System.Collections.Generic;

namespace MMXOnline;

#region Buster Weapon

public class SBassBuster : Weapon {
	public static SBassBuster netWeapon = new();

	public SBassBuster() : base() {
		iconSprite = "hud_weapon_icon_bass";
		index = (int)BassWeaponIds.SuperBassBuster;
		weaponSlotIndex = index;
		fireRate = 10;
		drawAmmo = false;
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 0;
	}

	public override void shoot(Character character, params int[] args) {
		Point shootPos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;
		int chargeLevel = args[0];
		Bass bass = character as Bass ?? throw new NullReferenceException();

		if (chargeLevel == 3) {
			new ChamoBuster(bass, shootPos.addxy(12 * xDir, 0), xDir, player.getNextActorNetId(), true);
			character.playSound("buster3", sendRpc: true);

			for (int i = 1; i < 3; i++) {
				Global.level.delayedActions.Add(new DelayedAction(
					() => {
						new SBassShot(
							bass, shootPos.addxy(11 * xDir, 0), xDir,
							player.getNextActorNetId(), true, superBass: true
						);
						character.playSound("buster2", sendRpc: true);
						shootCooldown = fireRate;
						bass.weaponCooldown = fireRate;
					},
					(i * 8) / 60f
				));
			}
		} else if (chargeLevel == 2) {
			new ChamoBuster(bass, shootPos, xDir, player.getNextActorNetId(), true);
			character.playSound("buster3", sendRpc: true);
		} else if (chargeLevel == 1) {
			new SBassShot(bass, shootPos, xDir, player.getNextActorNetId(), true);
			character.playSound("buster2X1", sendRpc: true);
		} else {
			new SBassLemon(bass, shootPos, xDir, 0, player.getNextActorNetId(), true);
			new SBassLemon(bass, shootPos, xDir, 1, player.getNextActorNetId(), true);
			new SBassLemon(bass, shootPos, xDir, 2, player.getNextActorNetId(), true);

			character.playSound("bassbuster");
		}
	}
}

#endregion

#region Rocket Punch Weapon

public class SBassRP : Weapon {
	public SBassRP() : base() {
		iconSprite = "hud_weapon_icon_bass";
		index = (int)BassWeaponIds.SuperBassRP;
		weaponSlotIndex = index;
		fireRate = 10;
		drawAmmo = false;
	}

	public override float getAmmoUsage(int chargeLevel) {
		return 0;
	}

	public override void shoot(Character character, params int[] args) {
		Point shootPos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;
		int chargeLevel = args[0];
		Bass bass = character as Bass ?? throw new NullReferenceException();

		if (chargeLevel >= 3) {
			new SuperBassRP(bass, shootPos, xDir, player.getNextActorNetId(), true);
			character.playSound("super_adaptor_punch", sendRpc: true);

			for (int i = 1; i < 3; i++) {
				Global.level.delayedActions.Add(new DelayedAction(
					() => {
						new SBassShot(
							bass, shootPos.addxy(11 * xDir, 0), xDir,
							player.getNextActorNetId(), true,
							superBass: bass.isSuperBass || bass.isTrebbleBoost
						);
						character.playSound("buster2", sendRpc: true);
						shootCooldown = fireRate;
						bass.weaponCooldown = fireRate;
					},
					(i * 8) / 60f
				));
			}
		} else if (chargeLevel == 2) {
			bass.sbRocketPunch = new SuperBassRP(bass, shootPos, xDir, player.getNextActorNetId(), true);
			character.playSound("super_adaptor_punch", sendRpc: true);
		} else if (chargeLevel == 1) {
			new SBassShot(bass, shootPos, xDir, player.getNextActorNetId(), true);
			character.playSound("buster2X1", sendRpc: true);
		} else {
			new SBassLemon(bass, shootPos, xDir, 0, player.getNextActorNetId(), true);
			new SBassLemon(bass, shootPos, xDir, 1, player.getNextActorNetId(), true);
			new SBassLemon(bass, shootPos, xDir, 2, player.getNextActorNetId(), true);

			character.playSound("bassbuster");
		}
	}
}

#endregion


#region Projectiles
public class SBassLemon : Projectile {
	public SBassLemon(
		Actor owner, Point pos, int xDir, int type, ushort? netId,
		bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "bass_buster_proj", netId, altPlayer
	) {
		projId = (int)BassProjIds.SuperBassLemon;
		maxTime = 0.3f;

		byteAngle = (type - 1) * 32;
		if (xDir < 0) {
			byteAngle = -byteAngle + 128;
			xScale *= -1;
		}
		vel = Point.createFromByteAngle(byteAngle).times(360);
		damager.damage = 1;
		damager.hitCooldown = 5;
		fadeSprite = "bass_buster_proj_fade";
		fadeOnAutoDestroy = true;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, new byte[] { (byte)type });
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new SBassLemon(
			arg.owner, arg.pos, arg.xDir, arg.extraData[0], 
			arg.netId, altPlayer: arg.player
		);
	}
}

public class SBassShot : Projectile {
	public SBassShot(
		Actor owner, Point pos, int xDir, ushort? netId,
		bool rpc = false, Player? altPlayer = null, bool superBass = false
	) : base(
		pos, xDir, owner, "bass_buster_proj2", netId, altPlayer
	) {
		projId = (int)BassProjIds.SuperBassShot;
		maxTime = 0.5f;

		vel.x = 300 * xDir;
		damager.damage = 2;
		if (superBass) {
			damager.damage = 1;
			damager.flinch = Global.miniFlinch;
		}
		fadeSprite = "rock_buster1_fade";
		fadeOnAutoDestroy = true;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new SBassShot(
			arg.owner, arg.pos, arg.xDir, arg.netId, altPlayer: arg.player
		);
	}
}


public class ChamoBuster : Projectile {
	public ChamoBuster(
		Actor owner, Point pos, int xDir, ushort? netId,
		bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "bass_chamobuster", netId, altPlayer
	) {
		projId = (int)BassProjIds.ChamoBuster;
		maxTime = 0.5f;

		vel.x = 360 * xDir;
		damager.damage = 3;
		damager.flinch = Global.halfFlinch;
		fadeSprite = "thunder_bolt_fade2";
		fadeOnAutoDestroy = true;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new ChamoBuster(
			arg.owner, arg.pos, arg.xDir, arg.netId, altPlayer: arg.player
		);
	}
}

public class SuperBassRP : Projectile {
	Player player;
	float maxReverseTime;
	bool reversed;
	Actor? target;
	float projSpeed = 240;

	public SuperBassRP(
		Actor owner, Point pos, int xDir, ushort? netId,
		bool rpc = false, Player? altPlayer = null
	) : base(
		pos, xDir, owner, "sb_rocket_punch", netId, altPlayer
	) {
		projId = (int)BassProjIds.SuperBassRocketPunch;

		maxReverseTime = 0.5f;
		this.player = ownerPlayer;

		vel.x = projSpeed * xDir;
		damager.damage = 3;
		damager.flinch = Global.halfFlinch;
		damager.hitCooldown = 30;

		destroyOnHit = false;
		canBeLocal = false;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new SuperBassRP(
			arg.owner, arg.pos, arg.xDir, arg.netId, altPlayer: arg.player
		);
	}

	public override void update() {
		base.update();
		if (!locallyControlled) return;
		if (!ownedByLocalPlayer) return;

		if (ownerActor?.destroyed != false) {
			destroySelf("generic_explosion");
			return;
		}

		var targets = Global.level.getTargets(ownerActor.pos, player.alliance, true);
		foreach (var t in targets) {
			if (ownerActor.isFacing(t) && MathF.Abs(t.pos.y - ownerActor.pos.y) < 80) {
				target = t;
				break;
			}
		}

		if (!reversed && target != null) {
			vel = new Point(0, 0);
			if (pos.x > target.pos.x) xDir = -1;
			else xDir = 1;
			Point targetPos = target.getCenterPos();
			move(pos.directionToNorm(targetPos).times(projSpeed));
			if (pos.distanceTo(targetPos) < 5) {
				reversed = true;
			}
		}

		if (!reversed && time > maxReverseTime) reversed = true;

		if (reversed) {
			vel = new Point(0, 0);
			if (pos.x > ownerActor.pos.x) xDir = -1;
			else xDir = 1;

			Point returnPos = ownerActor.getCenterPos();
			if (ownerActor.sprite.name == "rock_rocket_punch") {
				Point poi = ownerActor.pos;
				var pois = ownerActor.sprite.getCurrentFrame()?.POIs;
				if (pois != null && pois.Length > 0) {
					poi = pois[0];
				}
				returnPos = ownerActor.pos.addxy(poi.x * ownerActor.xDir, poi.y);
			}

			move(pos.directionToNorm(returnPos).times(projSpeed));
			if (pos.distanceTo(returnPos) < 10) {
				destroySelf();
				Global.playSound("super_adaptor_punch_recover");
			}
		}
	}

	public void followOwner() {
		if (ownerActor == null) {
			return;
		}
		float targetPosX = ownerActor.getCenterPos().x;
		float targetPosY = ownerActor.getCenterPos().y;
		float moveSpeed = speed;

		// X axis follow.
		if (pos.x < targetPosX) {
			move(new Point(moveSpeed, 0));
			if (pos.x > targetPosX) {
				changePos(targetPosX, pos.y);
			}
		} else if (pos.x > targetPosX) {
			move(new Point(-moveSpeed, 0));
			if (pos.x < targetPosX) {
				changePos(targetPosX, pos.y);
			}
		}
		// Y axis follow.
		if (pos.y < targetPosY) {
			move(new Point(0, moveSpeed));
			if (pos.y > targetPosY) {
				changePos(pos.x, targetPosY);
			}
		} else if (pos.y > targetPosY) {
			move(new Point(0, -moveSpeed));
			if (pos.y < targetPosY) {
				changePos(pos.x, targetPosY);
			}
		}
	}

	public void followTarget() {
		if (target == null) {
			target = Global.level.getClosestTarget(
			new Point(pos.x, pos.y),
			damager.alliance,
			false, 200
			);
		} else {
			Point enemyPos = target.getCenterPos();
			float moveSpeed = speed;

			// X axis follow.
			if (pos.x < enemyPos.x) {
				move(new Point(moveSpeed, 0));
				if (pos.x > enemyPos.x) {
					changePos(enemyPos.x, pos.y);
				}
			} else if (pos.x > enemyPos.x) {
				move(new Point(-moveSpeed, 0));
				if (pos.x < enemyPos.x) {
					changePos(enemyPos.x, pos.y);
				}
			}
			// Y axis follow.
			if (pos.y < enemyPos.y) {
				move(new Point(0, moveSpeed));
				if (pos.y > enemyPos.y) {
					changePos(pos.x, enemyPos.y);
				}
			} else if (pos.y > enemyPos.y) {
				move(new Point(0, -moveSpeed));
				if (pos.y < enemyPos.y) {
					changePos(pos.x, enemyPos.y);
				}
			}
		}
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);
		if (locallyControlled) {
			reversed = true;
		}
		if (isRunByLocalPlayer()) {
			reversed = true;
		}
	}
}


public class SweepingLaserProj : Projectile {
	int startHeight;
	Sprite? bodySprite;
	int spriteHeight;
	int pieces;
	int maxPieces = 6;
	Point endPos;
	Anim? topAnim;
	Anim? bodyAnim;
	Anim? bottomAnim;
	bool ground;
	int groundTime;
	int lastGroundTime;

	public SweepingLaserProj(
		Actor owner, Point pos, int xDir, ushort? netId, 
		bool rpc = false, Player? player = null
	) : base(
		pos, xDir, owner, "sweeping_laser_top", netId, player
	) {

		projId = (int)BassProjIds.SweepingLaser;
		maxTime = 1;
		setIndestructableProperties();
		damager.damage = 3;
		damager.flinch = Global.halfFlinch;
		damager.hitCooldown = 30;
		canBeLocal = false;
		start();

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new SweepingLaserProj(
			arg.owner, arg.pos, arg.xDir, arg.netId, player: arg.player
		);
	}

	void start() {
		startHeight = (int)sprite.getCurrentFrame().rect.h();
		bodySprite = new Sprite("sweeping_laser");
		spriteHeight = (int)bodySprite.getCurrentFrame().rect.h();
		checkGround();

		topAnim = new Anim(pos, sprite.name, xDir, null, false) { visible = false };
		bodyAnim = new Anim(pos, bodySprite.name, xDir, null, false) { visible = false };
		bottomAnim = new Anim(pos, "sweeping_laser_bottom", xDir, null, false) { visible = false };
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) return;

		if (ownerActor is Character chara) {
			changePos(chara.getShootPos());
		}
		checkGround();
		if (ground) groundTime++;

		if (groundTime != lastGroundTime && groundTime % 4 == 0 && ground) {
			new Anim(
				endPos, "dust_purple", xDir, damager.owner.getNextActorNetId(),
				true, true, zIndex: zIndex - 2
			) {
				vel = new Point(0, -60),
				customShaders = getShaders()
			};
			lastGroundTime = groundTime;
		}

		pieces = (MathInt.Floor(pos.distanceTo(endPos)) - startHeight) / spriteHeight;
	}

	public override void render(float x, float y) {
		base.render(x, y);
		if (bodyAnim == null || bodySprite == null) return;
		List<ShaderWrapper> shaders = getShaders();

		for (int i = 0; i < pieces; i++) {
			bodySprite.draw(
				bodyAnim.frameIndex, pos.x, pos.y + ((i * spriteHeight) + startHeight),
				xDir, yDir, null, alpha, 1, 1, zIndex, shaders
			);
		}

		if (ground && bottomAnim != null) {
			Global.sprites[bottomAnim.sprite.name].draw(
				bottomAnim.frameIndex, pos.x + (xDir * 2), endPos.y, 
				xDir, yDir, null, alpha, 1, 1, zIndex, shaders
			);
		}
	}

	public override void onDestroy() {
		base.onDestroy();

		topAnim?.destroySelf();
		bodyAnim?.destroySelf();
		bottomAnim?.destroySelf();
	}

	void checkGround() {
		CollideData? hits = Global.level.raycast(
			pos, pos.addxy(0, (maxPieces * spriteHeight) + startHeight), new List<Type>() { typeof(Wall) }
		);
		if (hits != null) {
			endPos = hits.getHitPointSafe();
			ground = true;
		} else {
			endPos = new Point(pos.x, pos.y + (maxPieces * spriteHeight) + startHeight);
			ground = false;
		}

		List<Point> points = new List<Point>() {
			new Point(6, 0),
			new Point(6, 0),
			new Point(6, endPos.y - pos.y),
			new Point(6, endPos.y - pos.y),
		};
		globalCollider = new Collider(points, true, this, false, false, 0, Point.zero);
	}

	public override List<byte> getCustomActorNetData() {
		return [
			(byte)pieces
		];
	}

	public override void updateCustomActorNetData(byte[] data) {
		pieces = data[0];
	}

	public override List<ShaderWrapper> getShaders() {
		List<ShaderWrapper> shaders = base.getShaders() ?? new();
		if (ownerActor is not Bass bass) {
			return shaders;
		}
		ShaderWrapper? palette = bass.player.superBassPaletteShader;	
		palette?.SetUniform("palette", bass.phase + 1);
		palette?.SetUniform("paletteTexture", Global.textures["bass_superadaptor_palette"]);
		if (palette != null) {
			shaders.Add(palette);
		}
		return shaders;
	}
}

public class DarkCometUpProj : Projectile {
	Actor? actor;
	Anim? anim;
	bool hitWall;

	public DarkCometUpProj(
		Actor owner, Point pos, int xDir,
		ushort? netId, bool rpc = false, Player? player = null
	) : base(
		pos, xDir, owner, "dark_comet", netId, player
	) {
		projId = (int)BassProjIds.DarkCometUp;
		maxTime = 0.5f;
		damager.damage = 3;
		damager.flinch = Global.halfFlinch;
		damager.hitCooldown = 15;
		destroyOnHit = false;

		vel.y = -240;
		yDir *= -1;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
		actor = owner;
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new DarkCometUpProj(
			arg.owner, arg.pos, arg.xDir, arg.netId, player: arg.player
		);
	}

	public override void onStart() {
		base.onStart();
		anim = new Anim(pos, "dark_comet_center", xDir, null, false) { visible = false };
	}

	public override void render(float x, float y) {
		base.render(x,y);
		if (anim != null) {
			Global.sprites[anim.sprite.name].draw(
				anim.frameIndex, pos.x, pos.y + 2,
				xDir, yDir, null, alpha, 1, 1, zIndex, getShaders()
			);
		}
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		if (!ownedByLocalPlayer) return;

		if (other.gameObject is Wall && other.isCeilingHit()) {
			hitWall = true;
			createProjs();
			destroySelf();
			new Anim(
				pos.addxy(0, -28), "dark_comet_land",
				xDir, damager.owner.getNextActorNetId(), true, true
			) {
				yScale = yScale * -1,
				customShaders = getShaders()
			};
			playSound("lightningbolt", sendRpc: true);;
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		if (damagedOnce || !ownedByLocalPlayer || hitWall) return;
		createProjs();
	}

	void createProjs() {
		if (actor == null) return;

		for (int i = -1; i < 2; i++) {
			new DarkCometDownProj(
				actor, pos.addxy(48 * i, 0), xDir,
				damager.owner.getNextActorNetId(), true, damager.owner
			);
		}
	}

	public override List<ShaderWrapper> getShaders() {
		List<ShaderWrapper> shaders = base.getShaders() ?? new();
		if (ownerActor is not Bass bass) {
			return shaders;
		}
		ShaderWrapper? palette = bass.player.superBassPaletteShader;	
		palette?.SetUniform("palette", bass.phase + 1);
		palette?.SetUniform("paletteTexture", Global.textures["bass_superadaptor_palette"]);
		if (palette != null) {
			shaders.Add(palette);
		}
		return shaders;
	}
}


public class DarkCometDownProj : Projectile {
	Anim? anim;

	public DarkCometDownProj(
		Actor owner, Point pos, int xDir,
		ushort? netId, bool rpc = false, Player? player = null
	) : base(
		pos, xDir, owner, "dark_comet", netId, player
	) {
		projId = (int)BassProjIds.DarkCometDown;
		maxTime = 1.5f;
		damager.damage = 3;
		damager.flinch = Global.halfFlinch;
		damager.hitCooldown = 15;
		destroyOnHit = false;

		vel.y = 240;

		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir);
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new DarkCometDownProj(
			arg.owner, arg.pos, arg.xDir, arg.netId, player: arg.player
		);
	}

	public override void onStart() {
		base.onStart();
		anim = new Anim(pos, "dark_comet_center", xDir, null, false) { visible = false };
	}

	public override void onHitWall(CollideData other) {
		base.onHitWall(other);
		if (!ownedByLocalPlayer) return;

		var floor = other.gameObject as Wall;
		Point hitPos = other.hitData.hitPoint ?? pos;

		if (floor != null && other.isGroundHit()) {
			destroySelf();
			new Anim(hitPos, "dark_comet_land", xDir, damager.owner.getNextActorNetId(), true, true) {
				customShaders = getShaders()
			};
		}
	}

	public override void render(float x, float y) {
		base.render(x,y);
		if (anim != null) {
			Global.sprites[anim.sprite.name].draw(
				anim.frameIndex, pos.x, pos.y + 2, 
				xDir, yDir, null, alpha, 1, 1, zIndex, getShaders()
			);
		}
	} 

	public override List<ShaderWrapper> getShaders() {
		List<ShaderWrapper> shaders = base.getShaders() ?? new();
		if (ownerActor is not Bass bass) {
			return shaders;
		}
		ShaderWrapper? palette = bass.player.superBassPaletteShader;	
		palette?.SetUniform("palette", bass.phase + 1);
		palette?.SetUniform("paletteTexture", Global.textures["bass_superadaptor_palette"]);
		if (palette != null) {
			shaders.Add(palette);
		}
		return shaders;
	}
}

#endregion
