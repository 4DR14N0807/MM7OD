﻿using System;
using System.Collections.Generic;
using SFML.Graphics;

namespace MMXOnline;

#region Wolf Sigma Char State
public class WolfSigmaHeadState : CharState {
	bool winTauntOnce;
	Point startPos;
	public WolfSigma sigma;

	public WolfSigmaHeadState() : base("head") {
		immuneToWind = true;
	}

	public override void update() {
		character.stopMoving();
		character.changePos(startPos);
		stateTime += Global.spf;
		if (Global.level.gameMode.isOver && Global.level.gameMode.playerWon(player)) {
			if (!winTauntOnce) {
				winTauntOnce = true;
				sigma.head.tauntTime = Global.spf;
				sigma.head.changeSprite("sigma_wolf_head_taunt", true);
				character.visible = false;
			}
		} else if (
			stateTime > 0.5f && player.input.isPressed(Control.Taunt, player) &&
			sigma.head.shootTime == 0 && sigma.head.tauntTime == 0
		) {
			sigma.head.tauntTime = Global.spf;
			sigma.head.changeSprite("sigma_wolf_head_taunt", true);
			character.visible = false;
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		sigma = character as WolfSigma;
		character.invulnTime = 0.5f;
		character.stopMoving();
		startPos = character.pos;
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
		sigma.leftHand.destroySelf();
		sigma.rightHand.destroySelf();
		var leftHandExplodeEffect = ExplodeDieEffect.createFromActor(player, sigma.leftHand, 10, 3, false);
		var rightHandExplodeEffect = ExplodeDieEffect.createFromActor(player, sigma.rightHand, 10, 3, false);

		leftHandExplodeEffect.silent = Helpers.clamp01(
			sigma.leftHand.pos.distanceTo(sigma.head.pos) / Global.screenW
		) < 1;
		rightHandExplodeEffect.silent = Helpers.clamp01(
			sigma.rightHand.pos.distanceTo(sigma.head.pos) / Global.screenW
		) < 1;

		Global.level.addEffect(leftHandExplodeEffect);
		Global.level.addEffect(rightHandExplodeEffect);
		sigma.head.explode();
	}

	public override bool canExit(Character character, CharState newState) {
		if (newState is not Die) {
			return false;
		}
		return base.canExit(character, newState);
	}
}

public class WolfSigmaFadeInShader {
	public ShaderWrapper shader;
	float fadeTime = 0.2f;

	public WolfSigmaFadeInShader() {
		shader = Helpers.cloneShaderSafe("fadein");
	}

	public void update() {
		if (shader == null) return;

		fadeTime += Global.spf * 0.5f;
		float actualFadeTime;
		if (fadeTime < 0.2f) actualFadeTime = 0;
		else if (fadeTime >= 0.2f && fadeTime < 0.4f) actualFadeTime = 0.2f;
		else if (fadeTime >= 0.4f && fadeTime < 0.6f) actualFadeTime = 0.4f;
		else if (fadeTime >= 0.6f && fadeTime < 0.8f) actualFadeTime = 0.6f;
		else if (fadeTime >= 0.8f && fadeTime < 1f) actualFadeTime = 0.8f;
		else actualFadeTime = 1;
		shader.SetUniform("fadeTime", actualFadeTime);

		if (fadeTime > 1) {
			shader = null;
		}
	}
}
#endregion

#region Wolf Sigma Head
public class WolfSigmaHeadWeapon : Weapon {
	public static WolfSigmaHeadWeapon netWeapon = new();
	public WolfSigmaHeadWeapon() : base() {
		index = (int)WeaponIds.SigmaWolfHead;
		killFeedIndex = 102;
		weaponSlotIndex = 93;
	}
}

public class WolfSigmaHead : Actor, IDamagable {
	public Player owner;
	WolfSigmaFadeInShader fadeinShader;
	public float shootTime;
	public float shootComponentX;
	public int BallsShot, FlamesShot;
	public float shootXDir;
	public bool isBall;
	public float explodeTime;
	float projTime;
	float flameSoundTime;
	public float tauntTime;
	Point startPos;
	public bool isControlling {
		get {
			return owner.weapon is WolfSigmaHeadWeapon w;
		}
	}

	public WolfSigmaHead(Point pos, Player player, ushort netId, bool ownedByLocalPlayer, bool rpc = false) :
		base("", pos, netId, ownedByLocalPlayer, false) {
		useGravity = false;
		this.owner = player;
		xDir = 1;
		zIndex = ZIndex.Character - 3;
		fadeinShader = new WolfSigmaFadeInShader();
		frameSpeed = 0;
		frameIndex = 0;

		netOwner = player;
		netActorCreateId = NetActorCreateId.WolfSigmaHead;
		if (rpc) {
			createActorRpc(player.id);
		}
	}

	public override void onStart() {
		base.onStart();
		startPos = pos;
	}

	public override List<ShaderWrapper> getShaders() {
		if (fadeinShader.shader == null) return null;
		return new List<ShaderWrapper>()
		{
				fadeinShader.shader
			};
	}

	public override void preUpdate() {
		base.preUpdate();
		updateProjectileCooldown();
	}

	public override void update() {
		base.update();

		stopMoving();
		changePos(startPos);

		if (explodeTime > 0) {
			frameIndex = 2;
			explodeTime += Global.spf;
			visible = Global.isOnFrameCycle(5);

			if (explodeTime > 3) {
				destroySelf();
				return;
			}

			return;
		}

		fadeinShader.update();
		if (!ownedByLocalPlayer) return;
		if (BallsShot > 9) BallsShot = 0;
		if (FlamesShot > 64) FlamesShot = 0;

		if (tauntTime > 0) {
			if (sprite.loopCount > 3) {
				tauntTime = 0;
				changeSprite("sigma_wolf_head", true);
				frameSpeed = 0;
				frameIndex = 0;
				owner.character.visible = true;
			}
			return;
		}
		if (shootTime > 0) {
			shootTime += Global.spf;
			if (shootTime < 0.5f) {
				frameIndex = Global.isOnFrameCycle(5) ? 1 : 0;
			} else if (shootTime >= 0.5f) {
				frameIndex = 0;
				shootComponentX += Global.spf * shootXDir;
				projTime += Global.spf;
				Point dir = new Point(shootComponentX, 1);
				if (isBall) {
					if (projTime > 0.2f) {
						projTime = 0;
						BallsShot++;
						new WolfSigmaBall(pos.addxy(0, 30), 1, BallsShot,
						this, owner, owner.getNextActorNetId(), rpc: true);
					}
				} else {
					flameSoundTime += Global.spf;
					if (flameSoundTime > 0.2f) {
						flameSoundTime = 0;
						//playSound("fireWave");
					}
					if (projTime > 0.06f) {
						projTime = 0;
						FlamesShot++;
						new WolfSigmaFlame(pos.addxy(0, 30), 1, FlamesShot,
						this, owner, owner.getNextActorNetId(), rpc: true);
					}
				}

				if ((shootXDir == 1 && shootComponentX >= 1) || (shootXDir == -1 && shootComponentX <= -1)) {
					shootTime = 0;
				}
			}

			return;
		}

		if (isControlling) {
			// Attack inputs go here
			if (owner.input.isPressed(Control.Shoot, owner) || owner.input.isPressed(Control.Special1, owner)) {
				shootTime = Global.spf;
				isBall = owner.input.isPressed(Control.Shoot, owner);
				shootComponentX = -1;
				shootXDir = 1;
				if (owner.input.isHeld(Control.Right, owner)) {
					shootComponentX = 1;
					shootXDir = -1;
				}
			}
		}

	}

	public void applyDamage(float damage, Player? owner, Actor? actor, int? weaponIndex, int? projId) {
	}

	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) {
		return owner.alliance != damagerAlliance;
	}

	public bool isInvincible(Player attacker, int? projId) {
		return true;
	}

	public bool canBeHealed(int healerAlliance) {
		return false;
	}

	public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) {
	}

	public override void render(float x, float y) {
		base.render(x, y);
		if (!visible) return;
		if (isControlling && explodeTime == 0) {
			// Global.sprites["cursorchar"].draw(0, pos.x + x, pos.y + y - 25, 1, 1, null, 1, 1, 1, ZIndex.HUD);
		}
		Global.sprites["sigma_wolf_body"].draw(0, pos.x + x, pos.y + y + 57, 1, 1, null, alpha, 1, 1, ZIndex.Backwall + 100, getShaders());
	}

	public void explode() {
		if (explodeTime == 0) {
			explodeTime = Global.spf;
		}
	}

	public bool isPlayableDamagable() {
		return false;
	}
}

public class WolfSigmaBall : Projectile {
	bool once;
	public int type = 0;
	public WolfSigmaBall(
		Point pos, int xDir, int type, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "ws_proj_ball", netId, player	
	) {
		weapon = WolfSigmaHeadWeapon.netWeapon;
		damager.damage = 6;
		damager.hitCooldown = 30;
		damager.flinch = Global.defFlinch;
		projId = (int)ProjIds.SigmaWolfHeadBallProj;
		this.type = type;
		destroyOnHit = false;
		maxTime = 0.5f;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new WolfSigmaBall(
			args.pos, args.xDir, args.extraData[0], args.owner, args.player, args.netId
		);
	}

	public override void update() {
		if (frameIndex > 3 && !once) {
			once = true;
			playSound("energyBall");
			int x = 450 - 100 * type;
			if (type <= 9) vel = new Point(x, 300);		
		}
		base.update();
	}
}

public class WolfSigmaFlame : Projectile {
	bool once;
	public int type = 0;
	public WolfSigmaFlame(
		Point pos, int xDir, int type, Actor owner, Player player, ushort? netId, bool rpc = false
	) : base(
		pos, xDir, owner, "ws_proj_flame", netId, player	
	) {
		weapon = WolfSigmaHeadWeapon.netWeapon;
		damager.damage = 3;
		damager.hitCooldown = 15;
		projId = (int)ProjIds.SigmaWolfHeadFlameProj;
		destroyOnHit = false;
		maxTime = 0.35f;
		this.type = type;
		if (rpc) {
			rpcCreate(pos, owner, ownerPlayer, netId, xDir, (byte)type);
		}
	}
	public static Projectile rpcInvoke(ProjParameters args) {
		return new WolfSigmaFlame(
			args.pos, args.xDir, args.extraData[0], args.owner, args.player, args.netId
		);
	}

	public override void update() {
		if (!once) {
			once = true;
			int x = 450 - 25 * type;
			if (type <= 64) vel = new Point(x, 300);		
		}
		base.update();
	}
}

#endregion

#region Wolf Sigma Hand
public class WolfSigmaHandWeapon : Weapon {
	public WolfSigmaHand hand;
	public WolfSigmaHandWeapon(Player player, WolfSigmaHand hand) : base() {
		index = (int)WeaponIds.SigmaWolfHand;
		damager = new Damager(player, 6, Global.defFlinch, 1f);
		killFeedIndex = 105;
		weaponSlotIndex = 94;
		this.hand = hand;
		hand.weapon = this;
	}
}

public class WolfSigmaHand : Actor, IDamagable {
	WolfSigmaFadeInShader fadeinShader;
	public Player owner;
	public Point origPos;
	public Point lungeStartPos;
	public Point origSpinCenterPos;
	public const float spinRadius = 25;
	public bool movedOnce;
	public bool isLeft;
	public float moveAngle;
	public bool startMoving;

	public float beamTime;
	public bool beamShot;
	public bool beamLetGo;

	public Point lungeDir;
	public float lungeTime;
	public float lungeReturnTime;
	public const float lungeSpeed = 300;
	public const float moveSpeed = 200;
	public static float maxDistFromHeadX = Global.screenW * 2;
	public static float maxDistFromHeadY = Global.screenW * 2;
	public float offScreenTime;
	public float maxOffscreenTime = 16;

	public bool isControlling {
		get {
			return owner.weapon is WolfSigmaHandWeapon w && w.hand == this;
		}
	}
	public WolfSigmaHandWeapon weapon;
	public Anim beamMuzzle1;
	public Anim beamMuzzle2;

	public WolfSigmaHand(Point pos, Player player, bool isLeft, ushort netId, bool ownedByLocalPlayer, bool rpc = false) :
		base("sigma_wolf_hand", pos, netId, ownedByLocalPlayer, false) {
		useGravity = false;
		owner = player;
		xDir = 1;
		origPos = pos;
		origSpinCenterPos = pos.addxy(0, spinRadius);
		if (player?.character != null) {
			zIndex = player.character.zIndex + 10;
		}
		this.isLeft = isLeft;
		frameSpeed = 0;
		//wall = new WolfSigmaHandWall(pos);
		fadeinShader = new WolfSigmaFadeInShader();
		startMoving = true;
		useFrameProjs = true;
		isPlatform = true;

		netOwner = player;
		netActorCreateId = NetActorCreateId.WolfSigmaHand;
		if (rpc) {
			createActorRpc(player.id);
		}
	}

	public override void preUpdate() {
		base.preUpdate();
		updateProjectileCooldown();
	}

	public override void update() {
		base.update();
		fadeinShader.update();

		if (beamMuzzle1 != null && beamMuzzle1.destroyed) beamMuzzle1 = null;
		if (beamMuzzle2 != null && beamMuzzle2.destroyed) beamMuzzle2 = null;

		if (!ownedByLocalPlayer) return;
		if (!startMoving) return;
		if (owner == null) return;

		if (owner.character != null) {
			if (pos.distanceTo(owner.character.getCenterPos()) > Global.screenW * 0.75f) {
				float mod = 1;
				if (beamTime > 0) mod = 5;
				if (lungeTime > 0) mod = 4;
				offScreenTime += Global.spf * mod;
				if (offScreenTime > maxOffscreenTime) offScreenTime = maxOffscreenTime;
			} else if (offScreenTime < maxOffscreenTime) {
				offScreenTime -= Global.spf * 2.5f;
				if (offScreenTime < 0) offScreenTime = 0;
			}
		}

		if (lungeTime > 0) {
			lungeTime += Global.spf;
			frameSpeed = 1;
			if (lungeTime < 0.25f) {
				move(lungeDir.times(lungeSpeed));
				movedOnce = true;
			} else if (lungeTime >= 0.75f) {
				frameSpeed = 0;
				frameIndex = 0;
				frameTime = 0;
				lungeTime = 0;
				lungeReturnTime = Global.spf;
			}

			return;
		} else if (lungeReturnTime > 0) {
			if (!isControlling) {
				lungeReturnTime = 0;
				return;
			}

			lungeReturnTime += Global.spf;

			float returnSpeed = 200;
			Point moveAmount = pos.directionTo(lungeStartPos).normalize().times(Global.spf * returnSpeed);
			move(moveAmount, false);
			if (pos.distanceTo(lungeStartPos) < Global.spf * returnSpeed * 2) {
				changePos(lungeStartPos);
				movedOnce = true;
				lungeReturnTime = 0;
			}

			return;
		} else if (beamTime > 0) {
			const float beamDuration = 0.4f;

			/*
			if (!owner.input.isHeld(Control.Special1, owner) && beamTime < beamDuration)
			{
				if (beamTime < 0.3f) beamDamage = 6;
				else if (beamTime < beamDuration) beamDamage = 10;
				beamLetGo = true;
				beamMuzzle1?.destroySelf();
				beamMuzzle2?.destroySelf();
			}
			*/

			beamTime += Global.spf;
			frameSpeed = 1;
			if ((beamTime > beamDuration || beamLetGo) && !beamShot) {
				//playSound("WolfSigmaThunderX1", sendRpc: true);
				beamShot = true;
				new WolfSigmaBeam(new WolfSigmaBeamWeapon(), pos.addxy(0, -10), 1, -1, 0, owner, owner.getNextActorNetId(), rpc: true);
				new WolfSigmaBeam(new WolfSigmaBeamWeapon(), pos.addxy(0, 10), 1, 1, 0, owner, owner.getNextActorNetId(), rpc: true);
			}
			if (beamTime > 1) {
				frameIndex = 0;
				frameSpeed = 0;
				frameTime = 0;
				beamTime = 0;
				beamShot = false;
				beamLetGo = false;
			}

			return;
		}

		if (isControlling && offScreenTime < maxOffscreenTime) {
			if (owner.input.isPressed(Control.Shoot, owner)) {
				lungeTime = Global.spf;
				lungeStartPos = pos;
				lungeDir = owner.input.getInputDir(owner);
				if (lungeDir.isZero()) lungeDir.y = 1;
				return;
			} else if (owner.input.isPressed(Control.Special1, owner)) {
				beamTime = Global.spf;
				beamMuzzle1 = new Anim(pos.addxy(0, -10), "ws_proj_beam_muzzle", 1, owner.getNextActorNetId(), true, sendRpc: true);
				beamMuzzle2 = new Anim(pos.addxy(0, 10), "ws_proj_beam_muzzle", 1, owner.getNextActorNetId(), true, sendRpc: true) { yDir = -1 };

				return;
			}

			Point moveAmount = owner.input.getInputDir(owner);
			if (!moveAmount.isZero()) {
				movedOnce = true;
			}

			moveAmount = moveAmount.normalize().times(Global.spf * moveSpeed);
			move(moveAmount, false);
			if (pos.x < 0) changePos(new Point(0, pos.y));
			if (pos.x > Global.level.width) changePos(new Point(Global.level.width, pos.y));
			if (pos.y < 0) changePos(new Point(pos.x, 0));
			if (pos.y > Global.level.height) changePos(new Point(pos.x, Global.level.height));

			if (Global.level.gameMode is CTF || Global.level.gameMode is ControlPoints || Global.level.gameMode is KingOfTheHill) {
				Point cPos = owner.character.getCenterPos();
				float xDiff = pos.x - cPos.x;
				float yDiff = pos.y - cPos.y;
				if (MathF.Abs(xDiff) > maxDistFromHeadX) {
					changePos(new Point(cPos.x + (maxDistFromHeadX * MathF.Sign(xDiff)), pos.y));
				}
				if (MathF.Abs(yDiff) > maxDistFromHeadY) {
					changePos(new Point(pos.x, cPos.y + (maxDistFromHeadY * MathF.Sign(yDiff))));
				}
			}
		} else {
			if (movedOnce) {
				float returnSpeed = 400;
				Point moveAmount = pos.directionTo(origPos).normalize().times(Global.spf * returnSpeed);
				move(moveAmount, false);
				if (pos.distanceTo(origPos) < Global.spf * returnSpeed * 2) {
					changePos(origPos);
					movedOnce = false;
					moveAngle = 0;
					offScreenTime = 0;
				}
			} else {
				moveAngle += Global.spf * 300;
				if (moveAngle > 360) moveAngle -= 360;

				Point moveAmount = new Point(
					spinRadius * Helpers.cosd(moveAngle - 90) * (isLeft ? 1 : -1),
					spinRadius * Helpers.sind(moveAngle - 90)
				);
				changePos(origSpinCenterPos.add(moveAmount));
			}
		}
	}

	public override Projectile getProjFromHitbox(Collider hitbox, Point centerPoint) {
		if (hitbox.flag == (int)HitboxFlag.Hitbox) {
			Weapon handWeapon = weapon ?? new WolfSigmaHandWeapon(owner, this);
			return new GenericMeleeProj(handWeapon, centerPoint, ProjIds.SigmaHand, owner);
		}
		return null;
	}

	public override void updateProjFromHitbox(Projectile proj) {
		if (canHandDamage()) {
			proj.damager.damage = weapon.damager.damage;
		} else {
			proj.damager.damage = 0;
		}
	}

	public bool canHandDamage() {
		return (frameIndex >= 1 && deltaPos.magnitude > 1);
	}

	public override void onDestroy() {
		base.onDestroy();
	}

	public override List<ShaderWrapper> getShaders() {
		if (fadeinShader.shader == null) return null;
		return new List<ShaderWrapper>()
		{
				fadeinShader.shader
			};
	}

	public void applyDamage(float damage, Player? owner, Actor? actor, int? weaponIndex, int? projId) {
	}

	public bool canBeDamaged(int damagerAlliance, int? damagerPlayerId, int? projId) {
		return owner.alliance != damagerAlliance;
	}

	public bool isInvincible(Player attacker, int? projId) {
		return true;
	}

	public bool canBeHealed(int healerAlliance) {
		return false;
	}

	public void heal(Player healer, float healAmount, bool allowStacking = true, bool drawHealText = false) {
	}

	public override void render(float x, float y) {
		base.render(x, y);
		if (isControlling) {
			Global.sprites["cursor"].draw(0, pos.x + x, pos.y + y - 25, 1, 1, null, 1, 1, 1, ZIndex.HUD);
		}
		if (offScreenTime > 0) {
			float width = Helpers.progress(offScreenTime, maxOffscreenTime) * 30;
			float offY = -15;
			Point topLeft = new Point(pos.x - 16, pos.y - 5 + offY);
			Point botRight = new Point(pos.x + 16, pos.y + offY);

			//DrawWrappers.DrawText("Possessing...", pos.x, pos.y - 58, Alignment.Center, true, 0.75f, Color.White, Helpers.getAllianceColor(), Text.Styles.Regular, 1, true, ZIndex.HUD);

			DrawWrappers.DrawRect(topLeft.x, topLeft.y, botRight.x, botRight.y, true, Color.Black, 0, ZIndex.HUD - 1, outlineColor: Color.White);
			DrawWrappers.DrawRect(topLeft.x + 1, topLeft.y + 1, topLeft.x + 1 + width, botRight.y - 1, true, Color.Yellow, 0, ZIndex.HUD - 1);
		}
	}

	public bool isPlayableDamagable() {
		return false;
	}
}

public class WolfSigmaBeamWeapon : Weapon {
	public WolfSigmaBeamWeapon() {
		index = (int)WeaponIds.SigmaWolfHandBeam;
		killFeedIndex = 105;
	}
}

public class WolfSigmaBeam : Projectile {
	Point origin;
	bool hitWall;
	const float beamMidHeight = 16;
	const float beamEndHeight = 14;
	int type = 0;
	int frameCount;
	float newFrameTime;
	int newFrameIndex;
	public WolfSigmaBeam(Weapon weapon, Point pos, int xDir, int yDir, int type, Player player, ushort netProjId, bool rpc = false) :
		base(weapon, pos, xDir, 800, 8, player, type == 0 ? "ws_proj_beam" : "wsponge_thunder_point", Global.defFlinch, 0.5f, netProjId, player.ownedByLocalPlayer) {
		this.yDir = yDir;
		this.type = type;
		origin = pos;
		if (type == 0) {
			projId = (int)ProjIds.SigmaHandElecBeam;
			frameCount = 2;
		} else {
			projId = (int)ProjIds.WSpongeLightning;
			frameCount = 4;
			fadeSprite = "wsponge_thunder_fade";
			startSound = "wspongeThunder";
			fadeOnAutoDestroy = true;
			damager.damage = 8;
			damager.hitCooldown = 9;
		}

		if (type == 0) maxTime = 0.5f;
		else if (type == 1) maxDistance = 100;
		else if (type == 2) maxTime = 0.5f;

		destroyOnHit = false;
		shouldShieldBlock = false;
		frameSpeed = 0;
		vel = new Point(0, yDir * speed);

		if (!ownedByLocalPlayer) {
			maxTime = float.MaxValue;
			maxDistance = float.MaxValue;
		}

		if (rpc) {
			rpcCreate(pos, player, netProjId, xDir);
		}
	}

	public override void update() {
		base.update();

		newFrameTime += Global.spf;
		if (newFrameTime > 0.03f) {
			newFrameTime = 0;
			newFrameIndex++;
			if (newFrameIndex >= frameCount) newFrameIndex = 0;
		}

		int pieceCount = getPiecesThatFit();

		var rect = collider._shape.getRect();
		if (yDir == 1) rect.y1 = (pieceCount + 1) * -14;
		else {
			rect.y1 = -14;
			rect.y2 = pieceCount * 14;
		}
		collider._shape = rect.getShape();

		// Wolf sigma
		if (type == 0) {
			if (pieceCount >= 6 && !hitWall) {
				hitWall = true;
				vel.y = 0;
			}

			if (hitWall) {
				frameIndex = 1;
				origin.y += Global.spf * speed * yDir;
				if ((yDir == 1 && origin.y >= pos.y) || (yDir == -1 && origin.y <= pos.y)) {
					frameIndex = 2;
					destroySelf();
				}
			}
		}
		// Wire sponge
		else if (ownedByLocalPlayer) {
			if (pieceCount >= 6 && !hitWall) {
				var hit = Global.level.checkCollisionShape(new Rect(pos.x - 5, pos.y, pos.x + 5, pos.y + 10).getShape(), null);
				if (hit != null) {
					hitWall = true;
					vel.y = 0;
					changePos(hit.getHitPointSafe().addxy(0, -8));
					destroySelf();
					return;
				}
			}
		}
	}

	public override void render(float x, float y) {
		base.render(x, y);
		string midSprite = "ws_beam_mid";
		string startSprite = "ws_beam_start";
		if (type != 0) {
			midSprite = "wsponge_thunder_mid";
			startSprite = "";
		}

		int piecesThatFit = getPiecesThatFit();
		int i;
		for (i = 0; i < piecesThatFit; i++) {
			Global.sprites[midSprite].draw(newFrameIndex, pos.x, pos.y - (i * beamMidHeight * yDir), 1, 1, null, 1, 1, 1, zIndex - 100);
		}
		if (!string.IsNullOrEmpty(startSprite)) {
			if (yDir == -1) i -= 1;
			Global.sprites[startSprite].draw(newFrameIndex, pos.x, pos.y - (i * beamMidHeight * yDir), 1, yDir, null, 1, 1, 1, zIndex - 100);
		}
	}

	private int getPiecesThatFit() {
		float yDistFromOrigin = MathF.Abs(pos.y - origin.y);
		int piecesThatFit = MathInt.Floor(yDistFromOrigin / beamMidHeight);
		if (piecesThatFit > 6) piecesThatFit = 6;
		return piecesThatFit;
	}
}

#endregion

#region wolf sigma revive
public class WolfSigmaRevive : CharState {
	public int state;
	public ExplodeDieEffect explodeDieEffect;
	public bool groundStart;
	Point destPos;
	public WolfSigma sigma;

	float speed = 1;

	public WolfSigmaRevive(ExplodeDieEffect explodeDieEffect) : base("head_intro") {
		this.explodeDieEffect = explodeDieEffect;
	}

	public override void update() {
		base.update();

		speed = 1;
		if (Global.debug && Global.input.isHeld(Control.Special1, player)) {
			speed = 10;
			stateTime += Global.spf * speed;
		}

		if (state == 0) {
			if (explodeDieEffect == null || explodeDieEffect.destroyed) {
				state = 1;
			}
		} else if (state == 1) {
			character.visible = true;
			if (character.grounded || groundStart) {
				sigma.sigmaHeadGroundCamCenterPos = character.getCamCenterPos();
				state = 2;
				stateTime = 0;
			} else {
				character.useGravity = true;
				character.immuneToKnockback = false;
			}
		} else if (state == 2) {
			if (stateTime > 1) {
				character.immuneToKnockback = true;
				character.useGravity = false;
				character.grounded = false;
				character.collider.isTrigger = true;

				float destY = Math.Max(character.pos.y - 140, Global.level.getTopScreenY(character.pos.y) + 30);
				int headH = 20;

				var hit = Global.level.raycast(character.pos.addxy(0, -5), new Point(character.pos.x, Global.level.getTopScreenY(character.pos.y)), new List<Type>() { typeof(Wall) });
				if (hit != null) {
					destPos = new Point(character.pos.x, Math.Max(destY, hit.getHitPointSafe().y + headH));
				} else {
					destPos = new Point(character.pos.x, destY);
				}

				stateTime = 0;
				state = 3;

				character.addMusicSource("wolfSigmaIntro", character.pos.addxy(0, -75), false, loop: false);
				RPC.actorToggle.sendRpc(character.netId, RPCActorToggleType.AddWolfSigmaIntroMusicSource);

			}
		} else if (state == 3) {
			if (character.pos.y > destPos.y) {
				character.incPos(new Point(0, -50 * Global.spf * speed));
			} else {
				character.changePos(destPos);
				if (stateTime >= 2.9f) {
					stateTime = 0;
					state = 4;
				}
			}
		} else if (state == 4) {
			if (stateTime > 1.2 && sigma.head == null) {
				sigma.head = new WolfSigmaHead(destPos, player, player.getNextActorNetId(), true, rpc: true);
				sigma.leftHand = new WolfSigmaHand(destPos.addxy(-85, 25), player, true, player.getNextActorNetId(), true, rpc: true);
				sigma.rightHand = new WolfSigmaHand(destPos.addxy(85, 25), player, false, player.getNextActorNetId(), true, rpc: true);
			}
			if (stateTime > 4) {
				character.frameSpeed = 1;
			}
			if (stateTime > 4.5f) {
				player.health = 1;
				character.addHealth(player.maxHealth);
				state = 5;
			}
		} else if (state == 5) {
			if (player.health >= player.maxHealth) {
				character.weapons.Add(new WolfSigmaHandWeapon(player, sigma.leftHand));
				character.weapons.Add(new WolfSigmaHeadWeapon());
				character.weapons.Add(new WolfSigmaHandWeapon(player, sigma.rightHand));
				character.weaponSlot = 1;

				character.changeState(new WolfSigmaHeadState(), true);
				character.addMusicSource("wolfSigmaIntro", character.pos.addxy(0, -75), false, loop: false);
				RPC.actorToggle.sendRpc(character.netId, RPCActorToggleType.AddWolfSigmaMusicSource);
			}
		}
	}

	public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		sigma = character as WolfSigma;
		character.stopMoving();
		character.visible = false;
		character.useGravity = false;
		character.frameSpeed = 0;
		character.immuneToKnockback = true;

		Global.level.addToGrid(character);

		/*
		Point? groundPos = Global.level.getGroundPosWithNull(character.pos, 30);
		if (groundPos != null)
		{
			character.changePos(groundPos.Value);
			groundStart = true;
		}
		*/
		groundStart = false;
	}
}
#endregion
