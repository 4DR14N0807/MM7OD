﻿using System;

namespace MMXOnline;

public class GenericMeleeProj : Projectile {
	public GenericMeleeProj(
		Weapon weapon, Point pos, ProjIds projId, Player player,
		float? damage = null, int? flinch = null, float? hitCooldown = null,
		Actor? ownerActor = null, bool isShield = false, bool isDeflectShield = false, bool isReflectShield = false,
		bool addToLevel = false, float? hitCooldownSeconds = null,
		bool isZSaberEffect = false, bool isZSaberEffect2 = false, bool isZSaberEffect2B = false, bool isZSaberClang = false
	) : base(
		weapon, pos, 1, 0, 2, player, "empty", 0, 0.5f, null, player.ownedByLocalPlayer, addToLevel: addToLevel
	) {
		destroyOnHit = false;
		shouldVortexSuck = false;
		shouldShieldBlock = false;
		this.projId = (int)projId;
		damager.damage = damage ?? weapon.damager.damage;
		damager.flinch = flinch ?? weapon.damager.flinch;
		if (hitCooldown != null) {
			damager.hitCooldown = hitCooldown.Value;
		}
		else if (hitCooldownSeconds != null) {
			damager.hitCooldownSeconds = hitCooldownSeconds.Value;
		}
		else {
			damager.hitCooldown = weapon?.damager?.hitCooldown ?? 0;
		}
		if (hitCooldownSeconds == null && damager.hitCooldown <= 0) {
			damager.hitCooldown = 30;
		}
		this.ownerActor = ownerActor;
		this.xDir = ownerActor?.xDir ?? player.character?.xDir ?? 1;
		this.isShield = isShield;
		this.isDeflectShield = isDeflectShield;
		this.isReflectShield = isReflectShield;
		this.isZSaberEffect = isZSaberEffect;
		this.isZSaberEffect2 = isZSaberEffect2;
		this.isZSaberEffect2B = isZSaberEffect2B;
		this.isZSaberClang = isZSaberClang;
		isMelee = true;
	}

	public override void update() {
		base.update();
	}
	public bool isZSaberEffectBool(bool isEffect2, bool isEffect2B) {
		if (isEffect2) return isZSaberEffect2;
		if (isEffect2B) return isZSaberEffect2B;
		return isZSaberEffect;
	}
	public void charGrabCode(
		CommandGrabScenario scenario, Character? grabber,
		IDamagable? damagable, CharState grabState, CharState grabbedState
	) {
		if (grabber != null && damagable is Character grabbedChar && grabbedChar.canBeGrabbed()) {
			if (!owner.isDefenderFavored) {
				if (ownedByLocalPlayer && !Helpers.isOfClass(grabber.charState, grabState.GetType())) {
					owner.character.changeState(grabState, true);
					if (Global.isOffline) {
						grabbedChar.changeState(grabbedState, true);
					} else {
						RPC.commandGrabPlayer.sendRpc(grabber.netId, grabbedChar.netId, scenario, false);
					}
				}
			} else {
				if (grabbedChar.ownedByLocalPlayer &&
					!Helpers.isOfClass(grabbedChar.charState, grabbedState.GetType())
				) {
					grabbedChar.changeState(grabbedState);
					if (Helpers.isOfClass(grabbedChar.charState, grabbedState.GetType())) {
						RPC.commandGrabPlayer.sendRpc(grabber.netId, grabbedChar.netId, scenario, true);
					}
				}
			}
		}
	}

	public void maverickGrabCode(CommandGrabScenario scenario, Maverick grabber, IDamagable damagable, CharState grabbedState) {
		if (damagable is Character chr && chr.canBeGrabbed()) {
			if (!owner.isDefenderFavored) {
				if (ownedByLocalPlayer && grabber.state.trySetGrabVictim(chr)) {
					if (Global.isOffline) {
						chr.changeState(grabbedState, true);
					} else {
						RPC.commandGrabPlayer.sendRpc(grabber.netId, chr.netId, scenario, false);
					}
				}
			} else {
				if (chr.ownedByLocalPlayer && !Helpers.isOfClass(chr.charState, grabbedState.GetType())) {
					chr.changeState(grabbedState);
					if (Helpers.isOfClass(chr.charState, grabbedState.GetType())) {
						RPC.commandGrabPlayer.sendRpc(grabber.netId, chr.netId, scenario, true);
					}
				}
			}
		}
	}

	public override void onHitDamagable(IDamagable damagable) {
		base.onHitDamagable(damagable);

		if (projId == (int)ProjIds.QuakeBlazer) {
			if (owner.character?.charState is ZeroDownthrust hyouretsuzanState) {
				hyouretsuzanState.quakeBlazerExplode(false);
			}
		}

		// Command grab section
		Character? grabberChar = owner.character;
		Character? grabbedChar = damagable as Character;
		switch (projId) {
			case (int)ProjIds.UPGrab:
				charGrabCode(CommandGrabScenario.UPGrab, grabberChar, damagable, new XUPGrabState(grabbedChar), new UPGrabbed(grabberChar));
				break;
			case (int)ProjIds.VileMK2Grab:
				charGrabCode(CommandGrabScenario.MK2Grab, grabberChar, damagable, new VileMK2GrabState(grabbedChar), new VileMK2Grabbed(grabberChar));
				break;
			case (int)ProjIds.LaunchODrain when ownerActor is LaunchOctopus lo:
				maverickGrabCode(CommandGrabScenario.WhirlpoolGrab, lo, damagable, new WhirlpoolGrabbed(lo));
				break;
			case (int)ProjIds.FStagUppercut when ownerActor is FlameStag fs:
				maverickGrabCode(CommandGrabScenario.FStagGrab, fs, damagable, new FStagGrabbed(fs));
				break;
			case (int)ProjIds.WheelGGrab when ownerActor is WheelGator wg:
				maverickGrabCode(CommandGrabScenario.WheelGGrab, wg, damagable, new WheelGGrabbed(wg));
				break;
			case (int)ProjIds.MagnaCTail when ownerActor is MagnaCentipede ms:
				maverickGrabCode(CommandGrabScenario.MagnaCGrab, ms, damagable, new MagnaCDrainGrabbed(ms));
				break;
			case (int)ProjIds.BoomerangKDeadLift when ownerActor is BoomerangKuwanger bk:
				maverickGrabCode(CommandGrabScenario.DeadLiftGrab, bk, damagable, new DeadLiftGrabbed(bk));
				break;
			case (int)ProjIds.GBeetleLift when ownerActor is GravityBeetle gb:
				maverickGrabCode(CommandGrabScenario.BeetleLiftGrab, gb, damagable, new BeetleGrabbedState(gb));
				break;
			case (int)ProjIds.CrushCGrab when ownerActor is CrushCrawfish cc:
				maverickGrabCode(CommandGrabScenario.CrushCGrab, cc, damagable, new CrushCGrabbed(cc));
				break;
			case (int)ProjIds.BBuffaloDrag when ownerActor is BlizzardBuffalo bb:
				maverickGrabCode(CommandGrabScenario.BBuffaloGrab, bb, damagable, new BBuffaloDragged(bb));
				break;
		}
	}

	public override DamagerMessage? onDamage(IDamagable? damagable, Player? attacker) {	
		return null;
	}
	public override void onDestroy() {
		base.onDestroy();
	}
}
