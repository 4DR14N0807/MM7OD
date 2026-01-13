using System;
using SFML.Graphics;

namespace MMXOnline;

public class ETank : BaseTank {
	public ETank(float maxAmmo = 20) {
		this.maxAmmo = maxAmmo;
		baseMaxAmmo = maxAmmo;
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

		ammo--;
		healTime = 8;
		character.addHealth(1);

		if (player == Global.level.mainPlayer && character.playHealSound) {
			character.playSound("heal", forcePlay: true, sendRpc: true);
			character.playHealSound = false;
		}

		if (ammo <= 0) {
			int id = (int)character.charId;
			player.eTanksMap[id] = null;
			ammo = 0;
			healTime = 0;
		}
	}

	public override void use(Character character) {
		character.usedEtank = this;
		inUse = true;
		RPC.useETank.sendRpc(character.netId, (int)maxAmmo);
	}

	public override bool canUse(Character character) {
		return (
			character.alive && character.healAmount <= 0 &&
			character.health < character.maxHealth &&
			character.charState is not Die
		);
	}

	public override void stop(Character character) {
		character.usedEtank = this;
		RPC.useETank.sendRpc(character.netId, (int)maxAmmo);
	}
}
