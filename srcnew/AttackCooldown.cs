using System;
using System.Collections.Generic;
using System.Linq;

namespace MMXOnline;

public class AttackCooldown {

	public float cooldown;
	public float maxCooldown;
	float modifier;
	public int iconIndex;

	public AttackCooldown(int iconIndex, float maxCooldown, float cooldown = 0, float modifier = 1) {
		this.iconIndex = iconIndex;
		this.cooldown = cooldown;
		this.maxCooldown = maxCooldown;
		this.modifier = modifier;
	}

	public void updateCooldown() {
		decrementCooldown();
	}

	public void decrementCooldown() {
		cooldown -= Global.speedMul * modifier;
		if (cooldown < 0) cooldown = 0;
	}
}
