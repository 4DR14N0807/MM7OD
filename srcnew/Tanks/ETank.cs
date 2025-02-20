namespace MMXOnline;


public class ETank {
	public static int maxHealth = 14;
	public float health = 14;
	public bool inUse;

	public ETank() {

	}

	public void use(Player player, Character character) {
		character.usedEtank = this;
		RPC.useETank.sendRpc(character.netId, (int)player.maxHealth);
	}
}
