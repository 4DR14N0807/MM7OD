namespace MMXOnline;


public class WTank {

	public const int maxAmmo = 28;
	public float ammo = 28;
	public bool inUse;

	public WTank() {

	}
	public void use(Player player, Character character, int weaponSlot) {
		//character.addAmmo(ammo);
		character.addWTankAddAmmo(weaponSlot, ammo);
		character.usedWtank = this;
		RPC.useWTank.sendRpc(character.netId, (int)player.weapon.maxAmmo);	
	}
}