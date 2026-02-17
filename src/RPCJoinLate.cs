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

		// For actors that persist much like players.
		List<ActorRpcResponse> lateActors = [];
		foreach (GameObject go in Global.level.gameObjects) {
			// Skip if has not the correct flag or is not an actor.
			if (go is not Actor { syncOnLateJoin: true } actor) {
				continue;
			}
			// We crash if has the flag but a null NetId.
			if (actor.netId == null) {
				throw new Exception(
					$"Error, lateSync actor with type {actor.GetType().Name} has a null NetID."
				);
			}
			// Get the serialized version of the object.
			ActorRpcResponse? serial = actor.getActorSerial();
			// We crash if has the flag but a null Serial.
			if (serial == null) {
				throw new Exception(
					$"Error, lateSync actor with type {actor.GetType().Name} has a null Serial."
				);
			}
			// If everithing goes well we add it's serialized version.
			lateActors.Add(serial.Value);
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
