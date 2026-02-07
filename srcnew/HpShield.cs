using System;
using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;

public class HpShieldManager() {
	public List<HpShield> shields = [];
	public Dictionary<ShieldIds, HpShield> shieldsById = [];

	public decimal totalHealth {
		get {
			decimal hp = 0;
			foreach (HpShield shield in shields) {
				hp += shield.health;
			}
			return hp;
		}
	}

	public void update(Character chara) {
		foreach (HpShield shield in shields) {
			shield.update(chara);
		}
		shortShields();
	}

	public void shortShields() {
		shields = shields.Where(s => !s.destroyed).OrderBy(s => s.piority).ToList();
		shieldsById = shieldsById.Where(kvp => !kvp.Value.destroyed).ToDictionary();
	}

	public decimal applyDamage(decimal damage) {
		shortShields();
		int i = shields.Count - 1;

		while (i >= 0 && damage > 0) {
			damage = shields[i].applyDamage(damage);
			if (shields[i].health <= 0) {
				shieldsById.Remove(shields[i].id);
				shields.RemoveAt(i);
			}
		}
		return damage;
	}

	public void addShield(float health, int time, ShieldIds id) {
		addShield((decimal)health, time, id);
	}

	public void addShield(decimal health, int time, ShieldIds id) {
		HpShield? shield = shields.FirstOrDefault(shield => shield.id == id);
		if (shield != null) {
			if (shield.health < health) {
				shield.health = health;
			}
			return;
		}
		var newShield = new HpShield() {
			health = health,
			time = time,
			id = id
		};
		shields.Add(newShield);
		shieldsById[id] = newShield;
		shortShields();
	}
}

public class HpShield {
	public decimal health;
	public float time;
	public int piority;
	public ShieldIds id;
	public bool destroyed;

	public decimal applyDamage(decimal damage) {
		if (damage > health) {
			decimal retDamage = damage - health;
			health = 0;
			time = 0;
			return retDamage;
		}
		health -= damage;
		return 0;
	}

	public void update(Character chara) {
		if (time < 0) {
			return;
		}
		time -= chara.speedMul;
		if (time < 0) {
			time = 0;
			destroyed = true;
		}
	}

	public virtual void render(float x, float y) { }
}

public enum ShieldIds {
	None,
	Pickup,
}
