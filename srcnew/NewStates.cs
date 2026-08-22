using System;

namespace MMXOnline;

public class HealState : CharState {
    
    Tank tank;
    public HealState(Tank tank) : base("win") {
        this.tank = tank;
    }

	public override void update() {
		base.update();

        tank.heal(player, character);

        if (!tank.isHealing) {
            character.changeToIdleOrFall();
        }
	}

	public override void onExit(CharState? newState) {
		base.onExit(newState);
        if (tank?.buff != null) {
            character.buffList.Remove(tank.buff);
            tank.buff = null;
        }
	}
}