namespace MMXOnline;


public class ETank {
	public int maxHealth = 14;
	public float health = 14;
	public bool inUse;
	bool once;

	public ETank() {

	}

	public void use(Player player, Character character) {
		if (!once) {
			maxHealth = (int)player.maxHealth;
			health = maxHealth;
			once = true;
		}
		
		character.usedEtank = this;
		RPC.useETank.sendRpc(character.netId, maxHealth);
	}
}
