namespace MMXOnline;

public abstract class BaselineBoltPickup : Pickup {
	public BaselineBoltPickup(
		Player owner, Point pos, string sprite, ushort? netId,
		bool ownedByLocalPlayer, CActorIds cActorId,
		bool sendRpc = false, bool teamOnly = false
	) : base(
		owner, pos, sprite, netId, ownedByLocalPlayer,
		cActorId, sendRpc: sendRpc, teamOnly: teamOnly
	) {
		pickupType = PickupType.Bolts;
		healAmount = 8;
	}

	public override void use(Character chr) {	
		chr.player.currency += (int)healAmount;
		chr.playSound("bolt");
		base.use(chr);
	}
}

public class GiantBoltPickup : BaselineBoltPickup {
	public GiantBoltPickup(
		Player owner, Point pos, ushort? netId, bool ownedByLocalPlayer,
		bool sendRpc = false, bool teamOnly = false
	) : base(
		owner, pos, "pickup_ammo_giant", netId, ownedByLocalPlayer,
		CActorIds.GiantBoltPickup, sendRpc: sendRpc, teamOnly: teamOnly
	) {
		healAmount = 32;
	}

	public static Actor rpcInvoke(ActorRpcParameters arg) {
		return new GiantBoltPickup(
			arg.player, arg.pos, arg.netId, false, teamOnly: arg.extraData[0] == 1
		);
	}
}

public class LargeBoltPickup : BaselineBoltPickup {
	public LargeBoltPickup(
		Player owner, Point pos, ushort? netId, bool ownedByLocalPlayer,
		bool sendRpc = false, bool teamOnly = false
	) : base(
		owner, pos, "pickup_bolt_large", netId, ownedByLocalPlayer,
		CActorIds.LargeBoltPickup, sendRpc: sendRpc, teamOnly: teamOnly
	) {
		healAmount = 8;
	}

	public static Actor rpcInvoke(ActorRpcParameters arg) {
		return new LargeBoltPickup(
			arg.player, arg.pos, arg.netId, false, teamOnly: arg.extraData[0] == 1
		);
	}
}

public class SmallBoltPickup : BaselineBoltPickup {
	public SmallBoltPickup(
		Player owner, Point pos, ushort? netId, bool ownedByLocalPlayer,
		bool sendRpc = false, bool teamOnly = false
	) : base(
		owner, pos, "pickup_bolt_small", netId, ownedByLocalPlayer,
		CActorIds.SmallBoltPickup, sendRpc: sendRpc, teamOnly: teamOnly
	) {
		healAmount = 2;
	}

	public static Actor rpcInvoke(ActorRpcParameters arg) {
		return new SmallBoltPickup(
			arg.player, arg.pos, arg.netId, false, teamOnly: arg.extraData[0] == 1
		);
	}
}
