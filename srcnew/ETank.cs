namespace MMXOnline;


public class ETank {

	public const int maxHealth = 28;
	public float health = 28;
	public bool inUse;

	public ETank() {

	}

	public void use(Player player, Character character) {
		//player.health = player.maxHealth;
		character.addHealth(health);
		character.usedEtank = this;
		RPC.useETank.sendRpc(character.netId, (int)player.maxHealth);	
	}

	public void use(Maverick maverick) {
		maverick.addHealth(health, false);
		maverick.usedETank = this;
		RPC.useETank.sendRpc(maverick.netId, (int)health);
	}
}