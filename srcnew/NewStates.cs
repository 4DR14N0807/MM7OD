using System;

namespace MMXOnline;

public class HealState : CharState {
    float healMaxTime = 25;
    float healTime;
    Tank tank;
    public HealState(Tank tank) : base("win") {
        healTime = healMaxTime;
        this.tank = tank;
        normalCtrl = true;
        attackCtrl = true;
    }

	public override void update() {
		base.update();

        groundCodeWithMove();

        Helpers.decrementFrames(ref healTime);

        if (healTime <= 0) {
            float hpToHeal = MathF.Min(tank.health, (float)(character.maxHealth - character.health));
            hpToHeal = MathF.Min(2, hpToHeal);
            character.health += (decimal)hpToHeal;
            tank.health -= hpToHeal;
            character.playSound("heal", sendRpc: true);
            player.fuseETanks();
            healTime = healMaxTime;
        }

        if (tank.health <= 0) {
            if (tank is ETank et) {
                player.ETanks.Remove(et);
            }
        }

        if (character.health >= character.maxHealth) {
            character.changeToIdleOrFall();
        }
	}
}