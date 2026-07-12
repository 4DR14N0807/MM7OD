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

	// Function calls. Only use what's needed,
	// as that would be whats added to the call stack.
	// If you want to add a new one add the [ItemFunct] tag
	// and add the proper calls in the character.
	[ItemFunct] public GenericLink? onRunning;
	[ItemFunct] public DeathCAct? preDeath;
	[ItemFunct] public DeathCAct? onDeath;
	[ItemFunct] public GenericLink? onRespawn;
	[ItemFunct] public CreateLink? onAttack;
	[ItemFunct] public CreateLink? onMelee;
	[ItemFunct] public CreateLink? onShoot;
	[ItemFunct] public GenericLink? onJump;
	[ItemFunct] public GenericLink? onLand;
	[ItemFunct] public AttackLink? onDamage;
	[ItemFunct] public AttackLink? onApplyDamage;
	[ItemFunct] public AttackLink? onFlinch;
	[ItemFunct] public AttackLink? onApplyFlinch;
	[ItemFunct] public AttackLink? onStun;
	[ItemFunct] public AttackLink? onApplyStun;
	[ItemFunct] public AttackLink? onHealing;
	[ItemFunct] public AttackLink? onApplyHeal;
	[ItemFunct] public KillLink? onKill;
	[ItemFunct] public KillLink? onPickup;

	public virtual void preUpdate(Character chara) {}
	public virtual void update(Character chara) {}
	public virtual void postUpdate(Character chara) {}
}

public enum ChipId {
	None,
	ThunderRevenge
}

public class ItemFunctAttribute : Attribute {
}


public class BaselineCAct<T> : SortedList<ChipId, T> {
}

public class GenericCAct : BaselineCAct<Chip.GenericLink> {
	public void Invoke(Character chara) {
		foreach (Chip.GenericLink action in Values) {
			action(chara);
		}
	}
}

public class BoolCAct : BaselineCAct<Chip.BoolLink> {
	public bool Invoke(Character chara) {
		bool trackVal = false;
		foreach (Chip.BoolLink action in Values) {
			trackVal = action(chara, trackVal);
		}
		return trackVal;
	}
}

public class DeathCAct : BaselineCAct<Chip.DeathLink> {
	public bool Invoke(Character chara, Player? killer, Actor? damager, Character? enemyChar) {
		bool trackVal = false;
		foreach (Chip.DeathLink action in Values) {
			trackVal = action(chara, killer, damager, enemyChar, trackVal);
		}
		return trackVal;
	}
}

public class KillCAct : BaselineCAct<Chip.KillLink> {
	public void Invoke(Character chara, bool isAssist, Player? enemy, Actor? damager, Character? enemyChar) {
		foreach (Chip.KillLink action in Values) {
			action(chara, isAssist, enemy, damager, enemyChar);
		}
	}
}

public class AttackCAct : BaselineCAct<Chip.AttackLink> {
	public float Invoke(Character chara, float val, Actor? damager, Player? enemyPlayer, Character? enemyChar) {
		float trackVal = val;
		foreach (Chip.AttackLink action in Values) {
			trackVal = action(chara, val, damager, enemyPlayer, enemyChar, trackVal);
		}
		return trackVal;
	}
}

public class CreateCAct : BaselineCAct<Chip.CreateLink> {
	public void Invoke(Character chara, Projectile proj) {
		foreach (Chip.CreateLink action in Values) {
			action(chara, proj);
		}
	}
}
