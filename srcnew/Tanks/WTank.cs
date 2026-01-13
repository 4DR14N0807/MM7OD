using System;

namespace MMXOnline;

public class WTank : BaseTank {
	public WTank() {
		maxAmmo = 100;
		baseMaxAmmo = 100;
		ammo = maxAmmo;
	}

	public override void update(Character character) {
		if (!inUse) {
			return;
		}
		if (!canUse(character)) {
			return;
		}
		if (healTime <= 0) {
			healTime -= character.speedMul;
			return;
		}
		Player player = character.player;

		healTime = 8;
		ammo -= 5;
		character.addWTankAddAmmo(5);

		if (player == Global.level.mainPlayer && character.playHealSound) {
			character.playSound("heal", forcePlay: true, sendRpc: true);
			character.playHealSound = false;
		}

		if (ammo <= 0) {
			int id = (int)character.charId;
			player.wTanksMap[id].Remove(this);
			ammo = 0;
			healTime = 0;
		}
	}
	
	public override void use(Character character) {
		character.usedWtank = this;
		inUse = true;
		RPC.useWTank.sendRpc(character.netId, (int)maxAmmo);
	}
}
