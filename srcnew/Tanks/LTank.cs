namespace MMXOnline;

public class LTank {
	public static int maxHealth = 12;
	public float health = 12;
	public static int maxAmmo = 28;
	public float ammo = 28;
	public bool inUse;
	public bool once;

	public LTank() {

	}

	public void use(Player player, Character character) {
		if (character is not Blues blues) {
			return;
		}
		if (!once) {
			maxHealth = (int)player.maxHealth;
			health = maxHealth;
			once = true;
		}

		blues.usedLtank = this;
	}
}
