using System.Collections.Generic;
using ProtoBuf;

namespace MMXOnline;

# nullable disable

[ProtoContract]
public class RPCMatchOverResponse {
	[ProtoMember(1)] public HashSet<int> winningAlliances;
	[ProtoMember(2)] public string winMessage;
	[ProtoMember(3)] public string loseMessage;
	[ProtoMember(4)] public string loseMessage2;
	[ProtoMember(5)] public string winMessage2;

	public RPCMatchOverResponse() { }
}

[ProtoContract]
public class RPCAnimModel {
	[ProtoMember(1)] public long? zIndex;
	[ProtoMember(2)] public ushort? zIndexRelActorNetId;
	[ProtoMember(3)] public bool fadeIn;
	[ProtoMember(4)] public bool hasRaColorShader;

	public RPCAnimModel() { }
}

[ProtoContract]
public class ControlPointResponseModel {
	[ProtoMember(1)] public int alliance;
	[ProtoMember(2)] public int num;
	[ProtoMember(3)] public bool locked;
	[ProtoMember(4)] public bool captured;
	[ProtoMember(5)] public float captureTime;
	public ControlPointResponseModel() { }
}

[ProtoContract]
public class MagnetMineResponseModel {
	[ProtoMember(1)] public float x;
	[ProtoMember(2)] public float y;
	[ProtoMember(3)] public ushort netId;
	[ProtoMember(4)] public int playerId;
	public MagnetMineResponseModel() { }
}

[ProtoContract]
public class TurretResponseModel {
	[ProtoMember(1)] public float x;
	[ProtoMember(2)] public float y;
	[ProtoMember(3)] public ushort netId;
	[ProtoMember(4)] public int playerId;
	public TurretResponseModel() { }
}

[ProtoContract]
public class JoinLateResponseModel {
	[ProtoMember(1)] public PlayerPB[] players;
	[ProtoMember(2)] public ServerPlayer newPlayer;
	[ProtoMember(3)] public ControlPointResponseModel[] controlPoints;
	[ProtoMember(4)] public ActorRpcResponse[] lateActors;
	public JoinLateResponseModel() { }
}

[ProtoContract]
public struct ActorRpcResponse {
	[ProtoMember(1)] public bool isProj;
	[ProtoMember(2)] public int playerId;
	[ProtoMember(3)] public int actorId;
	[ProtoMember(4)] public ushort? ownerId;
	[ProtoMember(5)] public float posX;
	[ProtoMember(6)] public float posY;
	[ProtoMember(7)] public int xDir;
	[ProtoMember(8)] public ushort netId;
	[ProtoMember(9)] public float byteAngle;
	[ProtoMember(10)] public byte[] extraData;

	public ActorRpcResponse() { }
}

[ProtoContract]
public class PeriodicServerSyncModel {
	[ProtoMember(1)] public List<ServerPlayer> players;
	public PeriodicServerSyncModel() { }
}

[ProtoContract]
public class PeriodicHostSyncModel {
	[ProtoMember(1)] public RPCMatchOverResponse matchOverResponse;
	[ProtoMember(2)] public int redPoints;
	[ProtoMember(3)] public HashSet<byte> crackedWallBytes = new HashSet<byte>();
	[ProtoMember(4)] public byte virusStarted;
	[ProtoMember(5)] public byte safeZoneSpawnIndex;
	[ProtoMember(6)] public byte[] teamPoints;
}

[ProtoContract]
public class PlayerPB {
	[ProtoMember(1)] public int alliance;
	[ProtoMember(2)] public int newAlliance;
	[ProtoMember(3)] public bool isAI;
	[ProtoMember(4)] public int newCharNum;
	[ProtoMember(5)] public ushort curMaxNetId;
	[ProtoMember(6)] public bool warpedIn = false;
	[ProtoMember(7)] public float readyTime;
	[ProtoMember(8)] public bool spawnChar = false;
	[ProtoMember(9)] public ushort armorFlag;
	[ProtoMember(10)] public LoadoutData loadoutData;
	[ProtoMember(11)] public Disguise disguise;

	[ProtoMember(12)] public ushort? charNetId;
	[ProtoMember(13)] public ushort? charRollingShieldNetId;
	[ProtoMember(14)] public float charXPos;
	[ProtoMember(15)] public float charYPos;
	[ProtoMember(16)] public int charXDir;
	[ProtoMember(17)] public LoadoutData atransLoadout;
	[ProtoMember(18)] public int? currentCharNum;
	[ProtoMember(19)] public int? preAtransCharId;

	[ProtoMember(20)] public ServerPlayer serverPlayer;

	public PlayerPB() { }

	public PlayerPB(Player player) {
		serverPlayer = player.serverPlayer;
		alliance = player.alliance;
		newAlliance = player.newAlliance;
		newCharNum = player.newCharNum;
		if (player.character != null) {
			currentCharNum = (int)player.character.charId;
			charNetId = player.character.netId;
		}
		if (player.preTransformedChar != null) {
			preAtransCharId = (int)player.preTransformedChar.charId;
		}
		curMaxNetId = player.curMaxNetId;
		warpedIn = player.warpedInOnce;
		readyTime = player.readyTime;
		//readyTextOver = player.readyTextOver;
		loadoutData = player.loadout;
		disguise = player.disguise;
		atransLoadout = player.atransLoadout;
		if (player.character != null) {
			charNetId = player.character.netId;
			charXPos = player.character.pos.x;
			charYPos = player.character.pos.y;
			charXDir = player.character.xDir;
		}
	}
}

# nullable enable
