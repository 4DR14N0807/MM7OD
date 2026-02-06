using System;
using System.Collections.Generic;
using Lidgren.Network;

namespace MMXOnline;

public enum CActorIds {
	Default,
	// HP
	GiantHealthPickup,
	TankHealthPickup,
	LargeHealthPickup,
	SmallHealthPickup,
	MicroHealthPickup,
	// Ammo
	GiantAmmoPickup,
	WTankAmmoPickup,
	LargeAmmoPickup,
	SmallAmmoPickup,
	MicroAmmoPickup,
	// Bolts
	GiantBoltPickup,
	LargeBoltPickup,
	SmallBoltPickup,
	// Shield.
	TankShieldPickup,
	LargeShieldPickup,
	SmallShieldPickup,
	MicroShieldPickup,
	// Super.
	STankSuperPickup,
	LargeSuperPickup,
	SmallSuperPickup,
	MicroSuperPickup,
}

public partial class RPCCreateActor : RPC {
	public RPCCreateActor() {
		netDeliveryMethod = NetDeliveryMethod.ReliableOrdered; 
		isPreUpdate = true;
	}

	public override void invoke(params byte[] arguments) {
		// Data of the array.
		bool[] dataInf = Helpers.byteToBoolArray(arguments[0]);
		// Always present values.
		byte playerId = arguments[1];
		int cActorId = BitConverter.ToUInt16(arguments.AsSpan()[2..4]);
		float xPos = BitConverter.ToSingle(arguments.AsSpan()[4..8]);
		float yPos = BitConverter.ToSingle(arguments.AsSpan()[8..12]);
		ushort netIdByte = BitConverter.ToUInt16(arguments.AsSpan()[12..14]);
		// Angle and Dir
		int xDir = arguments[14] - 1;
		float byteAngle = 0;
		float angle = 0;
		// Optional arguments.
		int index = 15;
		if (dataInf[0]) {
			byteAngle = arguments[index];
			angle = byteAngle * 1.40625f;
			index++;
		}
		Actor? owner = null;
		if (dataInf[1]) {
			owner = Global.level.getActorByNetId(
				BitConverter.ToUInt16(arguments.AsSpan()[index..(index+2)]),
				true
			);
			index += 2;
		}
		// Extra arguments.
		byte[] extraData;
		if (dataInf[2] && arguments.Length >= index + 1) {
			extraData = arguments[index..];
		} else {
			extraData = [];
		}

		Player? player = Global.level.getPlayerById(playerId);
		if (player == null) { return; }
		if (Global.level.getActorByNetId(netIdByte) != null) { return; }
		if (Global.level.recentlyDestroyedNetActors.ContainsKey(netIdByte)) { return; }
		Point pos = new Point(xPos, yPos);

		if (functs.ContainsKey(cActorId)) {
			ActorRpcParameters args = new() {
				actorId = cActorId,
				pos = pos,
				xDir = xDir,
				player = player,
				netId = netIdByte,
				angle = 0,
				byteAngle = 0,
				extraData = extraData,
				owner = owner,
			};
			functs[cActorId](args);
			return;
		}

		switch (cActorId) {
			switch (createId) {
			case (int)NetActorCreateId.DWrapBigBubble:
				new DWrapBigBubble(pos, player, xDir, netProjByte, false);
				break;
			case (int)NetActorCreateId.Rush:
				new Rush(pos, player, xDir, netProjByte, false);
				break;
			case (int)NetActorCreateId.CopyVisionClone:
				new CopyVisionClone(null, pos, xDir, netProjByte, false, altPlayer: player);
				break;
			case (int)NetActorCreateId.Met:
				new Met(pos, xDir, player, netProjByte, false);
				break;
		}
	}

	public void sendRpc(Actor actor, Player player, Actor? owner, params byte[] extraData) {
		if (Global.serverClient == null || !actor.ownedByLocalPlayer) {
			return;
		}
		sendRpc(
			actor, actor.pos, actor.xDir, player, (int)actor.cActorId,
			actor.netId, owner, actor.byteAngle, extraData
		);
	}

	public void sendRpc(
		Actor actor, Point pos, int xDir, Player player, int cActorId, ushort? netId,
		Actor? owner = null, float? byteAngle = null, byte[]? extraData = null
	) {
		if (Global.serverClient == null) {
			return;
		}
		if (player == null) {
			throw new Exception(
				$"Attempt to create RPC of projectile type {actor.getActorTypeName()} without a owner player."
			);
		}
		if (netId == null || cActorId == (int)CActorIds.Default) {
			throw new Exception(
				$"Attempt to create RPC of projectile type {actor.getActorTypeName()} with null ID"
			);
		}
		byte[] cActorIdBytes = BitConverter.GetBytes((ushort)cActorId);
		byte[] xBytes = BitConverter.GetBytes(pos.x);
		byte[] yBytes = BitConverter.GetBytes(pos.y);
		byte[] netIdByte = BitConverter.GetBytes(netId.Value);
		// Create bools of data.
		byte dataInf = Helpers.boolArrayToByte([
			byteAngle != null && byteAngle != 0,
			owner?.netId != null,
			extraData != null && extraData.Length > 0,
		]);
		byte netDir = (byte)(Math.Sign(xDir) + 1);

		// Create byte list.
		List<byte> bytes = [
			dataInf, (byte)player.id,
			cActorIdBytes[0], cActorIdBytes[1],
			xBytes[0], xBytes[1], xBytes[2], xBytes[3],
			yBytes[0], yBytes[1], yBytes[2], yBytes[3],
			netIdByte[0], netIdByte[1],
			netDir
		];
		if (byteAngle != null && byteAngle != 0) {
			bytes.Add((byte)(Math.Round(actor.byteAngle) % 256));
		}
		if (owner?.netId != null) {
			bytes.AddRange(BitConverter.GetBytes(owner.netId.Value));
		}
		if (extraData != null && extraData.Length > 0) {
			bytes.AddRange(extraData);
		}
		Global.serverClient?.rpc(createActor, bytes.ToArray());
	}
}

public struct ActorRpcParameters {
	public int actorId;
	public Point pos;
	public int xDir;
	public Player player;
	public ushort netId;
	public byte[] extraData;
	public float angle;
	public float byteAngle;
	public Actor owner;
}

public delegate Actor ActorRpcCreate(ActorRpcParameters arg);


// Old stuff.
public enum NetActorCreateId {
	Default,
	RideArmor = 100,
	RaySplasherTurret,
	DWrapBigBubble,
	Rush,
	CopyVisionClone,
	Met,
	ChillPenguin,
	SparkMandrill,
	ArmoredArmadillo,
	LaunchOctopus,
	BoomerangKuwanger,
	StingChameleon,
	StormEagle,
	FlameMammoth,
	Velguarder,
	WolfSigmaHead,
	WolfSigmaHand,
	// Other stuff.
	MechaniloidTank,
	MechaniloidFish,
	MechaniloidHopper,
	WireSponge,
	WheelGator,
	BubbleCrab,
	FlameStag,
	MorphMoth,
	MorphMothCocoon,
	MagnaCentipede,
	CrystalSnail,
	OverdriveOstrich,
	FakeZero,
	CrystalHunterCharged,
	CrystalSnailShell,
	BlizzardBuffalo,
	ToxicSeahorse,
	TunnelRhino,
	VoltCatfish,
	CrushCrawfish,
	NeonTiger,
	GravityBeetle,
	BlastHornet,
	DrDoppler,
	RideChaser,
}

public class RPCCreateActor : RPC {
	public RPCCreateActor() {
		netDeliveryMethod = NetDeliveryMethod.ReliableOrdered; 
		isPreUpdate = true;
	}

	public override void invoke(params byte[] arguments) {
		int createId = arguments[0];
		float xPos = BitConverter.ToSingle(new byte[] { arguments[1], arguments[2], arguments[3], arguments[4] }, 0);
		float yPos = BitConverter.ToSingle(new byte[] { arguments[5], arguments[6], arguments[7], arguments[8] }, 0);
		var playerId = arguments[9];
		var netProjByte = BitConverter.ToUInt16(new byte[] { arguments[10], arguments[11] }, 0);
		int xDir = arguments[12] - 128;

		var player = Global.level.getPlayerById(playerId);
		if (player == null) return;

		Actor? actor = Global.level.getActorByNetId(netProjByte);
		if (actor != null && (int)actor.netActorCreateId == createId) return;
		if (Global.level.recentlyDestroyedNetActors.ContainsKey(netProjByte)) return;
		Point pos = new Point(xPos, yPos);

		switch (createId) {
			case (int)NetActorCreateId.DWrapBigBubble:
				new DWrapBigBubble(pos, player, xDir, netProjByte, false);
				break;
			case (int)NetActorCreateId.Rush:
				new Rush(pos, player, xDir, netProjByte, false);
				break;
			case (int)NetActorCreateId.CopyVisionClone:
				new CopyVisionClone(actor, pos, xDir, netProjByte, false, altPlayer: player);
				break;
			case (int)NetActorCreateId.Met:
				new Met(pos, xDir, player, netProjByte, false);
				break;
			case (int)NetActorCreateId.GiantHealth:
				new GiantHealthPickup(player, pos, netProjByte, false);
				break;
			case (int)NetActorCreateId.LargeHealth:
				new LargeHealthPickup(player, pos, netProjByte, false);
				break;
			case (int)NetActorCreateId.SmallHealth:
				new SmallHealthPickup(player, pos, netProjByte, false);
				break;
			case (int)NetActorCreateId.GiantAmmo:
				new GiantAmmoPickup(player, pos, netProjByte, false);
				break;
			case (int)NetActorCreateId.LargeAmmo:
				new LargeAmmoPickup(player, pos, netProjByte, false);
				break;
			case (int)NetActorCreateId.SmallAmmo:
				new SmallAmmoPickup(player, pos, netProjByte, false);
				break;
			case (int)NetActorCreateId.GiantBolt:
				new GiantBoltPickup(player, pos, netProjByte, false);
				break;
			case (int)NetActorCreateId.LargeBolt:
				new LargeBoltPickup(player, pos, netProjByte, false);
				break;
			case (int)NetActorCreateId.SmallBolt:
				new SmallBoltPickup(player, pos, netProjByte, false);
				break;
		}
	}
}

