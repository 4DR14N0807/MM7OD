using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Lidgren.Network;
using Newtonsoft.Json;

namespace MMXOnline;
// This file contains mostly legacy RPCs that exists only because GM19 code shanigans.
// Hopefully these should be removed in the future.

public class RPCBroadcastLoadout : RPC {
	public RPCBroadcastLoadout() {
		netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
	}

	public override void invoke(params byte[] arguments) {
		LoadoutData loadout = Helpers.deserialize<LoadoutData>(arguments);
		var player = Global.level?.getPlayerById(loadout.playerId);
		if (player == null) return;

		player.loadout = loadout;
		player.loadoutSet = true;
	}

	public void sendRpc(Player player) {
		byte[] loadoutBytes = Helpers.serialize(player.loadout);
		Global.serverClient?.rpc(this, loadoutBytes);
	}
}
