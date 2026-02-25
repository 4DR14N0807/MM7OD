namespace MMXOnline;


public class ETank : Tank {
	public bool inUse;

	public ETank() {
		maxHealth = 24;
		health = maxHealth;
	}

	public override void use(Player player, Character character) {
		character.usedEtank = this;
		RPC.useETank.sendRpc(character.netId, (int)maxHealth);
	}

	public override bool isFull() {
		return health >= maxHealth;
	}
}
