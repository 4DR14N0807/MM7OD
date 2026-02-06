namespace MMXOnline;

public abstract class BaselineAmmoPickup : Pickup {
	public BaselineAmmoPickup(
		Player owner, Point pos, string sprite, ushort? netId,
		bool ownedByLocalPlayer, CActorIds cActorId,
		bool sendRpc = false, bool teamOnly = false
	) : base(
		owner, pos, sprite, netId, ownedByLocalPlayer,
		cActorId, sendRpc: sendRpc, teamOnly: teamOnly
	) {
		pickupType = PickupType.Ammo;
		healAmount = 50;
		altHealAmount = 8;
	}

	public override void use(Character chr) {
		if (!chr.canAddAmmo()) {
			return;
		}
		if (chr is Blues blues) {
			blues.healCore(altHealAmount);
		} else {
			//Adrian: Use this one instead to swap to HDM Ammo System.
			//        (Remember to adjust the heal values too).
			//chr.addAmmo(healAmount);
			chr.addPercentAmmo(healAmount);
		}
		base.use(chr);
	}
}

public class GiantAmmoPickup : BaselineAmmoPickup {
	public GiantAmmoPickup(
		Player owner, Point pos, ushort? netId, bool ownedByLocalPlayer,
		bool sendRpc = false, bool teamOnly = false
	) : base(
		owner, pos, "pickup_ammo_giant", netId, ownedByLocalPlayer,
		CActorIds.GiantAmmoPickup, sendRpc: sendRpc, teamOnly: teamOnly
	) {
		healAmount = 100;
		altHealAmount = 64;
	}

	public static Actor rpcInvoke(ActorRpcParameters arg) {
		return new GiantAmmoPickup(
			arg.player, arg.pos, arg.netId, false, teamOnly: arg.extraData[0] == 1
		);
	}
}

public class TankAmmoPickup : BaselineAmmoPickup {
	public TankAmmoPickup(
		Player owner, Point pos, ushort? netId, bool ownedByLocalPlayer,
		bool sendRpc = false, bool teamOnly = false
	) : base(
		owner, pos, "pickup_etank", netId, ownedByLocalPlayer, 
		CActorIds.LargeAmmoPickup, sendRpc: sendRpc, teamOnly: teamOnly
	) {
		healAmount = 16;
	}

	public static Actor rpcInvoke(ActorRpcParameters arg) {
		return new TankAmmoPickup(
			arg.player, arg.pos, arg.netId, false, teamOnly: arg.extraData[0] == 1
		);
	}
}


public class LargeAmmoPickup : BaselineAmmoPickup {
	public LargeAmmoPickup(
		Player owner, Point pos, ushort? netId, bool ownedByLocalPlayer,
		bool sendRpc = false, bool teamOnly = false
	) : base(
		owner, pos, "pickup_ammo_large", netId, ownedByLocalPlayer, 
		CActorIds.LargeAmmoPickup, sendRpc: sendRpc, teamOnly: teamOnly
	) {
		healAmount = 50;
		altHealAmount = 8;
	}

	public static Actor rpcInvoke(ActorRpcParameters arg) {
		return new LargeAmmoPickup(
			arg.player, arg.pos, arg.netId, false, teamOnly: arg.extraData[0] == 1
		);
	}
}

public class SmallAmmoPickup : BaselineAmmoPickup {
	public SmallAmmoPickup(
		Player owner, Point pos, ushort? netId, bool ownedByLocalPlayer,
		bool sendRpc = false, bool teamOnly = false
	) : base(
		owner, pos, "pickup_ammo_small", netId, ownedByLocalPlayer, 
		CActorIds.SmallAmmoPickup, sendRpc: sendRpc, teamOnly: teamOnly
	) {
		healAmount = 25;
		altHealAmount = 4;
	}

	public static Actor rpcInvoke(ActorRpcParameters arg) {
		return new SmallAmmoPickup(
			arg.player, arg.pos, arg.netId, false, teamOnly: arg.extraData[0] == 1
		);
	}
}

public class MicroAmmoPickup : BaselineAmmoPickup {
	public MicroAmmoPickup(
		Player owner, Point pos, ushort? netId, bool ownedByLocalPlayer,
		bool sendRpc = false, bool teamOnly = false
	) : base(
		owner, pos, "pickup_ammo_micro", netId, ownedByLocalPlayer, 
		CActorIds.SmallAmmoPickup, sendRpc: sendRpc, teamOnly: teamOnly
	) {
		healAmount = 12.5f;
		altHealAmount = 2;
	}

	public static Actor rpcInvoke(ActorRpcParameters arg) {
		return new MicroAmmoPickup(
			arg.player, arg.pos, arg.netId, false, teamOnly: arg.extraData[0] == 1
		);
	}
}
