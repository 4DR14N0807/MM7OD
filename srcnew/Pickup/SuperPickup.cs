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

public class TankSuperPickup : BaselineAmmoPickup {
	public TankSuperPickup(
		Player owner, Point pos, ushort? netId, bool ownedByLocalPlayer,
		bool sendRpc = false, bool teamOnly = false
	) : base(
		owner, pos, "pickup_stank", netId, ownedByLocalPlayer, 
		CActorIds.STankSuperPickup, sendRpc: sendRpc, teamOnly: teamOnly
	) {
		healAmount = 8;
		altHealAmount = 50;
	}

	public static Actor rpcInvoke(ActorRpcParameters arg) {
		return new TankSuperPickup(
			arg.player, arg.pos, arg.netId, false, teamOnly: arg.extraData[0] == 1
		);
	}
}

public class LargeSuperPickup : BaselineAmmoPickup {
	public LargeSuperPickup(
		Player owner, Point pos, ushort? netId, bool ownedByLocalPlayer,
		bool sendRpc = false, bool teamOnly = false
	) : base(
		owner, pos, "pickup_super_large", netId, ownedByLocalPlayer, 
		CActorIds.LargeSuperPickup, sendRpc: sendRpc, teamOnly: teamOnly
	) {
		healAmount = 8;
		altHealAmount = 50;
	}

	public static Actor rpcInvoke(ActorRpcParameters arg) {
		return new LargeSuperPickup(
			arg.player, arg.pos, arg.netId, false, teamOnly: arg.extraData[0] == 1
		);
	}
}

public class SmallSuperPickup : BaselineAmmoPickup {
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

public class MicroSuperPickup : BaselineAmmoPickup {
	public MicroSuperPickup(
		Player owner, Point pos, ushort? netId, bool ownedByLocalPlayer,
		bool sendRpc = false, bool teamOnly = false
	) : base(
		owner, pos, "pickup_super_micro", netId, ownedByLocalPlayer, 
		CActorIds.MicroSuperPickup, sendRpc: sendRpc, teamOnly: teamOnly
	) {
		healAmount = 2;
		altHealAmount = 12.5f;
	}

	public static Actor rpcInvoke(ActorRpcParameters arg) {
		return new MicroSuperPickup(
			arg.player, arg.pos, arg.netId, false, teamOnly: arg.extraData[0] == 1
		);
	}
}