using System;
using System.Collections.Generic;
using System.Linq;
using Lidgren.Network;

namespace MMXOnline;

public class RPCJoinLateRequest : RPC {
	public RPCJoinLateRequest() {
		netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
		isPreUpdate = true;
		toHostOnly = true;
	}

	public override void invoke(params byte[] arguments) {
		var serverPlayer = Helpers.deserialize<ServerPlayer>(arguments);
		Global.level.addPlayer(serverPlayer, true);

		List<ControlPointResponseModel> controlPoints = [];
		foreach (var cp in Global.level.controlPoints) {
			controlPoints.Add(new ControlPointResponseModel() {
				alliance = cp.alliance,
				num = cp.num,
				locked = cp.locked,
				captured = cp.captured,
				captureTime = cp.captureTime
			});
		}

		List<ActorRpcResponse> lateActors = [];
		foreach (GameObject go in Global.level.gameObjects) {
			if (go is not Actor { syncOnLateJoin: true } actor) {
				continue;
			}
			ActorRpcResponse? serial = actor.getActorSerial();
			if (serial != null) {
				lateActors.Add(serial.Value);
			}
		}

		var joinLateResponseModel = new JoinLateResponseModel() {
			players = Global.level.players.Select(p => new PlayerPB(p)).ToArray(),
			newPlayer = serverPlayer,
			controlPoints = controlPoints.ToArray(),
			lateActors = lateActors.ToArray()
		};

		Global.serverClient?.rpc(RPC.joinLateResponse, Helpers.serialize(joinLateResponseModel));
	}
}

public class RPCJoinLateResponse : RPC {
	public RPCJoinLateResponse() {
		netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
		levelless = true;
		allowBreakMtuLimit = true;
	}

	public override void invoke(params byte[] arguments) {
		JoinLateResponseModel? joinLateResponseModel = null;
		try {
			joinLateResponseModel = Helpers.deserialize<JoinLateResponseModel>(arguments);
		} catch {
			try {
				Logger.logEvent(
					"error",
					"Bad joinLateResponseModel bytes. name: " +
					Options.main.playerName + ", match: " + Global.level?.server?.name +
					", bytes: " + arguments.ToString()
				);
				//Console.Write(message); 
			} catch { }
			throw;
		}

		// Original requester
		if (Global.serverClient?.serverPlayer.id == joinLateResponseModel.newPlayer.id) {
			Global.level.joinedLateSyncPlayers(joinLateResponseModel.players);
			Global.level.joinedLateSyncControlPoints(joinLateResponseModel.controlPoints);
			Global.level.joinedLateSyncActors(joinLateResponseModel.lateActors);
		} else {
			Global.level.addPlayer(joinLateResponseModel.newPlayer, true);
		}
	}
}
