using System;

namespace MMXOnline;

public abstract class BaselineSuperPickup : Pickup {
	public BaselineSuperPickup(
		Player owner, Point pos, string sprite, ushort? netId,
		bool ownedByLocalPlayer, CActorIds cActorId,
		bool sendRpc = false, bool teamOnly = false
	) : base(
		owner, pos, sprite, netId, ownedByLocalPlayer,
		cActorId, sendRpc: sendRpc, teamOnly: teamOnly
	) {
		healAmount = 8;
		altHealAmount = 50;
	}

	public override void use(Character chr) {
		bool used = false;
		if (chr.canBeHealed()) {
			chr.addHealth(healAmount);
			used = true;
		}
		if (chr.canBeShielded()) {
			float shield = Math.Max(healAmount - 2, 2);
			chr.shieldManager.addShield(shield, 60 * 4, ShieldIds.Pickup);
			used = true;
		}
		if (chr.canAddAmmo()) {
			if (chr is Blues blues) {
				blues.healCore(healAmount);
			} else {
				chr.addPercentAmmo(altHealAmount);
			}
			used = true;
		}
		if (used) {
			base.use(chr);
		}
	}
}

public class TankSuperPickup : BaselineSuperPickup {
	public TankSuperPickup(
		Player owner, Point pos, ushort? netId, bool ownedByLocalPlayer,
		bool sendRpc = false, bool teamOnly = false
	) : base(
		owner, pos, "pickup_stank", netId, ownedByLocalPlayer, 
		CActorIds.TankSuperPickup, sendRpc: sendRpc, teamOnly: teamOnly
	) {
		healAmount = 8;
		altHealAmount = 75;
	}

	public static Actor rpcInvoke(ActorRpcParameters arg) {
		return new TankSuperPickup(
			arg.player, arg.pos, arg.netId, false, teamOnly: arg.extraData[0] == 1
		);
	}
}

public class LargeSuperPickup : BaselineSuperPickup {
	public LargeSuperPickup(
		Player owner, Point pos, ushort? netId, bool ownedByLocalPlayer,
		bool sendRpc = false, bool teamOnly = false
	) : base(
		owner, pos, "pickup_super_large", netId, ownedByLocalPlayer, 
		CActorIds.LargeSuperPickup, sendRpc: sendRpc, teamOnly: teamOnly
	) {
		healAmount = 6;
		altHealAmount = 50;
	}

	public static Actor rpcInvoke(ActorRpcParameters arg) {
		return new LargeSuperPickup(
			arg.player, arg.pos, arg.netId, false, teamOnly: arg.extraData[0] == 1
		);
	}
}

public class SmallSuperPickup : BaselineSuperPickup {
	public SmallSuperPickup(
		Player owner, Point pos, ushort? netId, bool ownedByLocalPlayer,
		bool sendRpc = false, bool teamOnly = false
	) : base(
		owner, pos, "pickup_super_small", netId, ownedByLocalPlayer, 
		CActorIds.SmallSuperPickup, sendRpc: sendRpc, teamOnly: teamOnly
	) {
		healAmount = 4;
		altHealAmount = 25;
	}

	public static Actor rpcInvoke(ActorRpcParameters arg) {
		return new SmallSuperPickup(
			arg.player, arg.pos, arg.netId, false, teamOnly: arg.extraData[0] == 1
		);
	}
}

public class MiniSuperPickup : BaselineSuperPickup {
	public MiniSuperPickup(
		Player owner, Point pos, ushort? netId, bool ownedByLocalPlayer,
		bool sendRpc = false, bool teamOnly = false
	) : base(
		owner, pos, "pickup_super_mini", netId, ownedByLocalPlayer, 
		CActorIds.MiniSuperPickup, sendRpc: sendRpc, teamOnly: teamOnly
	) {
		healAmount = 2;
		altHealAmount = 12.5f;
	}

	public static Actor rpcInvoke(ActorRpcParameters arg) {
		return new MiniSuperPickup(
			arg.player, arg.pos, arg.netId, false, teamOnly: arg.extraData[0] == 1
		);
	}
}