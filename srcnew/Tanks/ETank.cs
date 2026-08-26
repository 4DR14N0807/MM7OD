using System;
using System.Linq;

namespace MMXOnline;


public class ETank : Tank {
	public bool inUse;

	public ETank() {
		maxHealth = 24;
		healAmount = 8;
		health = maxHealth;
		healMaxTime = 180;
	}

	public override void use(Player player, Character character) {
		if (!character.charState.normalCtrl) {
			return;
		}
		character.changeState(new HealState(this));
		healTime = healMaxTime;
		healAmount = Math.Ceiling(character.maxHealth / 3);
		healStacks = 0;
		isHealing = true;

		buff = new Buff("hud_buffs", 4, true, healTime, healMaxTime);
		//character.buffList.Add(buff);
		//RPC.useETank.sendRpc(character.netId, (int)maxHealth);
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
				player.ETanks.Remove(this);
			}
        }
	}
}
