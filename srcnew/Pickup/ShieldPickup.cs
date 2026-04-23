namespace MMXOnline;

public abstract class BaselineShieldPickup : Pickup {
	public BaselineShieldPickup(
		Player owner, Point pos, string sprite, ushort? netId,
		bool ownedByLocalPlayer, CActorIds cActorId,
		bool sendRpc = false, bool teamOnly = false, bool spawnUp = false
	) : base(
		owner, pos, sprite, netId, ownedByLocalPlayer,
		cActorId, sendRpc: sendRpc, teamOnly: teamOnly, spawnUp: spawnUp
	) {
		healAmount = 8;
		syncOnLateJoin = true;
	}

	public override void use(Character chr) {
		if (!chr.canBeShielded()) {
			return;
		}
		chr.playSound("subtank_fill");
		int time = 60 * 15;
		chr.buffList.Add(new Buff("hud_shields", 0, true, time, time) { update = buffUpdate });
		chr.shieldManager.addShield(healAmount, time, ShieldIds.Pickup);
		base.use(chr);
	}

	public static void buffUpdate(Buff self, Character chara) {
		if (chara.shieldManager.shieldsById.TryGetValue(ShieldIds.Pickup, out HpShield? value)) {
			self.time = value.time;
		} else {
			self.time = 0;
		}
	}
}

public class TankShieldPickup : BaselineShieldPickup {
	public TankShieldPickup(
		Player owner, Point pos, ushort? netId, bool ownedByLocalPlayer,
		bool sendRpc = false, bool teamOnly = false
	) : base(
		owner, pos, "pickup_atank", netId, ownedByLocalPlayer, 
		CActorIds.TankShieldPickup, sendRpc: sendRpc, teamOnly: teamOnly
	) {
		healAmount = 8;
	}

	public static Actor pickupInvoke(ActorLocalParameters arg, bool sendRpc) {
		return new TankShieldPickup(
			arg.player, arg.pos, arg.netId, false, sendRpc: sendRpc
		);
	}

	public static Actor rpcInvoke(ActorRpcParameters arg) {
		return new TankShieldPickup(
			arg.player, arg.pos, arg.netId, false, teamOnly: arg.extraData[0] == 1
		);
	}
}

public class LargeShieldPickup : BaselineShieldPickup {
	public LargeShieldPickup(
		Player owner, Point pos, ushort? netId, bool ownedByLocalPlayer,
		bool sendRpc = false, bool teamOnly = false
	) : base(
		owner, pos, "pickup_shield_large", netId, ownedByLocalPlayer, 
		CActorIds.LargeShieldPickup, sendRpc: sendRpc, teamOnly: teamOnly
	) {
		healAmount = 6;
	}

	public static Actor pickupInvoke(ActorLocalParameters arg, bool sendRpc) {
		return new LargeShieldPickup(
			arg.player, arg.pos, arg.netId, false, sendRpc: sendRpc
		);
	}

	public static Actor rpcInvoke(ActorRpcParameters arg) {
		return new LargeShieldPickup(
			arg.player, arg.pos, arg.netId, false, teamOnly: arg.extraData[0] == 1
		);
	}
}

public class SmallShieldPickup : BaselineShieldPickup {
	public SmallShieldPickup(
		Player owner, Point pos, ushort? netId, bool ownedByLocalPlayer,
		bool sendRpc = false, bool teamOnly = false
	) : base(
		owner, pos, "pickup_shield_small", netId, ownedByLocalPlayer, 
		CActorIds.SmallShieldPickup, sendRpc: sendRpc, teamOnly: teamOnly
	) {
		healAmount = 4;
	}

	public static Actor pickupInvoke(ActorLocalParameters arg, bool sendRpc) {
		return new SmallShieldPickup(
			arg.player, arg.pos, arg.netId, false, sendRpc: sendRpc
		);
	}

	public static Actor rpcInvoke(ActorRpcParameters arg) {
		return new SmallShieldPickup(
			arg.player, arg.pos, arg.netId, false, teamOnly: arg.extraData[0] == 1
		);
	}
}

public class MiniShieldPickup : BaselineShieldPickup {
	public MiniShieldPickup(
		Player owner, Point pos, ushort? netId, bool ownedByLocalPlayer,
		bool sendRpc = false, bool teamOnly = false
	) : base(
		owner, pos, "pickup_shield_mini", netId, ownedByLocalPlayer, 
		CActorIds.MiniShieldPickup, sendRpc: sendRpc, teamOnly: teamOnly
	) {
		healAmount = 2;
	}

	public static Actor pickupInvoke(ActorLocalParameters arg, bool sendRpc) {
		return new MiniShieldPickup(
			arg.player, arg.pos, arg.netId, false, sendRpc: sendRpc
		);
	}

	public static Actor rpcInvoke(ActorRpcParameters arg) {
		return new MiniShieldPickup(
			arg.player, arg.pos, arg.netId, false, teamOnly: arg.extraData[0] == 1
		);
	}
}
