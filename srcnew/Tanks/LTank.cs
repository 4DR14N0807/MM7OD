using System;
using System.Linq;

namespace MMXOnline;

public class LTank : Tank {
	int heals = 0;

	public LTank() {
		maxHealth = 24;
		health = maxHealth;
		healMaxTime = 180;
	}

	public override void use(Player player, Character character) {
		if (character is not Blues || !character.charState.normalCtrl){
			return;
		}

		//blues.usedLtank = this;
		healAmount = Math.Min(health, Math.Ceiling(character.maxHealth / 3));
		character.changeState(new HealState(this));
		healTime = healMaxTime;
		healAmount = Math.Ceiling(character.maxHealth / 3);
		isHealing = true;
		healStacks = 0;
	}

	public override void heal(Player player, Character character) {
		Helpers.decrementFrames(ref healTime);

        if (healTime <= 0) {
            decimal hpToHeal = Math.Ceiling(Math.Min(healAmount, character.maxHealth - character.health));
            character.heal(player, (int)hpToHeal, drawHealText: true, creditHeal: false);
            health -= hpToHeal;
			if (hpToHeal == 0) {
	            character.playSound("heal", sendRpc: true);
			}
            player.fuseETanks();
            healTime = healMaxTime;
			healStacks++;
			//buff = new Buff("hud_buffs", 4, true, healTime, healMaxTime);
			//character.buffList.Add(buff);

			decimal shield = healAmount - hpToHeal;
			if (shield > 0 && healStacks >= 3 && character.canBeShielded()) {
				character.playSound("subtank_fill");
				int time = 60 * 15;
				Buff? shieldTarget = character.buffList.FirstOrDefault(
					b => b.update == BaselineShieldPickup.buffUpdate
				);
				if (shieldTarget == null) {
					character.buffList.Add(new Buff("hud_shields", 0, true, time, time) {
						update = BaselineShieldPickup.buffUpdate
					});
				}
				character.shieldManager.addShield(shield, time, ShieldIds.Pickup);
				health -= shield;
			}
        }

        if (health <= 0 || healStacks >= 3) { 
			isHealing = false;
			healStacks = 0;
			if (health <= 0) {
				player.ltanks.Remove(this);
			}
        }
	}

	public override bool isFull() {
		return health >= maxHealth && ammo >= maxAmmo;
	}
}
