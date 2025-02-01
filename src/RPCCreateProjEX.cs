using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MMXOnline;

public partial class RPCCreateProj : RPC {
	public static Dictionary<int, ProjCreate> functs = new Dictionary<int, ProjCreate> {
		/*
		// X Stuff.
		//BUSTERS
		{ (int)ProjIds.Buster, BusterProj.rpcInvoke },
		{ (int)ProjIds.Buster2, Buster2Proj.rpcInvoke },
		{ (int)ProjIds.BusterUnpo, RagingBusterProj.rpcInvoke },
		{ (int)ProjIds.Buster3, Buster3Proj.rpcInvoke },
		{ (int)ProjIds.Buster4, Buster4Proj.rpcInvoke },
		{ (int)ProjIds.BusterX3Proj2, BusterX3Proj2.rpcInvoke },
		{ (int)ProjIds.BusterX3Plasma, BusterPlasmaProj.rpcInvoke },
		{ (int)ProjIds.BusterX3PlasmaHit, BusterPlasmaHitProj.rpcInvoke },

		//X1 PROJS
		{ (int)ProjIds.Torpedo, TorpedoProj.rpcInvoke },
		{ (int)ProjIds.TorpedoCharged, TorpedoProj.rpcInvoke },
		{ (int)ProjIds.Sting, StingProj.rpcInvoke },
		{ (int)ProjIds.StingDiag, StingProj.rpcInvoke },
		{ (int)ProjIds.RollingShield, RollingShieldProj.rpcInvoke },
		{ (int)ProjIds.RollingShieldCharged, RollingShieldProjCharged.rpcInvoke },
		{ (int)ProjIds.FireWave, FireWaveProj.rpcInvoke },
		{ (int)ProjIds.FireWaveChargedStart, FireWaveProjChargedStart.rpcInvoke },
		{ (int)ProjIds.FireWaveCharged, FireWaveProjCharged.rpcInvoke },
		{ (int)ProjIds.Tornado, TornadoProj.rpcInvoke },
		{ (int)ProjIds.TornadoCharged, TornadoProjCharged.rpcInvoke },
		{ (int)ProjIds.ElectricSpark, ElectricSparkProj.rpcInvoke },
		{ (int)ProjIds.ElectricSparkChargedStart, ElectricSparkProjChargedStart.rpcInvoke },
		{ (int)ProjIds.ElectricSparkCharged, ElectricSparkProjCharged.rpcInvoke },
		{ (int)ProjIds.Boomerang, BoomerangProj.rpcInvoke },
		{ (int)ProjIds.BoomerangCharged, BoomerangProjCharged.rpcInvoke },
		{ (int)ProjIds.ShotgunIce, ShotgunIceProj.rpcInvoke },
		{ (int)ProjIds.ShotgunIceCharged, ShotgunIceProjCharged.rpcInvoke },
		{ (int)ProjIds.ShotgunIceSled, ShotgunIceProjSled.rpcInvoke },
		{ (int)ProjIds.Hadouken, HadoukenProj.rpcInvoke} ,

		//X2 PROJS
		{ (int)ProjIds.CrystalHunter, CrystalHunterProj.rpcInvoke },
		{ (int)ProjIds.BubbleSplash, BubbleSplashProj.rpcInvoke },
		{ (int)ProjIds.BubbleSplashCharged, BubbleSplashProjCharged.rpcInvoke },
		{ (int)ProjIds.SilkShot, SilkShotProj.rpcInvoke },
		{ (int)ProjIds.SilkShotShrapnel, SilkShotProjShrapnel.rpcInvoke },
		{ (int)ProjIds.SilkShotChargedLv2, SilkShotProjLv2.rpcInvoke },
		{ (int)ProjIds.SilkShotCharged, SilkShotProjCharged.rpcInvoke },
		{ (int)ProjIds.SpinWheel, SpinWheelProj.rpcInvoke },
		{ (int)ProjIds.SpinWheelChargedStart, SpinWheelProjChargedStart.rpcInvoke },
		{ (int)ProjIds.SpinWheelCharged, SpinWheelProjCharged.rpcInvoke },
		{ (int)ProjIds.SonicSlicerStart, SonicSlicerStart.rpcInvoke },
		{ (int)ProjIds.SonicSlicer, SonicSlicerProj.rpcInvoke },
		{ (int)ProjIds.SonicSlicerCharged, SonicSlicerProjCharged.rpcInvoke },
		{ (int)ProjIds.StrikeChain, StrikeChainProj.rpcInvoke },
		{ (int)ProjIds.StrikeChainCharged, StrikeChainProjCharged.rpcInvoke },
		{ (int)ProjIds.MagnetMine, MagnetMineProj.rpcInvoke },
		{ (int)ProjIds.MagnetMineCharged, MagnetMineProjCharged.rpcInvoke },
		{ (int)ProjIds.SpeedBurner, SpeedBurnerProj.rpcInvoke },
		{ (int)ProjIds.SpeedBurnerWater, SpeedBurnerProjWater.rpcInvoke },
		{ (int)ProjIds.ItemTracer, ItemTracerProj.rpcInvoke },

		//X3 PROJS
		{ (int)ProjIds.AcidBurst, AcidBurstProj.rpcInvoke },
		{ (int)ProjIds.AcidBurstSmall, AcidBurstProjSmall.rpcInvoke },
		{ (int)ProjIds.AcidBurstCharged, AcidBurstProjCharged.rpcInvoke },
		{ (int)ProjIds.ParasiticBomb, ParasiticBombProj.rpcInvoke },
		{ (int)ProjIds.ParasiticBombCharged, ParasiticBombProjCharged.rpcInvoke },
		{ (int)ProjIds.TriadThunder, TriadThunderProj.rpcInvoke },
		{ (int)ProjIds.TriadThunderQuake, TriadThunderQuake.rpcInvoke },
		{ (int)ProjIds.TriadThunderCharged, TriadThunderProjCharged.rpcInvoke },
		{ (int)ProjIds.SpinningBlade, SpinningBladeProj.rpcInvoke },
		{ (int)ProjIds.SpinningBladeCharged, SpinningBladeProjCharged.rpcInvoke },
		{ (int)ProjIds.RaySplasher, RaySplasherProj.rpcInvoke },
		{ (int)ProjIds.RaySplasherChargedProj, RaySplasherTurretProj.rpcInvoke },
		{ (int)ProjIds.GravityWell, GravityWellProj.rpcInvoke },
		{ (int)ProjIds.GravityWellCharged, GravityWellProjCharged.rpcInvoke },
		{ (int)ProjIds.FrostShield, FrostShieldProj.rpcInvoke },
		{ (int)ProjIds.FrostShieldAir, FrostShieldProjAir.rpcInvoke },
		{ (int)ProjIds.FrostShieldGround, FrostShieldProjGround.rpcInvoke },
		{ (int)ProjIds.FrostShieldCharged, FrostShieldProjCharged.rpcInvoke },
		{ (int)ProjIds.FrostShieldChargedGrounded, FrostShieldProjChargedGround.rpcInvoke },
		{ (int)ProjIds.FrostShieldPlatform, FrostShieldProjPlatform.rpcInvoke },
		{ (int)ProjIds.TornadoFang, TornadoFangProj.rpcInvoke },
		{ (int)ProjIds.TornadoFang2, TornadoFangProj.rpcInvoke },
		{ (int)ProjIds.TornadoFangCharged, TornadoFangProjCharged.rpcInvoke },
		{ (int)ProjIds.XSaberProj, XSaberProj.rpcInvoke },

		//EXTRA
		{ (int)ProjIds.UPParryMelee, UPParryMeleeProj.rpcInvoke },
		{ (int)ProjIds.UPParryProj, UPParryRangedProj.rpcInvoke },
		
		// Vile stuff.
		{ (int)ProjIds.FrontRunner, VileCannonProj.rpcInvoke },
		{ (int)ProjIds.FatBoy, VileCannonProj.rpcInvoke },
		{ (int)ProjIds.LongshotGizmo, VileCannonProj.rpcInvoke },
		// Zero
		{ (int)ProjIds.SuiretsusanProj, SuiretsusenProj.rpcInvoke },
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
		{ (int)ProjIds.VoltCSuck, VoltCSuckProj.rpcInvoke },
		{ (int)ProjIds.TSeahorseAcid2, TSeahorseAcid2Proj.rpcInvoke },
		{ (int)ProjIds.WSpongeSpike, WSpongeSpike.rpcInvoke },
		{ (int)ProjIds.BBuffaloIceProj, BBuffaloIceProj.rpcInvoke },
		//Axl
		{ (int)ProjIds.BlackArrowGround, BlackArrowGrounded.rpcInvoke },
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
		{ (int)RockProjIds.ScorchWheelSpawn, ScorchWheelSpawn.rpcInvoke},
		{ (int)RockProjIds.ScorchWheel, ScorchWheelProj.rpcInvoke},
		{ (int)RockProjIds.ScorchWheelMove, ScorchWheelMoveProj.rpcInvoke },
		{ (int)RockProjIds.ScorchWheelUnderwater, UnderwaterScorchWheelProj.rpcInvoke},
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
		{ (int)BluesProjIds.StarCrash2, StarCrashProj2.rpcInvoke },
		{ (int)BluesProjIds.ProtoStrike, ProtoStrikeProj.rpcInvoke },
		{ (int)BluesProjIds.ProtoStrikePush, StrikeAttackPushProj.rpcInvoke },
		{ (int)BluesProjIds.RedStrike, RedStrikeProj.rpcInvoke },
		{ (int)BluesProjIds.RedStrikeExplosion, RedStrikeExplosionProj.rpcInvoke },
		{ (int)BluesProjIds.BigBangStrike, BigBangStrikeProj.rpcInvoke },
		{ (int)BluesProjIds.BigBangStrikeExplosion, BigBangStrikeExplosionProj.rpcInvoke },

		//Bass stuff.
		{ (int)BassProjIds.BassLemon, BassBusterProj.rpcInvoke },
		{ (int)BassProjIds.CopyVisionLemon, CopyVisionLemon.rpcInvoke },
		{ (int)BassProjIds.CopyVisionLemonAlt, CopyVisionLemonAlt.rpcInvoke },
		{ (int)BassProjIds.IceWall, IceWallProj.rpcInvoke },
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
		{ (int)BassProjIds.LightningBolt, LightningBoltProj.rpcInvoke },

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
	public Actor owner;
}

public delegate Projectile ProjCreate(ProjParameters arg);
