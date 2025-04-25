using System;
using Lidgren.Network;

namespace MMXOnline;

public enum NetActorCreateId {
	Default,
	RideArmor,
	RaySplasherTurret,
	DWrapBigBubble,
	Rush,
	CopyVisionClone,
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
	LargeHealth,
	SmallHealth,
	LargeAmmo,
	SmallAmmo,
	LargeBolt,
	SmallBolt,
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
			case (int)NetActorCreateId.LargeHealth:
				new LargeHealthPickup(player, pos, netProjByte, false);
				break;
			case (int)NetActorCreateId.SmallHealth:
				new SmallHealthPickup(player, pos, netProjByte, false);
				break;
			case (int)NetActorCreateId.LargeAmmo:
				new LargeAmmoPickup(player, pos, netProjByte, false);
				break;
			case (int)NetActorCreateId.SmallAmmo:
				new SmallAmmoPickup(player, pos, netProjByte, false);
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

