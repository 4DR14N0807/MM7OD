using System;
using System.Collections.Generic;
using Lidgren.Network;

namespace MMXOnline;

public partial class RPCCreateProj : RPC {
	public RPCCreateProj() {
		netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
		isPreUpdate = true;
	}

	public override void invoke(params byte[] arguments) {
		// Data of the array.
		bool[] dataInf = Helpers.byteToBoolArray(arguments[0]);
		// Always present values.
		var playerId = arguments[1];
		ushort projId = BitConverter.ToUInt16(arguments[2..4], 0);
		float xPos = BitConverter.ToSingle(arguments[4..8], 0);
		float yPos = BitConverter.ToSingle(arguments[8..12], 0);
		var netProjByte = BitConverter.ToUInt16(arguments[12..14], 0);
		// Angle or Dir
		int xDir = 1;
		float angle = 0;
		float byteAngle = 0;
		if (!dataInf[0]) {
			xDir = arguments[14];
			xDir -= 1;
		} else {
			byteAngle = arguments[14];
			angle = byteAngle * 1.40625f;
		}
		// Optional arguments.
		int extraDataIndex = 15;
		Actor? owner = null;
		if (dataInf[1]) {
			owner = Global.level.getActorByNetId(BitConverter.ToUInt16(arguments[15..17], 0), true);
			extraDataIndex = 17;
		}
		// Extra arguments.
		byte[] extraData;
		if (dataInf[2] && arguments.Length >= extraDataIndex + 1) {
			extraData = arguments[extraDataIndex..];
		} else {
			extraData = new byte[0];
		}
		Point bulletDir = Point.createFromByteAngle(byteAngle);

		var player = Global.level.getPlayerById(playerId);
		if (player == null) return;

		Point pos = new Point(xPos, yPos);

		if (functs.ContainsKey(projId)) {
			ProjParameters args = new() {
				projId = projId,
				pos = pos,
				xDir = xDir,
				player = player,
				netId = netProjByte,
				angle = angle,
				byteAngle = byteAngle,
				extraData = extraData,
				owner = owner,
			};
			functs[projId](args);
		}
	}

	public byte[] getSendBytes(ProjParameters args) {
		return getSendBytes(
			args.pos, args.xDir, args.player.id, args.projId, args.netId,
			args.owner.netId, args.byteAngle, args.extraData
		);
	}


	private byte[] getSendBytes(
		Point pos, int xDir, int playerId, int projId, ushort? netId,
		int? ownerId, float? byteAngle = null, byte[]? extraData = null
	) {
		if (netId == null) {
			throw new Exception($"Attempt to create RPC of projectile type {projId} with null ID");
		}
		byte[] projIdBytes = BitConverter.GetBytes((ushort)projId);
		byte[] xBytes = BitConverter.GetBytes(pos.x);
		byte[] yBytes = BitConverter.GetBytes(pos.y);
		byte[] netProjIdByte = BitConverter.GetBytes(netId.Value);
		// Create bools of data.
		byte dataInf = Helpers.boolArrayToByte(new bool[] {
			false,
			ownerId != null,
			extraData != null && extraData.Length > 0
		});
		// xDir.
		xDir += 1;
		// Create byte list.
		List<byte> bytes = [
			dataInf, (byte)playerId,
			projIdBytes[0], projIdBytes[1],
			xBytes[0], xBytes[1], xBytes[2], xBytes[3],
			yBytes[0], yBytes[1], yBytes[2], yBytes[3],
			netProjIdByte[0], netProjIdByte[1],
			(byte)xDir
		];
		if (ownerId != null) {
			bytes.AddRange(BitConverter.GetBytes(ownerId.Value));
		}
		if (extraData != null && extraData.Length > 0) {
			bytes.AddRange(extraData);
		}
		return bytes.ToArray();
	}
}
