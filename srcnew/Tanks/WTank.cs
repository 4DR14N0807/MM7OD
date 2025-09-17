namespace MMXOnline;


public class WTank : Tank {
	public WTank() {}
	
	public override void use(Player player, Character character) {
		character.addWTankAddAmmo(100);
		character.usedWtank = this;
		RPC.useWTank.sendRpc(character.netId, (int)ammo);
	}
}
