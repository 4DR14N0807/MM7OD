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
	public float coreHealAmount = 0;
	public PickupType pickupType;

	public Pickup(
		Player owner, Point pos, string sprite, ushort? netId,
		bool ownedByLocalPlayer, NetActorCreateId netActorCreateId, bool sendRpc = false
	) : base(
		sprite, pos, netId, ownedByLocalPlayer, false
	) {
		netOwner = owner;
		collider.wallOnly = true;
		collider.isTrigger = false;

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
		if (other.otherCollider.flag == (int)HitboxFlag.Hitbox) return;

		if (other.gameObject is Character chr) {
			if (!chr.ownedByLocalPlayer) return;

			if (pickupType == PickupType.Health) {
				if (chr.health >= chr.maxHealth) return;
				chr.addHealth(healAmount);
				destroySelf(doRpcEvenIfNotOwned: true);
			} else if (pickupType == PickupType.Ammo) {
				if (chr.canAddAmmo()) {
					if (chr is Blues blues) {
						blues.healCore(coreHealAmount);
					} else {
						chr.addPercentAmmo(healAmount); //Adrian: Use this one instead to swap to HDM Ammo System (Remember to adjust the heal values too).
						//chr.addAmmo(healAmount);
					}
					destroySelf(doRpcEvenIfNotOwned: true);
				}
			} else if (pickupType == PickupType.Bolts) {
				if (chr.player == netOwner) {
					chr.player.currency += (int)healAmount;
					destroySelf(doRpcEvenIfNotOwned: true);
				}	
			}
		}
	}
}

public class LargeHealthPickup : Pickup {
	public LargeHealthPickup(
		Player owner, Point pos, ushort? netId, 
		bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		owner, pos, "pickup_health_large", netId, ownedByLocalPlayer, 
		NetActorCreateId.LargeHealth, sendRpc: sendRpc
	) {
		healAmount = 7;
		pickupType = PickupType.Health;
	}
}

public class SmallHealthPickup : Pickup {
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

public class LargeAmmoPickup : Pickup {
	public LargeAmmoPickup(
		Player owner, Point pos, ushort? netId, 
		bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		owner, pos, "pickup_ammo_large", netId, ownedByLocalPlayer, 
		NetActorCreateId.LargeAmmo, sendRpc: sendRpc
	) {
		healAmount = 50;
		coreHealAmount = 8;
		pickupType = PickupType.Ammo;
	}
}

public class SmallAmmoPickup : Pickup {
	public SmallAmmoPickup(
		Player owner, Point pos, ushort? netId, 
		bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		owner, pos, "pickup_ammo_small", netId, ownedByLocalPlayer, 
		NetActorCreateId.SmallAmmo, sendRpc: sendRpc
	) {
		healAmount = 25;
		coreHealAmount = 4;
		pickupType = PickupType.Ammo;
	}
}

public class LargeBoltPickup : Pickup {

	bool isFalling;
	bool isColliding;
	public LargeBoltPickup(
		Player owner, Point pos, ushort? netId,
		bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		owner, pos, "pickup_bolt_large", netId, ownedByLocalPlayer,
		NetActorCreateId.LargeBolt, sendRpc: sendRpc
	) {
		healAmount = 10;
		pickupType = PickupType.Bolts;
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);

		if (isColliding && isFalling) {
			stopMoving();
		}
	}

	public override void update() {
		base.update();

		if (vel.y > 0) isFalling = true;
		if (!tryMove(vel, out _)) {
			isColliding = true;
		}
	}
}

public class SmallBoltPickup : Pickup {

	bool isFalling;
	bool isColliding;
	public SmallBoltPickup(
		Player owner, Point pos, ushort? netId,
		bool ownedByLocalPlayer, bool sendRpc = false
	) : base(
		owner, pos, "pickup_bolt_small", netId, ownedByLocalPlayer,
		NetActorCreateId.SmallBolt, sendRpc: sendRpc
	) {
		healAmount = 5;
		pickupType = PickupType.Bolts;
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);

		if (isColliding && isFalling) {
			stopMoving();
		}
	}

	public override void update() {
		base.update();

		if (vel.y > 0) isFalling = true;
		if (!tryMove(vel, out _)) {
			isColliding = true;
		}
	}
}
