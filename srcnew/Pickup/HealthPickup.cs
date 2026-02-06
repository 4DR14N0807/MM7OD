namespace MMXOnline;

public abstract class BaselineHealthPickup : Pickup {
	public BaselineHealthPickup(
		Player owner, Point pos, string sprite, ushort? netId,
		bool ownedByLocalPlayer, CActorIds cActorId,
		bool sendRpc = false, bool teamOnly = false
	) : base(
		owner, pos, sprite, netId, ownedByLocalPlayer,
		cActorId, sendRpc: sendRpc, teamOnly: teamOnly
	) {
		pickupType = PickupType.Health;
		healAmount = 8;
	}

	public override void use(Character chr) {
		if (!chr.canBeHealed()) {
			return;
		}
		chr.addHealth(healAmount);
		base.use(chr);
	}
}

public class GiantHealthPickup : BaselineHealthPickup {
	public GiantHealthPickup(
		Player owner, Point pos, ushort? netId, bool ownedByLocalPlayer,
		bool sendRpc = false, bool teamOnly = false
	) : base(
		owner, pos, "pickup_health_giant", netId, ownedByLocalPlayer,
		CActorIds.GiantHealthPickup, sendRpc: sendRpc, teamOnly: teamOnly
	) {
		healAmount = 64;
	}

	public static Actor rpcInvoke(ActorRpcParameters arg) {
		return new GiantHealthPickup(
			arg.player, arg.pos, arg.netId, false, teamOnly: arg.extraData[0] == 1
		);
	}
}

public class TankHealthPickup : BaselineHealthPickup {
	public TankHealthPickup(
		Player owner, Point pos, ushort? netId, bool ownedByLocalPlayer,
		bool sendRpc = false, bool teamOnly = false
	) : base(
		owner, pos, "pickup_etank", netId, ownedByLocalPlayer, 
		CActorIds.TankHealthPickup, sendRpc: sendRpc, teamOnly: teamOnly
	) {
		healAmount = 16;
	}

	public static Actor rpcInvoke(ActorRpcParameters arg) {
		return new TankHealthPickup(
			arg.player, arg.pos, arg.netId, false, teamOnly: arg.extraData[0] == 1
		);
	}
}

public class LargeHealthPickup : BaselineHealthPickup {
	public LargeHealthPickup(
		Player owner, Point pos, ushort? netId, bool ownedByLocalPlayer,
		bool sendRpc = false, bool teamOnly = false
	) : base(
		owner, pos, "pickup_health_large", netId, ownedByLocalPlayer, 
		CActorIds.LargeHealthPickup, sendRpc: sendRpc, teamOnly: teamOnly
	) {
		healAmount = 8;
	}

	public static Actor rpcInvoke(ActorRpcParameters arg) {
		return new LargeHealthPickup(
			arg.player, arg.pos, arg.netId, false, teamOnly: arg.extraData[0] == 1
		);
	}
}

public class SmallHealthPickup : BaselineHealthPickup {
	public SmallHealthPickup(
		Player owner, Point pos, ushort? netId, bool ownedByLocalPlayer,
		bool sendRpc = false, bool teamOnly = false
	) : base(
		owner, pos, "pickup_health_small", netId, ownedByLocalPlayer,
		CActorIds.SmallHealthPickup, sendRpc: sendRpc, teamOnly: teamOnly
	) {
		healAmount = 4;
	}

	public static Actor rpcInvoke(ActorRpcParameters arg) {
		return new SmallHealthPickup(
			arg.player, arg.pos, arg.netId, false, teamOnly: arg.extraData[0] == 1
		);
	}
}

public class MicroHealthPickup : BaselineHealthPickup {
	public MicroHealthPickup(
		Player owner, Point pos, ushort? netId, bool ownedByLocalPlayer,
		bool sendRpc = false, bool teamOnly = false
	) : base(
		owner, pos, "pickup_health_micro", netId, ownedByLocalPlayer,
		CActorIds.SmallHealthPickup, sendRpc: sendRpc, teamOnly: teamOnly
	) {
		healAmount = 2;
	}

	public static Actor rpcInvoke(ActorRpcParameters arg) {
		return new MicroHealthPickup(
			arg.player, arg.pos, arg.netId, false, teamOnly: arg.extraData[0] == 1
		);
	}
}
