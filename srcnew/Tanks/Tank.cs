using System;

using MMXOnline;

public class Tank {
	public float ammo;
	public float maxAmmo;
	public decimal health;
	public decimal maxHealth;
	public bool isHealing;
	public float healMaxTime = 45;
    public float healTime;
	public decimal healAmount;
	public Buff? buff;
	public Tank() {

	}

	public virtual void use(Player player, Character character) {}
	public virtual void heal(Player player, Character character) {}
	public virtual bool isFull() { return health >= maxHealth; }
}
