namespace MMXOnline;

public enum PickupType {
	None,
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
	public Player ownerPlayer;
	public float healAmount = 0;
	public float altHealAmount = 0;
	public PickupType pickupType = PickupType.None;
	public bool teamOnly;

	public Pickup(
		Player owner, Point pos, string sprite, ushort? netId,
		bool ownedByLocalPlayer, CActorIds cActorId,
		bool sendRpc = false, bool teamOnly = false
	) : base(
		sprite, pos, netId, ownedByLocalPlayer, false
	) {
		this.teamOnly = teamOnly;
		canBeLocal = false;
		netOwner = owner;
		ownerPlayer = owner;

		if (collider != null) {
			collider.wallOnly = true;
			collider.isTrigger = false;
		}

		this.cActorId = cActorId;

		if (sendRpc) {
			RPC.createActor.sendRpc(this, owner, null, (byte)(teamOnly ? 1 : 0));
		}
	}

	public override void update() {
		base.update();
		if (!ownedByLocalPlayer) {
			return;
		}
		int leeway = 500;

		if (pos.x > Global.level.width + leeway
			|| pos.x < -leeway ||
			pos.y > Global.level.height + leeway ||
			pos.y < -leeway
		) {
			destroySelf();
		}
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (other.otherCollider?.flag == (int)HitboxFlag.Hitbox) {
			return;
		}
		if (other.gameObject is Character chr && chr.ownedByLocalPlayer) {
			if (!teamOnly || chr.player.teamAlliance == ownerPlayer.teamAlliance) {
				use(chr);
			}
		}
	}

	public virtual void use(Character chr) {
		destroySelf(doRpcEvenIfNotOwned: true);
	}
}
