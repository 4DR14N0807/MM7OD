using System.Collections.Generic;

namespace MMXOnline;

public partial class RPCCreateProj : RPC {
	public static Dictionary<int, ProjCreate> functs = new Dictionary<int, ProjCreate> {
		// X Stuff.
		/*{ (int)ProjIds.Boomerang, BoomerangProj.rpcInvoke },
		{ (int)ProjIds.ShotgunIce, ShotgunIceProj.rpcInvoke },
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
		{ (int)ProjIds.LongshotGizmo, VileCannonProj.rpcInvoke },*/
		
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
		
		// Buster Zero
		/*{ (int)ProjIds.DZBuster, DZBusterProj.rpcInvoke },
		{ (int)ProjIds.DZBuster2, DZBuster2Proj.rpcInvoke },
		{ (int)ProjIds.DZBuster3, DZBuster3Proj.rpcInvoke },*/
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
