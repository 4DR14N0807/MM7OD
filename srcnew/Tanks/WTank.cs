namespace MMXOnline;


public class WTank {

	public const int maxAmmo = 28;
	public float ammo = 28;
	public bool inUse;

	public WTank() {

	}
	public void use(Player player, Character character, float amount) {
		//character.addAmmo(ammo);
		ammo = amount;
		character.addWTankAddAmmo(ammo);
		character.usedWtank = this;
		RPC.useWTank.sendRpc(character.netId, (int)ammo);
	}
}
