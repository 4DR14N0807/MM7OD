using System;
using System.Linq;

namespace MMXOnline;

public abstract class BaselineSuperPickup : Pickup {
	public BaselineSuperPickup(
		Player owner, Point pos, string sprite, ushort? netId,
		bool ownedByLocalPlayer, CActorIds cActorId,
		bool sendRpc = false, bool teamOnly = false, bool spawnUp = false, bool isRushPickup = false
	) : base(
		owner, pos, sprite, netId, ownedByLocalPlayer,
		cActorId, sendRpc: sendRpc, teamOnly: teamOnly, spawnUp: spawnUp, isRushPickup: isRushPickup
	) {
		healAmount = 8;
		altHealAmount = 50;
		syncOnLateJoin = true;
	}

	public override bool canUse(int alliance, Character chr) {
		return base.canUse(alliance, chr) && (chr.canBeHealed() || chr.canBeShielded() || chr.canAddAmmo());
	}

	public override void use(Character chr) {
		bool used = false;
		if (chr.canBeHealed()) {
			chr.addHealth(healAmount);
			used = true;
		}
		if (chr.canBeShielded()) {
			float shield = Math.Max(healAmount - 2, 2);
			chr.playSound("subtank_fill");
			int time = 60 * 15;
			Buff? shieldTarget = chr.buffList.FirstOrDefault(b => b.update == BaselineShieldPickup.buffUpdate);
			if (shieldTarget == null) {
				chr.buffList.Add(new Buff("hud_shields", 0, true, time, time) {
					update = BaselineShieldPickup.buffUpdate
				});
			}
			chr.shieldManager.addShield(shield, time, ShieldIds.Pickup);
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
		bool sendRpc = false, bool teamOnly = false, bool isRushPickup = false
	) : base(
		owner, pos, "pickup_stank", netId, ownedByLocalPlayer, 
		CActorIds.TankSuperPickup, sendRpc: sendRpc, teamOnly: teamOnly, isRushPickup: isRushPickup
	) {
		healAmount = 8;
		altHealAmount = 50;
	}

	public static Actor pickupInvoke(ActorLocalParameters arg, bool sendRpc) {
		return new TankSuperPickup(
			arg.player, arg.pos, arg.netId, true, sendRpc: sendRpc
		);
	}

	public static Actor rpcInvoke(ActorRpcParameters arg) {
		return new TankSuperPickup(
			arg.player, arg.pos, arg.netId, false, 
			teamOnly: arg.extraData[0] == 1, isRushPickup: arg.extraData[2] == 1
		);
	}
}

public class LargeSuperPickup : BaselineSuperPickup {
	public LargeSuperPickup(
		Player owner, Point pos, ushort? netId, bool ownedByLocalPlayer,
		bool sendRpc = false, bool teamOnly = false, bool isRushPickup = false
	) : base(
		owner, pos, "pickup_super_large", netId, ownedByLocalPlayer, 
		CActorIds.LargeSuperPickup, sendRpc: sendRpc, teamOnly: teamOnly, isRushPickup: isRushPickup
	) {
		healAmount = 6;
		altHealAmount = 37.5f;
	}

	public static Actor pickupInvoke(ActorLocalParameters arg, bool sendRpc) {
		return new LargeSuperPickup(
			arg.player, arg.pos, arg.netId, true, sendRpc: sendRpc
		);
	}

	public static Actor rpcInvoke(ActorRpcParameters arg) {
		return new LargeSuperPickup(
			arg.player, arg.pos, arg.netId, false, 
			teamOnly: arg.extraData[0] == 1, isRushPickup: arg.extraData[2] == 1
		);
	}
}

public class SmallSuperPickup : BaselineSuperPickup {
	public SmallSuperPickup(
		Player owner, Point pos, ushort? netId, bool ownedByLocalPlayer,
		bool sendRpc = false, bool teamOnly = false, bool isRushPickup = false
	) : base(
		owner, pos, "pickup_super_small", netId, ownedByLocalPlayer, 
		CActorIds.SmallSuperPickup, sendRpc: sendRpc, teamOnly: teamOnly, isRushPickup: isRushPickup
	) {
		healAmount = 4;
		altHealAmount = 25;
	}

	public static Actor pickupInvoke(ActorLocalParameters arg, bool sendRpc) {
		return new SmallSuperPickup(
			arg.player, arg.pos, arg.netId, true, sendRpc: sendRpc
		);
	}

	public static Actor rpcInvoke(ActorRpcParameters arg) {
		return new SmallSuperPickup(
			arg.player, arg.pos, arg.netId, false, 
			teamOnly: arg.extraData[0] == 1, isRushPickup: arg.extraData[2] == 1
		);
	}
}

public class MiniSuperPickup : BaselineSuperPickup {
	public MiniSuperPickup(
		Player owner, Point pos, ushort? netId, bool ownedByLocalPlayer,
		bool sendRpc = false, bool teamOnly = false, bool isRushPickup = false
	) : base(
		owner, pos, "pickup_super_mini", netId, ownedByLocalPlayer, 
		CActorIds.MiniSuperPickup, sendRpc: sendRpc, teamOnly: teamOnly, isRushPickup: isRushPickup
	) {
		healAmount = 2;
		altHealAmount = 12.5f;
	}

	public static Actor pickupInvoke(ActorLocalParameters arg, bool sendRpc) {
		return new MiniSuperPickup(
			arg.player, arg.pos, arg.netId, true, sendRpc: sendRpc
		);
	}

	public static Actor rpcInvoke(ActorRpcParameters arg) {
		return new MiniSuperPickup(
			arg.player, arg.pos, arg.netId, false, 
			teamOnly: arg.extraData[0] == 1, isRushPickup: arg.extraData[2] == 1
		);
	}
}
