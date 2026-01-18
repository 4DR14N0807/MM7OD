using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MMXOnline;

// Handles all chips and allows to call them in a easy and deterministic way.
public class ChipInventory {
	// List, it contains all the chips in order.
	public SortedList<ChipId, Chip> chips = [];

	// Invoke-able functiions.
	// Call order is determined by ID, so it's always the same.
	public GenericCAct onRunning = [];
	public DeathCAct preDeath = [];
	public DeathCAct onDeath = [];
	public GenericCAct onRespawn = [];
	public CreateCAct onAttack = [];
	public CreateCAct onMelee = [];
	public CreateCAct onShoot = [];
	public GenericCAct onJump = [];
	public AttackCAct onDamage = [];
	public AttackCAct onApplyDamage = [];
	public AttackCAct onFlinch = [];
	public AttackCAct onApplyFlinch = [];
	public AttackCAct onStun = [];
	public AttackCAct onApplyStun = [];
	public AttackCAct onHealing = [];
	public AttackCAct onApplyHeal = [];
	public KillCAct onKill = [];

	// Called before RPCs and most stuff.
	public void preUpdate(Character chara) {
		foreach (Chip chip in chips.Values) {
			chip.preUpdate(chara);
		}
	}
	
	// Called before rendering.
	public void update(Character chara) {
		foreach (Chip chip in chips.Values) {
			chip.update(chara);
		}
	}

	// Called after collision and rendering.
	public void postUpdate(Character chara) {
		foreach (Chip chip in chips.Values) {
			chip.postUpdate(chara);
		}
	}

	/// Adds a chip and links avaliable functions.
	public void addChip(Chip chip) {
		// Add to global chip list.
		chips[chip.id] = chip;

		// To make this not-a-mess we use reflection.
		Type chipType = chip.GetType();
		Type selfType = GetType();

		// Add each type to an its corresponding list.
		foreach (string funcName in Chip.functionNames) {
			// Use reflection to get function by name.
			dynamic? val = chipType.GetField(funcName)?.GetValue(chip);
			// Stop if function is null.
			if (val == null) { continue; }
			// Get the list with the same name as the function.
			dynamic? target = selfType.GetField(funcName);
			// Crash if a target function does not exist.
			if (target == null) {
				throw new Exception($"Error parsing chip target {selfType.Name}.{funcName}[{chip.id}]");
			}
			// Add the function to the list.
			target[chip.id] = val;
		}
	}

	// Removes a chip and its linked functions.
	public void removeChip(Chip chip) {
		// Get chip ids.
		chips[chip.id] = chip;

		// To make this not-a-mess we use reflection.
		Type chipType = chip.GetType();
		Type selfType = GetType();

		// Check each type and remove if its on one of the lists.
		foreach (string funcName in Chip.functionNames) {
			// Use reflection to get function by name.
			dynamic? val = chipType.GetField(funcName)?.GetValue(chip);
			// Stop if function is null.
			if (val == null) { continue; }
			// Get the list with the same name as the function.
			dynamic? target = selfType.GetField(funcName);
			// Crash if a target function does not exist.
			if (target == null) {
				throw new Exception($"Error parsing chip target {selfType.Name}.{funcName}[{chip.id}]");
			}
			// Remove from the list if target function is found
			target.Remove(chip.id);
		}
	}
}

