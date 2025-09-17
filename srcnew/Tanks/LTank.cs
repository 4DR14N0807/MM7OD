namespace MMXOnline;

public class LTank : Tank {
	

	public LTank(float maxHealth) {
		this.maxHealth = maxHealth;
		this.maxHealth += 28;
		health = this.maxHealth;
	}

	public override void use(Player player, Character character) {
		if (character is not Blues blues) {
			return;
		}
		
		blues.usedLtank = this;
	}

	public override bool isFull() {
		return health >= maxHealth && ammo >= maxAmmo;
	}
}
