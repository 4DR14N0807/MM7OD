namespace MMXOnline;

public abstract class BaselineShieldPickup : Pickup {
	public BaselineShieldPickup(
		Player owner, Point pos, string sprite, ushort? netId,
		bool ownedByLocalPlayer, CActorIds cActorId,
		bool sendRpc = false, bool teamOnly = false
	) : base(
		owner, pos, sprite, netId, ownedByLocalPlayer,
		cActorId, sendRpc: sendRpc, teamOnly: teamOnly
	) {
		healAmount = 10;
	}

	public override void use(Character chr) {
		if (!chr.canBeShielded()) {
			return;
		}
		chr.shieldManager.addShield((decimal)healAmount, 60 * 4, ShieldIds.Pickup);
		base.use(chr);
	}
}

public class LargeShieldPickup : BaselineShieldPickup {
	public LargeShieldPickup(
		Player owner, Point pos, ushort? netId, bool ownedByLocalPlayer,
		bool sendRpc = false, bool teamOnly = false
	) : base(
		owner, pos, "pickup_shield_large", netId, ownedByLocalPlayer, 
		CActorIds.LargeAmmoPickup, sendRpc: sendRpc, teamOnly: teamOnly
	) {
		healAmount = 10;
	}

	public static Actor rpcInvoke(ActorRpcParameters arg) {
		return new LargeShieldPickup(
			arg.player, arg.pos, arg.netId, false, teamOnly: arg.extraData[0] == 1
		);
	}
}

public class SmallShieldPickup : BaselineShieldPickup {
	public SmallShieldPickup(
		Player owner, Point pos, ushort? netId, bool ownedByLocalPlayer,
		bool sendRpc = false, bool teamOnly = false
	) : base(
		owner, pos, "pickup_shield_small", netId, ownedByLocalPlayer, 
		CActorIds.SmallAmmoPickup, sendRpc: sendRpc, teamOnly: teamOnly
	) {
		healAmount = 6;
	}

	public static Actor rpcInvoke(ActorRpcParameters arg) {
		return new SmallShieldPickup(
			arg.player, arg.pos, arg.netId, false, teamOnly: arg.extraData[0] == 1
		);
	}
}

public class MicroShieldPickup : BaselineShieldPickup {
	public MicroShieldPickup(
		Player owner, Point pos, ushort? netId, bool ownedByLocalPlayer,
		bool sendRpc = false, bool teamOnly = false
	) : base(
		owner, pos, "pickup_shield_micro", netId, ownedByLocalPlayer, 
		CActorIds.SmallAmmoPickup, sendRpc: sendRpc, teamOnly: teamOnly
	) {
		healAmount = 4;
	}

	public static Actor rpcInvoke(ActorRpcParameters arg) {
		return new MicroShieldPickup(
			arg.player, arg.pos, arg.netId, false, teamOnly: arg.extraData[0] == 1
		);
	}
}
