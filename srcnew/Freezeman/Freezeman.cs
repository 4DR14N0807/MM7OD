using System;
using System.Collections.Generic;

namespace MMXOnline;

public class Freezeman : Character {
	float speedMod = 1f;
	float speedModTimer;
	Weapon? meleeWeapon = null;
	public float freezeMaxAmmo = 20;
	public float freezeAmmo;
	float freezeAmmoHeal;
	float freezeAmmoHealTime;
	public bool isGuarding;
	public Freezeman(
		Player player, float x, float y, int xDir,
		bool isVisible, ushort? netId, bool ownedByLocalPlayer,
		bool isWarpIn = false
	) : base(
		player, x, y, xDir, isVisible, netId, ownedByLocalPlayer, isWarpIn
	) {
		charId = CharIds.Freezeman;
		spriteFrameToSounds["freezem_run/0"] = "freezemWalk";
		spriteFrameToSounds["freezem_run/4"] = "freezemWalk";

		meleeWeapon = new Weapon();
		freezeAmmoHeal = freezeMaxAmmo;

		changeState(new FreezeMWarpIn(), true);

		addAttackCooldown((int)AttackIds.Attack, new AttackCooldown(1, "hud_weapon_icon", 30));
		addAttackCooldown((int)AttackIds.Freeze, new AttackCooldown(0, "hud_debuffs", 30));
	}

	public override void update() {
		base.update();

		isGuarding = charState is FreezeMGuardState && frameIndex >= 3;

		Helpers.decrementFrames(ref freezeAmmoHealTime);

		if (freezeAmmoHealTime <= 0 && freezeAmmoHeal > 0) {
			freezeAmmo++;
			freezeAmmoHeal--;
			freezeAmmoHealTime = 3;
			playSound("heal", true);

			if (freezeAmmo >= freezeMaxAmmo) {
				freezeAmmo = freezeMaxAmmo;
				freezeAmmoHeal = 0;
			}
		}

		if (groundedIce) {
			speedMod = 2f;
			speedModTimer = 60;
		} else {
			Helpers.decrementFrames(ref speedModTimer);
			if (speedModTimer <= 0) speedMod = Helpers.lerp(speedMod, 1f, 0.025f);
		}
	}

	public override bool attackCtrl() {
		bool attackPressed = player.input.isPressed(Control.Shoot, player);
		bool specialPressed = player.input.isPressed(Control.Special1, player);
		bool weaponLHeld = player.input.isHeld(Control.WeaponLeft, player);
		bool weaponRHeld = player.input.isHeld(Control.WeaponRight, player);

		if (weaponLHeld && freezeAmmo < freezeMaxAmmo) {
			changeState(new FreezeMChargeState(), false);
			return true;
		}

		if (weaponRHeld && freezeAmmo >= 1) {
			changeState(new FreezeMGuardState(), false);
			return true;
		}

		if (specialPressed && freezeAmmo >= 1 && isCooldownOver((int)AttackIds.Freeze)) {
			useFreezeAmmo(3);
			changeState(new FreezeMAttackState(player.input.getInputDir(player), true), false);
			return true;
		}

		if (attackPressed && isCooldownOver((int)AttackIds.Attack)) {
			changeState(new FreezeMAttackState(player.input.getInputDir(player), false), false);
			return true;
		}

		return base.attackCtrl();
	}

	public override Projectile? getProjFromHitbox(Collider hitbox, Point centerPoint) {
		int meleeId = getHitboxMeleeId(hitbox);
		if (meleeId == -1) {
			return null;
		}
		Projectile? proj = getMeleeProjById(meleeId, centerPoint);

		if (proj != null) {
			proj.meleeId = meleeId;
			proj.ownerActor = this;
			updateProjFromHitbox(proj);
			return proj;
		}
		return null;
	}

	public override int getHitboxMeleeId(Collider hitbox) {
		return (int)(sprite.name switch {
			"freezem_attack_down" or "freezem_attack_down_air" => MeleeIds.DownAttack,
			_ => MeleeIds.None
		});
	}

	public override Projectile? getMeleeProjById(int id, Point projPos, bool addToLevel = true) {
		Projectile? proj = id switch {
			(int)MeleeIds.DownAttack => new GenericMeleeProj(
				meleeWeapon ?? new Weapon(), projPos, ProjIds.FreezeMDownAttack,
				player, 1, Global.halfFlinch, 0.5f * 60, addToLevel: addToLevel
			),

			_ => null
		};
		return proj;

	}

	public enum MeleeIds {
		None = -1,
		DownAttack
	}

	public enum AttackIds {
		Attack,
		Freeze,
	}

	public override void renderHUD(Point offset, GameMode.HUDHealthPosition position) {
		base.renderHUD(offset, position);

		Point ammoBarPos = GameMode.getHUDHealthPosition(position, false).add(offset);

		Global.sprites["hud_weapon_base"].drawToHUD(1, ammoBarPos.x, ammoBarPos.y);
		ammoBarPos.y -= 16;

		for (int j = 0; j < freezeMaxAmmo; j++) {
			if (j < freezeAmmo) {
				Global.sprites["hud_weapon_full"].drawToHUD(1, ammoBarPos.x, ammoBarPos.y);
			} else {
				Global.sprites["hud_health_empty"].drawToHUD(0, ammoBarPos.x, ammoBarPos.y);
			}
			ammoBarPos.y -= 2;
		}

		Global.sprites["hud_health_top"].drawToHUD(0, ammoBarPos.x, ammoBarPos.y);
	}

	public override void applyDamage(
		float fDamage, Player? attacker, Actor? actor,
		int? weaponIndex, int? projId
	) {
		if (isGuarding) {

			// Return if not owned.
			if (!ownedByLocalPlayer || fDamage <= 0) {
				return;
			}
			// Apply mastery level before any reduction.
			if (attacker != null && attacker != player && attacker != Player.stagePlayer) {
				if (fDamage < Damager.ohkoDamage) {
					mastery.addDefenseExp(fDamage);
					attacker.mastery.addDamageExp(fDamage, true);
				}
			}
			decimal damage = decimal.Parse(fDamage.ToString());
			decimal originalDamage = damage;
			decimal originalHP = health;

			if (damage > 0 && !Damager.isDot(projId) &&
				attacker != null && attacker != player && attacker != Player.stagePlayer
			) {
				player.delayETank();
				stopETankHeal();
			}

			if (originalHP > 0 && (originalDamage > 0 || damage > 0)) {
				//addDamageTextHelper(attacker, (float)damage, (float)maxHealth, true);
				float damageText = (float)damage;
				int fontColor = (int)FontType.BlueSmall;
				addDamageText(damageText, fontColor);
				RPC.addDamageText.sendRpc(attacker.id, netId, damageText, fontColor);
			}

			// Assist and kill logs.
			if ((originalDamage > 0 || Damager.alwaysAssist(projId)) && attacker != null && weaponIndex != null) {
				damageHistory.Add(new DamageEvent(attacker, weaponIndex.Value, projId, false, Global.time));
			}

			useFreezeAmmo(fDamage);
		} else {
			base.applyDamage(fDamage, attacker, actor, weaponIndex, projId);
		}

	}

	void useFreezeAmmo(float amount) {
		freezeAmmo -= amount;
		if (freezeAmmo < 0) freezeAmmo = 0;
	}

	public void breakIce() {
		if (ownedByLocalPlayer) {
			playSound("freezemcrystalbreak", sendRpc: true);
		}
		for (int i = 0; i < 7; i++) {
			Point spawnPos = getCenterPos().addRand(12, 20);
			int dirX = Helpers.randomRange(0, 1) == 1 ? xDir : -xDir;
			int dirY = Helpers.randomRange(0, 1) == 1 ? yDir : -yDir;
			Point vel = new Point(60 / Helpers.randomRange(1, 2) * dirX, -240 / Helpers.randomRange(1, 2));
			new FreezeMCrystalPiece(
				spawnPos, dirX, dirY, null, vel, vel.times(0.5f), 0
			);
		}
		for (int i = 0; i < 9; i++) {
			Point spawnPos = getCenterPos().addRand(12, 20);
			int dirX = Helpers.randomRange(0, 1) == 1 ? xDir : -xDir;
			int dirY = Helpers.randomRange(0, 1) == 1 ? yDir : -yDir;
			Point vel = new Point(60 / Helpers.randomRange(1, 2), -240 / Helpers.randomRange(1, 2));
			new FreezeMCrystalPiece(
				spawnPos, dirX, dirY, null, vel, vel.times(0.5f), 1
			);
		}
	}

	public override bool canAddAmmo() {
		return freezeAmmo < freezeMaxAmmo;
	}

	public override void addAmmo(float amount) {
		freezeAmmoHeal = amount;
	}
	public override void addPercentAmmo(float amount) {
		freezeAmmoHeal = (freezeMaxAmmo * amount) / 100;
	}

	public override float getRunSpeed() {
		return base.getRunSpeed() * speedMod;
	}

	public override Point getCenterPos() {
		return pos.addxy(0, -27);
	}

	public override (float, float) getGlobalColliderSize() {
		return (35, 48);
	}

	/* public override (float, float) getTerrainColliderSize() {
		return (35, 40);
	} */

	public override bool canDash() {
		return false;
	}

	public override bool canCrouch() {
		return false;
	}

	public override bool canWallClimb() {
		return false;
	}

	public override string getSprite(string spriteName) {
		return "_" + spriteName;
	}

	public override List<byte> getCustomActorNetData() {
		// Get base arguments.
		List<byte> customData = base.getCustomActorNetData();

		// Per-character data.
		customData.Add((byte)MathInt.Ceiling(freezeAmmo));
		bool[] flags = [
			isGuarding,
		];
		customData.Add(Helpers.boolArrayToByte(flags));

		return customData;
	}

	public override void updateCustomActorNetData(byte[] data) {
		// Update base arguments.
		base.updateCustomActorNetData(data);
		data = data[data[0]..];

		// Per-character data.
		freezeAmmo = data[0];
		bool[] flags = Helpers.byteToBoolArray(data[1]);
		isGuarding = flags[0];
	}
}