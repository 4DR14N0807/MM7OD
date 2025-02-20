namespace MMXOnline;

public class LTank {
	public static int maxHealth = 12;
	public float health = 12;
	public static int maxAmmo = 28;
	public float ammo = 28;
	public bool inUse;

	public LTank() {

	}

	public void use(Player player, Character character) {
		if (character is not Blues blues) {
			return;
		}
		blues.usedLtank = this;
	}
}
