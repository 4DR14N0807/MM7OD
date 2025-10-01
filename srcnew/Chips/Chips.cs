using System;
using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;

public class Chip {
	// Data stuff.
	public ChipId id;
	public (string name, int frame) sprite;
	public float timer;
	public float stacks;

	// Internal info.
	public int cost;
	public int level;

	// Delegates.
	public delegate void GenericLink(Character chara);
	public delegate float AttackLink(
		Character chara, float val, float trackVal, Actor? damager, Character? enemyChar
	);
	public delegate void CreateLink(Character chara, Projectile proj);

	// Function calls. Only use what's needed.
	public GenericLink onRunning;
	public AttackLink onDeath;
	public GenericLink onRespawn;
	public CreateLink onAttack;
	public CreateLink onMelee;
	public CreateLink onShoot;
	public AttackLink onJump;
	public AttackLink onDamage;
	public AttackLink onApplyDamage;
	public AttackLink onFlinch;
	public AttackLink onApplyFlinch;
	public AttackLink onStun;
	public AttackLink onApplyStun;
	public AttackLink onHealing;
	public AttackLink onApplyHeal;

	public static string[] functionNames = [
		"onRunning",
		"onJump",
		"onDamage",
		"onApplyDamage",
		"onFlinch",
		"onApplyFlinch",
		"onStun",
		"onApplyStun",
		"onHealing",
		"onApplyHeal",
		"onDeath",
		"onRespawn",
		"onAttack",
		"onMelee",
		"onShoot",
	];

	public virtual void preUpdate(Character chara) {}
	public virtual void update(Character chara) {}
	public virtual void postUpdate(Character chara) {}
}

public enum ChipId {
	None
}

public class GenericCAct : SortedList<ChipId, Chip.GenericLink> {
	public void Invoke(Character chara) {
		foreach (Chip.GenericLink action in Values) {
			action(chara);
		}
	}
}

public class AttackCAct : SortedList<ChipId, Chip.AttackLink> {
	public float Invoke(Character chara, float val, Actor damager, Character enemyChar) {
		float trackVal = val;
		foreach (Chip.AttackLink action in Values) {
			trackVal = action(chara, val, trackVal, damager, enemyChar);
		}
		return trackVal;
	}
}

public class CreateCAct : SortedList<ChipId, Chip.CreateLink> {
	public void Invoke(Character chara, Projectile proj) {
		foreach (Chip.CreateLink action in Values) {
			action(chara, proj);
		}
	}
}
