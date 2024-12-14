namespace MMXOnline;


public class ETank {
	public const int maxHealth = 28;
	public float health = 28;
	public bool inUse;

	public ETank() {

	}

	public void use(Player player, Character character) {
		//player.health = player.maxHealth;
		character.addETankHealth(health);
		character.usedEtank = this;
		RPC.useETank.sendRpc(character.netId, (int)player.maxHealth);
	}
}
