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
	public delegate bool BoolLink(Character chara, bool trackVal);
	public delegate bool DeathLink(
		Character chara, Player? killer,
		Actor? damager, Character? enemyChar, bool trackVal
	);
	public delegate bool KillLink(
		Character chara, bool isAssist, Player? enemy,
		Actor? damager, Character? enemyChar
	);
	public delegate float AttackLink(
		Character chara, float val, Actor? damager,
		Player? enemyPlayer, Character? enemyChar, float trackVal
	);
	public delegate void CreateLink(Character chara, Projectile proj);

	// Function calls. Only use what's needed.
	public GenericLink? onRunning;
	public DeathCAct? preDeath;
	public DeathCAct? onDeath;
	public GenericLink? onRespawn;
	public CreateLink? onAttack;
	public CreateLink? onMelee;
	public CreateLink? onShoot;
	public GenericLink? onJump;
	public AttackLink? onDamage;
	public AttackLink? onApplyDamage;
	public AttackLink? onFlinch;
	public AttackLink? onApplyFlinch;
	public AttackLink? onStun;
	public AttackLink? onApplyStun;
	public AttackLink? onHealing;
	public AttackLink? onApplyHeal;
	public KillLink? onKill;

	public static string[] functionNames = [
		"onRunning",
		"preDeath",
		"onDeath",
		"onRespawn",
		"onAttack",
		"onMelee",
		"onShoot",
		"onJump",
		"onDamage",
		"onApplyDamage",
		"onFlinch",
		"onApplyFlinch",
		"onStun",
		"onApplyStun",
		"onHealing",
		"onApplyHeal",
		"onKill"
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

public class BoolCAct : SortedList<ChipId, Chip.BoolLink> {
	public bool Invoke(Character chara) {
		bool trackVal = false;
		foreach (Chip.BoolLink action in Values) {
			trackVal = action(chara, trackVal);
		}
		return trackVal;
	}
}

public class DeathCAct : SortedList<ChipId, Chip.DeathLink> {
	public bool Invoke(Character chara, Player? killer, Actor? damager, Character? enemyChar) {
		bool trackVal = false;
		foreach (Chip.DeathLink action in Values) {
			trackVal = action(chara, killer, damager, enemyChar, trackVal);
		}
		return trackVal;
	}
}

public class KillCAct : SortedList<ChipId, Chip.KillLink> {
	public void Invoke(Character chara, bool isAssist, Player? enemy, Actor? damager, Character? enemyChar) {
		foreach (Chip.KillLink action in Values) {
			action(chara, isAssist, enemy, damager, enemyChar);
		}
	}
}

public class AttackCAct : SortedList<ChipId, Chip.AttackLink> {
	public float Invoke(Character chara, float val, Actor? damager, Player? enemyPlayer, Character? enemyChar) {
		float trackVal = val;
		foreach (Chip.AttackLink action in Values) {
			trackVal = action(chara, val, damager, enemyPlayer, enemyChar, trackVal);
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
