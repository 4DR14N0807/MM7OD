using System;

namespace MMXOnline;

public class NeedleCannon : Weapon {
	public static NeedleCannon netWeapon = new();

    public NeedleCannon() : base() {
		displayName = "Needle Cannon";
		descriptionV2 = "Rapid fire cannon that deals fast damage.\nHas high heat generation.";
		ammoUseText = "14 per second.";

		index = (int)RockWeaponIds.NeedleCannon;
        fireRateFrames = 6;
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
		return 1.4f;
	}
}

public class NeedleCannonProj : Projectile {
    public NeedleCannonProj(
		Point pos, int xDir, Player player, ushort? netId, bool rpc = false
	) : base(
		NeedleCannon.netWeapon, pos, xDir, 400, 0.5f, player, "rock_buster_proj",
		0, 0, netId, player.ownedByLocalPlayer
	) {
        maxTime = 0.25f;
        projId = (int)BluesProjIds.NeedleCannon;
    }
}
