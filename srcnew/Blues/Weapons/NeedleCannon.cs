using System;

namespace MMXOnline;

public class NeedleCannon : Weapon {
	public static NeedleCannon netWeapon = new();

	public NeedleCannon() : base() {
		// Tecnical data.
		index = (int)RockWeaponIds.NeedleCannon;
		fireRateFrames = 6;
		defaultAmmoUse = 1.4f;

		// Display data.
		displayName = "Needle Cannon";
		descriptionV2 = "Rapid fire cannon that deals fast damage\nbut has high heat generation.";

		// Auto-calculation for ammo per second text.
		decimal ammoUseDec = Decimal.Parse(defaultAmmoUse.ToString());
		decimal chps = ammoUseDec * (60m / (decimal)fireRateFrames);
		string chpsString = Math.Ceiling(chps).ToString("#");

		// Ammo use text.
		ammoUseText = chpsString + " per second";
	}

	public override void shoot(Character character, params int[] args) {
		base.shoot(character, args);
		Point shootPos = character.getShootPos();
		int xDir = character.getShootXDir();
		Player player = character.player;
		var temp = new NeedleCannonProj(shootPos, xDir, player, player.getNextActorNetId(), true) {
			owningActor = character
		};
		temp.vel.y = Helpers.randomRange(0, 500) - 250;
		character.playSound("buster");
		character.xPushVel = 60 * -xDir;
	}

	public override float getAmmoUsage(int chargeLevel) {
		return defaultAmmoUse;
	}
}

public class NeedleCannonProj : Projectile {
	public NeedleCannonProj(
		Point pos, int xDir, Player player, ushort? netId, bool rpc = false
	) : base(
		NeedleCannon.netWeapon, pos, xDir, 400, 0.5f, player, "needle_cannon_proj",
		0, 0, netId, player.ownedByLocalPlayer
	) {
		maxTime = 0.25f;
		fadeSprite = "needle_cannon_proj_fade";
		projId = (int)BluesProjIds.NeedleCannon;
	}
}
