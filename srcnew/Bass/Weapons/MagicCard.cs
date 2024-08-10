using System;
using System.Collections.Generic;

namespace MMXOnline;

public class MagicCard : Weapon {

	public static MagicCard netWeapon = new();
	public List<MagicCardProj> cardsOnField = new List<MagicCardProj>();

	public MagicCard() : base() {
		index = (int)BassWeaponIds.MagicCard;
		displayName = "MAGIC CARD";
		weaponSlotIndex = index;
		weaponBarBaseIndex = index;
		weaponBarIndex = index;
		fireRateFrames = 20;
	}

	public override void shoot(Character character, params int[] args) {
		if (character is not Bass bass) {
			return;
		}
		Point shootPos = character.getShootPos();
		float shootAngle = bass.getShootAngle(true, false);
		Player player = character.player;

		var card = new MagicCardProj(
			shootPos, character.getShootXDir(), shootAngle, player, player.getNextActorNetId(), true
		);
		cardsOnField.Add(card);
	}
}


public class MagicCardProj : Projectile {
	bool reversed;
	Character shooter;
	float maxReverseTime;
	const float projSpeed = 480;
	public Pickup? pickup;

	public MagicCardProj(
		Point pos, int xDir, float byteAngle, 
		Player player, ushort? netProjId, bool rpc = false
	) : base (
		MagicCard.netWeapon, pos, xDir, 0, 1,
		player, "magic_card_proj", 0, 0, 
		netProjId, player.ownedByLocalPlayer
	) {
		projId = (int)BassProjIds.MagicCard;
		maxTime = 3f;
		maxReverseTime = 0.45f;
		this.byteAngle = byteAngle;
		shooter = player.character;
		vel = Point.createFromByteAngle(byteAngle) * 425;
		canBeLocal = false;

		if (rpc) {
			rpcCreateByteAngle(pos, player, netId, byteAngle, (byte)(xDir + 1));
		}
	}

	public static Projectile rpcInvoke(ProjParameters arg) {
		return new MagicCardProj(
			arg.pos, arg.extraData[0] - 1, arg.byteAngle, arg.player, arg.netId
		);
	}

	public override void update() {
		base.update();

		if (ownedByLocalPlayer && (shooter == null || shooter.destroyed)) {
			destroySelf();
			return;
		}

		if (!reversed && time > maxReverseTime) reversed = true;

		if (reversed) {
			vel = new Point(0, 0);
			frameSpeed = -2;
			if (frameIndex == 0) frameIndex = sprite.totalFrameNum - 1;

			Point returnPos = shooter.getCenterPos();
			if (shooter.sprite.name.Contains("shoot")) {
				Point poi = shooter.pos;
				var pois = shooter.sprite.getCurrentFrame()?.POIs;
				if (pois != null && pois.Length > 0) {
					poi = pois[0];
				}
				returnPos = shooter.pos.addxy(poi.x * shooter.xDir, poi.y);
			}
			Point speed = pos.directionToNorm(returnPos).times(425);
			move(speed);
			byteAngle = speed.byteAngle;

			if (pos.distanceTo(returnPos) < 10) {
				destroySelf();
			}
		}

		if (!destroyed && pickup != null) {
			pickup.collider.isTrigger = true;
			pickup.useGravity = false;
			pickup.changePos(pos);
		}
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (!ownedByLocalPlayer) return;
		if (destroyed) return;

		if (other.gameObject is Pickup && pickup == null) {
			pickup = other.gameObject as Pickup;
			if (pickup != null) {
				if (!pickup.ownedByLocalPlayer) {
					pickup.takeOwnership();
					RPC.clearOwnership.sendRpc(pickup.netId);
				}
			}
			
		}

		var character = other.gameObject as Character;
		if (reversed && character != null && character.player == damager.owner) {
			if (pickup != null) {
				pickup.changePos(character.getCenterPos());
			}
			destroySelf();
		}
	}

	public override void onDestroy() {
		base.onDestroy();
		if (pickup != null) {
			pickup.useGravity = true;
			pickup.collider.isTrigger = false;
		}
	}
}
