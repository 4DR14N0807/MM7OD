using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MMXOnline;

public class ChipInventory {
	public SortedList<ChipId, Chip> chips = new();

	public GenericCAct onRunning = [];
	public AttackCAct onDeath = [];
	public GenericCAct onRespawn = [];
	public CreateCAct onAttack = [];
	public CreateCAct onMelee = [];
	public CreateCAct onShoot = [];
	public AttackCAct onJump = [];
	public AttackCAct onDamage = [];
	public AttackCAct onApplyDamage = [];
	public AttackCAct onFlinch = [];
	public AttackCAct onApplyFlinch = [];
	public AttackCAct onStun = [];
	public AttackCAct onApplyStun = [];
	public AttackCAct onHealing = [];
	public AttackCAct onApplyHeal = [];
	public AttackCAct onKill = [];

	public void preUpdate(Character chara) {
		foreach (Chip chip in chips.Values) {
			chip.preUpdate(chara);
		}
	}

	public void update(Character chara) {
		foreach (Chip chip in chips.Values) {
			chip.update(chara);
		}
	}

	public void postUpdate(Character chara) {
		foreach (Chip chip in chips.Values) {
			chip.postUpdate(chara);
		}
	}

	public void addChip(Chip chip) {
		// Add to global chip list.
		chips[chip.id] = chip;

		// To make this not-a-mess we use reflection.
		Type type = chip.GetType();
		Type selfType = GetType();
		foreach (string funcName in Chip.functionNames) {
			dynamic val = type.GetField(funcName).GetValue(chip);
			if (val != null) {
				dynamic target = selfType.GetField(funcName);
				target[chip.id] = val;
			};
		}
	}

	public void removeChip(Chip chip) {
		chips[chip.id] = chip;

		// To make this not-a-mess we use reflection.
		Type type = chip.GetType();
		Type selfType = GetType();
		foreach (string funcName in Chip.functionNames) {
			dynamic val = type.GetField(funcName).GetValue(chip);
			if (val != null) {
				dynamic target = selfType.GetField(funcName);
				target.Remove(chip.id);
			};
		}
	}
}

