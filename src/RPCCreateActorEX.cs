using System;
using System.Collections.Generic;

namespace MMXOnline;

public partial class RPCCreateActor : RPC {
	public static Dictionary<int, ActorRpcCreate> functs = new Dictionary<int, ActorRpcCreate> {
		// HP
		{ (int)CActorIds.GiantHealthPickup, MicroShieldPickup.rpcInvoke },
		{ (int)CActorIds.TankHealthPickup, MicroShieldPickup.rpcInvoke },
		{ (int)CActorIds.LargeHealthPickup, MicroShieldPickup.rpcInvoke },
		{ (int)CActorIds.SmallHealthPickup, MicroShieldPickup.rpcInvoke },
		{ (int)CActorIds.MicroHealthPickup, MicroShieldPickup.rpcInvoke },
		// Ammo
		{ (int)CActorIds.GiantAmmoPickup, MicroShieldPickup.rpcInvoke },
		{ (int)CActorIds.WTankAmmoPickup, MicroShieldPickup.rpcInvoke },
		{ (int)CActorIds.LargeAmmoPickup, MicroShieldPickup.rpcInvoke },
		{ (int)CActorIds.SmallAmmoPickup, MicroShieldPickup.rpcInvoke },
		{ (int)CActorIds.MicroAmmoPickup, MicroShieldPickup.rpcInvoke },
		// Bolts
		{ (int)CActorIds.GiantBoltPickup, MicroShieldPickup.rpcInvoke },
		{ (int)CActorIds.LargeBoltPickup, MicroShieldPickup.rpcInvoke },
		{ (int)CActorIds.SmallBoltPickup, MicroShieldPickup.rpcInvoke },
		// Shield.
		{ (int)CActorIds.TankShieldPickup, MicroShieldPickup.rpcInvoke },
		{ (int)CActorIds.LargeShieldPickup, MicroShieldPickup.rpcInvoke },
		{ (int)CActorIds.SmallShieldPickup, MicroShieldPickup.rpcInvoke },
		{ (int)CActorIds.MicroShieldPickup, MicroShieldPickup.rpcInvoke },
		// Super.
		{ (int)CActorIds.STankSuperPickup, TankSuperPickup.rpcInvoke },
		{ (int)CActorIds.LargeSuperPickup, LargeSuperPickup.rpcInvoke },
		{ (int)CActorIds.SmallSuperPickup, SmallSuperPickup.rpcInvoke },
		{ (int)CActorIds.MicroSuperPickup, MicroShieldPickup.rpcInvoke },
	};
}