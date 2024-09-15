using System.Collections.Generic;

namespace MMXOnline;

public partial class RPCCreateProj : RPC {
	public static Dictionary<int, ProjCreate> functs = new Dictionary<int, ProjCreate> {
		/*
		// X Stuff.
		{ (int)ProjIds.Boomerang, BoomerangProj.rpcInvoke },
		{ (int)ProjIds.ShotgunIce, ShotgunIceProj.rpcInvoke },
		{ (int)ProjIds.BubbleSplash, BubbleSplashProj.rpcInvoke },
		{ (int)ProjIds.TriadThunder, TriadThunderProj.rpcInvoke },
		{ (int)ProjIds.TriadThunderQuake, TriadThunderQuake.rpcInvoke },
		{ (int)ProjIds.TriadThunderCharged, TriadThunderProjCharged.rpcInvoke },
		{ (int)ProjIds.RaySplasher, RaySplasherProj.rpcInvoke },
		{ (int)ProjIds.RaySplasherChargedProj, RaySplasherProj.rpcInvoke },
		{ (int)ProjIds.UPParryMelee, UPParryMeleeProj.rpcInvoke },
		{ (int)ProjIds.UPParryProj, UPParryRangedProj.rpcInvoke },
		// Vile stuff.
		{ (int)ProjIds.FrontRunner, VileCannonProj.rpcInvoke },
		{ (int)ProjIds.FatBoy, VileCannonProj.rpcInvoke },
		{ (int)ProjIds.LongshotGizmo, VileCannonProj.rpcInvoke },
		{ (int)ProjIds.LongshotGizmo, VileCannonProj.rpcInvoke },
		// Buster Zero
		{ (int)ProjIds.DZBuster, DZBusterProj.rpcInvoke },
		{ (int)ProjIds.DZBuster2, DZBuster2Proj.rpcInvoke },
		{ (int)ProjIds.DZBuster3, DZBuster3Proj.rpcInvoke },
		// Mavericks
		{ (int)ProjIds.VoltCSuck, VoltCSuckProj.rpcInvoke }
		// Buster Zero
		{ (int)ProjIds.DZBuster, DZBusterProj.rpcInvoke },
		{ (int)ProjIds.DZBuster2, DZBuster2Proj.rpcInvoke },
		{ (int)ProjIds.DZBuster3, DZBuster3Proj.rpcInvoke },
		*/
		// Rock stuff.
		{ (int)RockProjIds.RockBuster, RockBusterProj.rpcInvoke },
		{ (int)RockProjIds.RockBusterMid, RockBusterMidChargeProj.rpcInvoke },
		{ (int)RockProjIds.RockBusterCharged, RockBusterChargedProj.rpcInvoke },
		{ (int)RockProjIds.FreezeCracker, FreezeCrackerProj.rpcInvoke },
		{ (int)RockProjIds.FreezeCrackerPiece, FreezeCrackerPieceProj.rpcInvoke },
		{ (int)RockProjIds.ThunderBolt, ThunderBoltProj.rpcInvoke },
		{ (int)RockProjIds.ThunderBoltSplit, ThunderBoltSplitProj.rpcInvoke },
		{ (int)RockProjIds.JunkShield, JunkShieldProj.rpcInvoke},
		{ (int)RockProjIds.JunkShieldPiece, JunkShieldShootProj.rpcInvoke},
		{ (int)RockProjIds.ScorchWheelSpawn, ScorchWheelSpawn.rpcInvoke},
		{ (int)RockProjIds.ScorchWheel, ScorchWheelProj.rpcInvoke},
		//{ (int)RockProjIds.ScorchWheelLoop, ScorchWheelProjLoop.rpcInvoke},
		{ (int)RockProjIds.ScorchWheelMove, ScorchWheelMoveProj.rpcInvoke },
		{ (int)RockProjIds.NoiseCrush, NoiseCrushProj.rpcInvoke },
		{ (int)RockProjIds.NoiseCrushCharged, NoiseCrushChargedProj.rpcInvoke },
		{ (int)RockProjIds.DangerWrapMine, DangerWrapMineProj.rpcInvoke },
		{ (int)RockProjIds.DangerWrapExplosion, DangerWrapExplosionProj.rpcInvoke },
		{ (int)RockProjIds.DangerWrap, DangerWrapBubbleProj.rpcInvoke },
		{ (int)RockProjIds.WildCoil, WildCoilProj.rpcInvoke },
		{ (int)RockProjIds.WildCoilCharged, WildCoilChargedProj.rpcInvoke },
		{ (int)RockProjIds.SARocketPunch, SARocketPunchProj.rpcInvoke },
		{ (int)RockProjIds.SAArrowSlash, ArrowSlashProj.rpcInvoke },

		//Blues
		{ (int)BluesProjIds.Lemon, ProtoBusterProj.rpcInvoke },
		{ (int)BluesProjIds.LemonOverdrive, ProtoBusterOverdriveProj.rpcInvoke },
		{ (int)BluesProjIds.LemonAngled, ProtoBusterAngledProj.rpcInvoke },
		{ (int)BluesProjIds.BusterLV2, ProtoBusterLv2Proj.rpcInvoke },
		{ (int)BluesProjIds.BusterLV3, ProtoBusterLv3Proj.rpcInvoke },
		{ (int)BluesProjIds.BusterLV4, ProtoBusterLv4Proj.rpcInvoke },
		{ (int)BluesProjIds.NeedleCannon, NeedleCannonProj.rpcInvoke },
		{ (int)BluesProjIds.HardKnuckle, HardKnuckleProj.rpcInvoke },
		{ (int)BluesProjIds.SearchSnake, SearchSnakeProj.rpcInvoke },
		{ (int)BluesProjIds.SparkShock, SparkShockProj.rpcInvoke },
		{ (int)BluesProjIds.GravityHold, GravityHoldProj.rpcInvoke},
		{ (int)BluesProjIds.PowerStone, PowerStoneProj.rpcInvoke },
		{ (int)BluesProjIds.GyroAttack, GyroAttackProj.rpcInvoke },
		{ (int)BluesProjIds.StarCrash, StarCrashProj.rpcInvoke },
		{ (int)BluesProjIds.ProtoStrike, ProtoStrikeProj.rpcInvoke },
		{ (int)BluesProjIds.ProtoStrikePush, StrikeAttackPushProj.rpcInvoke },
		{ (int)BluesProjIds.BigBangStrike, BigBangStrikeProj.rpcInvoke },
		{ (int)BluesProjIds.BigBangStrikeExplosion, BigBangStrikeExplosionProj.rpcInvoke },

		//Bass stuff.
		{ (int)BassProjIds.BassLemon, BassBusterProj.rpcInvoke },
		{ (int)BassProjIds.CopyVisionLemon, CopyVisionLemon.rpcInvoke },
		//{ (int)BassProjIds.IceWall, IceWallProj.rpcInvoke },
		{ (int)BassProjIds.MagicCard, MagicCardProj.rpcInvoke },
		{ (int)BassProjIds.MagicCardSSpawn, MagicCardSpecialSpawn.rpcInvoke },
		{ (int)BassProjIds.MagicCardS, MagicCardSpecialProj.rpcInvoke },
		{ (int)BassProjIds.RemoteMine, RemoteMineProj.rpcInvoke },
		{ (int)BassProjIds.RemoteMineExplosion, RemoteMineExplosionProj.rpcInvoke },
		{ (int)BassProjIds.SpreadDrill, SpreadDrillProj.rpcInvoke },
		{ (int)BassProjIds.SpreadDrillMid, SpreadDrillMediumProj.rpcInvoke },
		{ (int)BassProjIds.SpreadDrillSmall, SpreadDrillSmallProj.rpcInvoke },
		{ (int)BassProjIds.WaveBurner, WaveBurnerProj.rpcInvoke },
		{ (int)BassProjIds.WaveBurnerUnderwater, WaveBurnerUnderwaterProj.rpcInvoke },
		{ (int)BassProjIds.TenguBladeProj, TenguBladeProj.rpcInvoke },
	};

}

public struct ProjParameters {
	public int projId;
	public Point pos;
	public int xDir;
	public Player player;
	public ushort netId;
	public byte[] extraData;
	public float angle;
	public float byteAngle;
}

public delegate Projectile ProjCreate(ProjParameters arg);
