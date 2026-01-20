namespace MMXOnline;

public enum PickupType {
	Health,
	Ammo,
	Bolts
}

public enum PickupTypeRpc {
	LargeHealth,
	SmallHealth,
	LargeAmmo,
	SmallAmmo
}

public class Pickup : Actor {
	public float healAmount = 0;
	public float altHealAmount = 0;
	public PickupType pickupType;
	public bool teamOnly;

	public Pickup(
		Player owner, Point pos, string sprite, ushort? netId,
		bool ownedByLocalPlayer, NetActorCreateId netActorCreateId, bool sendRpc = false
	) : base(
		sprite, pos, netId, ownedByLocalPlayer, false
	) {
		canBeLocal = true;
		netOwner = owner;
		if (collider != null) {
			collider.wallOnly = true;
			collider.isTrigger = false;
		}

		this.netActorCreateId = netActorCreateId;
		if (sendRpc) {
			createActorRpc(owner.id);
		}
	}

	public override void update() {
		base.update();
		var leeway = 500;
		if (ownedByLocalPlayer && pos.x > Global.level.width + leeway || pos.x < -leeway || pos.y > Global.level.height + leeway || pos.y < -leeway) {
			destroySelf();
		}
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (other.otherCollider?.flag == (int)HitboxFlag.Hitbox) return;

		if (other.gameObject is Character chr) {
			if (!chr.ownedByLocalPlayer) return;
			if (teamOnly && chr.player != netOwner) return;
			use(chr);
		}
	}

	public virtual void use(Character chr) {
		destroySelf(doRpcEvenIfNotOwned: true);
	}
}

#region Health
public abstract class BaselineHealthPickup : Pickup {
	public BaselineHealthPickup(
		Player owner, Point pos, string sprite, ushort? netId,
		bool ownedByLocalPlayer, NetActorCreateId netActorCreateId,
		bool sendRpc = false
	) : base(
		owner, pos, sprite, netId, ownedByLocalPlayer,
		netActorCreateId, sendRpc: sendRpc
	) {
		pickupType = PickupType.Health;
		healAmount = 8;
	}

	public override void use(Character chr) {
		if (!chr.canBeHealed()) {
			return;
		}
		chr.addHealth(healAmount);
		base.use(chr);
	}
}

public class GiantHealthPickup : BaselineHealthPickup {
	public GiantHealthPickup(
		Player owner, Point pos, ushort? netId, 
		bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		owner, pos, "pickup_health_giant", netId, ownedByLocalPlayer,
		NetActorCreateId.GiantHealth, sendRpc: sendRpc
	) {
		healAmount = 64;
	}
}

public class LargeHealthPickup : BaselineHealthPickup {
	public LargeHealthPickup(
		Player owner, Point pos, ushort? netId, 
		bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		owner, pos, "pickup_health_large", netId, ownedByLocalPlayer, 
		NetActorCreateId.LargeHealth, sendRpc: sendRpc
	) {
		healAmount = 8;
		pickupType = PickupType.Health;
	}
}

public class SmallHealthPickup : BaselineHealthPickup {
	public SmallHealthPickup(
		Player owner, Point pos, ushort? netId,
		bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		owner, pos, "pickup_health_small", netId, ownedByLocalPlayer,
		NetActorCreateId.SmallHealth, sendRpc: sendRpc
	) {
		healAmount = 4;
		pickupType = PickupType.Health;
	}
}

public class MicroHealthPickup : BaselineHealthPickup {
	public MicroHealthPickup(
		Player owner, Point pos, ushort? netId,
		bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		owner, pos, "pickup_health_micro", netId, ownedByLocalPlayer,
		NetActorCreateId.SmallHealth, sendRpc: sendRpc
	) {
		healAmount = 4;
		pickupType = PickupType.Health;
	}
}
#endregion

#region Ammo
public abstract class BaselineAmmoPickup : Pickup {
	public BaselineAmmoPickup(
		Player owner, Point pos, string sprite, ushort? netId,
		bool ownedByLocalPlayer, NetActorCreateId netActorCreateId,
		bool sendRpc = false
	) : base(
		owner, pos, sprite, netId, ownedByLocalPlayer,
		netActorCreateId, sendRpc: sendRpc
	) {
		healAmount = 50;
		altHealAmount = 8;
		pickupType = PickupType.Ammo;
	}

	public override void use(Character chr) {
		if (!chr.canAddAmmo()) {
			return;
		}
		if (chr is Blues blues) {
			blues.healCore(altHealAmount);
		} else {
			//Adrian: Use this one instead to swap to HDM Ammo System.
			//        (Remember to adjust the heal values too).
			//chr.addAmmo(healAmount);
			chr.addPercentAmmo(healAmount);
		}
		base.use(chr);
	}
}

public class GiantAmmoPickup : BaselineAmmoPickup {
	public GiantAmmoPickup(
		Player owner, Point pos, ushort? netId, 
		bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		owner, pos, "pickup_ammo_giant", netId, ownedByLocalPlayer,
		NetActorCreateId.GiantAmmo, sendRpc: sendRpc
	) {
		healAmount = 100;
		altHealAmount = 64;
		pickupType = PickupType.Ammo;
	}
}

public class LargeAmmoPickup : BaselineAmmoPickup {
	public LargeAmmoPickup(
		Player owner, Point pos, ushort? netId, 
		bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		owner, pos, "pickup_ammo_large", netId, ownedByLocalPlayer, 
		NetActorCreateId.LargeAmmo, sendRpc: sendRpc
	) {
		healAmount = 50;
		altHealAmount = 8;
		pickupType = PickupType.Ammo;
	}
}

public class SmallAmmoPickup : BaselineAmmoPickup {
	public SmallAmmoPickup(
		Player owner, Point pos, ushort? netId, 
		bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		owner, pos, "pickup_ammo_small", netId, ownedByLocalPlayer, 
		NetActorCreateId.SmallAmmo, sendRpc: sendRpc
	) {
		healAmount = 25;
		altHealAmount = 4;
		pickupType = PickupType.Ammo;
	}
}

public class MicroAmmoPickup : BaselineAmmoPickup {
	public MicroAmmoPickup(
		Player owner, Point pos, ushort? netId, 
		bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		owner, pos, "pickup_ammo_micro", netId, ownedByLocalPlayer, 
		NetActorCreateId.SmallAmmo, sendRpc: sendRpc
	) {
		healAmount = 12.5f;
		altHealAmount = 2;
		pickupType = PickupType.Ammo;
	}
}
#endregion

#region Bolts
public abstract class BaselineBoltPickup : Pickup {
	public BaselineBoltPickup(
		Player owner, Point pos, string sprite, ushort? netId,
		bool ownedByLocalPlayer, NetActorCreateId netActorCreateId,
		bool sendRpc = false
	) : base(
		owner, pos, sprite, netId, ownedByLocalPlayer,
		netActorCreateId, sendRpc: sendRpc
	) {
		healAmount = 8;
		teamOnly = true;
		pickupType = PickupType.Bolts;
	}

	public override void use(Character chr) {	
		chr.player.currency += (int)healAmount;
		chr.playSound("bolt");
		base.use(chr);
	}
}

public class GiantBoltPickup : BaselineBoltPickup {
	public GiantBoltPickup(
		Player owner, Point pos, ushort? netId, 
		bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		owner, pos, "pickup_ammo_giant", netId, ownedByLocalPlayer,
		NetActorCreateId.GiantBolt, sendRpc: sendRpc
	) {
		healAmount = 32;
		teamOnly = true;
		pickupType = PickupType.Bolts;
	}
}

public class LargeBoltPickup : BaselineBoltPickup {
	public LargeBoltPickup(
		Player owner, Point pos, ushort? netId,
		bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		owner, pos, "pickup_bolt_large", netId, ownedByLocalPlayer,
		NetActorCreateId.LargeBolt, sendRpc: sendRpc
	) {
		healAmount = 8;
		pickupType = PickupType.Bolts;
		teamOnly = true;
	}
}

public class SmallBoltPickup : BaselineBoltPickup {
	public SmallBoltPickup(
		Player owner, Point pos, ushort? netId,
		bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		owner, pos, "pickup_bolt_small", netId, ownedByLocalPlayer,
		NetActorCreateId.SmallBolt, sendRpc: sendRpc
	) {
		healAmount = 2;
		pickupType = PickupType.Bolts;
		teamOnly = true;
	}
}
#endregion
