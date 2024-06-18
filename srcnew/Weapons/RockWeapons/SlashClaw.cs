using System;
using System.Collections.Generic;
using System.Security;

namespace MMXOnline;

public class SlashClawWeapon : Weapon {

    public SlashClawWeapon(Player player) : base() {
        damager = new Damager(player, 3, 0, 0.5f);
        index = (int)RockWeaponIds.SlashClaw;
        weaponBarBaseIndex = (int)RockWeaponBarIds.SlashClaw;
        weaponBarIndex = weaponBarBaseIndex;
        weaponSlotIndex = (int)RockWeaponSlotIds.SlashClaw;
        //shootSounds = new List<string>() {"slash_claw", "slash_claw", "slash_claw", ""};
        killFeedIndex = 0;
        maxAmmo = 18;
        ammo = maxAmmo;
        rateOfFire = 1f;
        description = new string[] {"Fast melee attack, requires proper spacing.", "No tiene flinch."};
    }

    public override void getProjectile(Point pos, int xDir, Player player, float chargeLevel, ushort netProjId) {
        base.getProjectile(pos, xDir, player, chargeLevel, netProjId);

        if (player.character.ownedByLocalPlayer) {
            /*if (chargeLevel >= 2 && player.hasBusterLoadout()) {
                player.character.changeState(new RockChargeShotState(player.character.grounded), true);
            }*/
            if (player.character.charState is LadderClimb) {
                player.character.changeState(new ShootAltLadder(this, (int)chargeLevel), true);
            } else {
                player.character.changeState(new ShootAlt(this, (int)chargeLevel), true);
            }
            player.character.playSound("slash_claw", sendRpc: true);
        }
	}
}

public class SlashClawState : CharState {

    bool grounded;
    bool fired;

    public SlashClawState(bool grounded) : base(grounded ? "slashclaw" : "slashclaw_air", "", "", "") {
        this.grounded = grounded;
		landSprite = "slashclaw";
		airMove = true;
    }

    public override void update() {
        base.update();

        if (!fired && character.frameIndex >= 6) {
			player.weapon.addAmmo(-1, player);
			fired = true;
    	}

       if (character.isAnimOver()) {
			character.changeToIdleOrFall();
		}
    }

    public override void onEnter(CharState oldState) {
		base.onEnter(oldState);
		character.useGravity = true;
	}

	public override void onExit(CharState newState) {
		base.onExit(newState);
        character.useGravity = true;
		
	}
}