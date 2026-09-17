using System;

using MMXOnline;

public class Tank {
	public float ammo;
	public float maxAmmo;
	public decimal health;
	public decimal maxHealth;
	public bool isHealing;
	public float healMaxTime = 120;
    public float healTime;
    public int healStacks;
	public decimal healAmount;
	public Buff? buff;
	public Tank() {

	}

	public virtual bool canUse(Player player, Character character) { return health > 0; }
	public virtual void use(Player player, Character character) {}
	public virtual void heal(Player player, Character character) {}
	public virtual bool isFull() { return health >= maxHealth; }
	public void buffUpdate(Buff self, Character chara) {
		if (chara.shieldManager.shieldsById.TryGetValue(ShieldIds.Tank, out HpShield? value)) {
			self.time = value.time;
		} else {
			self.time = 0;
		}
	}
}
