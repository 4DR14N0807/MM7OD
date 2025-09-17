namespace MMXOnline;


public class ETank : Tank {
	
	public bool inUse;

	public ETank(float maxHealth) {
		this.maxHealth = maxHealth;
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
