using System;
using System.Collections.Generic;

namespace MMXOnline;

public partial class RPCCreateActor : RPC {
	public static Dictionary<int, ActorRpcCreate> functs = new Dictionary<int, ActorRpcCreate> {
		// HP
		{ (int)CActorIds.GiantHealthPickup, GiantHealthPickup.rpcInvoke },
		{ (int)CActorIds.TankHealthPickup, TankHealthPickup.rpcInvoke },
		{ (int)CActorIds.LargeHealthPickup, LargeHealthPickup.rpcInvoke },
		{ (int)CActorIds.SmallHealthPickup, SmallHealthPickup.rpcInvoke },
		{ (int)CActorIds.MiniHealthPickup, MiniHealthPickup.rpcInvoke },
		// Ammo
		{ (int)CActorIds.GiantAmmoPickup, GiantAmmoPickup.rpcInvoke },
		{ (int)CActorIds.TankAmmoPickup, TankAmmoPickup.rpcInvoke },
		{ (int)CActorIds.LargeAmmoPickup, LargeAmmoPickup.rpcInvoke },
		{ (int)CActorIds.SmallAmmoPickup, SmallAmmoPickup.rpcInvoke },
		{ (int)CActorIds.MiniAmmoPickup, MiniAmmoPickup.rpcInvoke },
		// Bolts
		{ (int)CActorIds.GiantBoltPickup, GiantBoltPickup.rpcInvoke },
		{ (int)CActorIds.LargeBoltPickup, LargeBoltPickup.rpcInvoke },
		{ (int)CActorIds.SmallBoltPickup, SmallBoltPickup.rpcInvoke },
		// Shield.
		{ (int)CActorIds.TankShieldPickup, TankShieldPickup.rpcInvoke },
		{ (int)CActorIds.LargeShieldPickup, LargeShieldPickup.rpcInvoke },
		{ (int)CActorIds.SmallShieldPickup, SmallShieldPickup.rpcInvoke },
		{ (int)CActorIds.MiniShieldPickup, MiniShieldPickup.rpcInvoke },
		// Super.
		{ (int)CActorIds.TankSuperPickup, TankSuperPickup.rpcInvoke },
		{ (int)CActorIds.LargeSuperPickup, LargeSuperPickup.rpcInvoke },
		{ (int)CActorIds.SmallSuperPickup, SmallSuperPickup.rpcInvoke },
		{ (int)CActorIds.MiniSuperPickup, MiniSuperPickup.rpcInvoke },
		// Enemies.
		{ (int)CActorIds.Met, Met.rpcInvoke },
	};
}