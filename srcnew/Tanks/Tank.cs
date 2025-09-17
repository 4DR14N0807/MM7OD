using System;

using MMXOnline;

public class Tank {
	public float ammo;
	public float maxAmmo;
	public float health;
	public float maxHealth;
	public bool once;
	public Tank() {

	}

	public virtual void use(Player player, Character character) {}
	public virtual bool isFull() { return false; }
}
