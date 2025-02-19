using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace MMXOnline;

public class MasteryTracker {
	public Player player;
	public int charId;

	public float damageExp;
	public int damageLvLimit = 20;
	public int damageLevel = 1;
	public int damageLevelStacks;
	public float defenseExp;
	public int defenseLvLimit = 24;
	public int defenseLevel = 1;
	public int defenseLevelStacks;
	public float supportExp;
	public int supportLvLimit = 16;
	public int supportLevel = 1;
	public int supportLevelStacks;
	public bool mainCharActive => player == Global.level.mainPlayer && player.character != null;

	public MasteryTracker(Player player, CharIds charId) {
		this.player = player;
		this.charId = (int)charId;
	}

	public void addDamageExp(float value, bool sendRpc = false) {
		value = roundedShortExp(value);
		damageExp += value;
		if (damageExp >= damageLvLimit) {
			damageExp -= damageLvLimit;
			grantDamageLevel();
		}
		if (sendRpc) {
			RPC.creditExp.sendRpc(player, charId, 0, value);	
		}
	}
	public void addDefenseExp(float value, bool sendRpc = false) {
		value = roundedShortExp(value);
		defenseExp += value;
		if (defenseExp >= defenseLvLimit) {
			defenseExp -= defenseLvLimit;
			grantDefenseLevel();
		}
		if (sendRpc) {
			RPC.creditExp.sendRpc(player, charId, 1, value);	
		}
	}
	public void addSupportExp(float value, bool sendRpc = false) {
		value = roundedShortExp(value);
		supportExp += value;
		if (supportExp >= supportLvLimit) {
			supportExp -= supportLvLimit;
			grantSupportLevel();
		}
		if (sendRpc) {
			RPC.creditExp.sendRpc(player, charId, 2, value);
		}
	}

	public void grantDamageLevel() {
		damageLevelStacks++;
		if (damageLevelStacks >= damageLevel) {
			damageLevel++;
			damageLevelStacks = 0;
			if (mainCharActive) {
				player.character.addDamageText($"ATK LV {damageLevel}!", (int)FontType.WhiteSmall);
			}
		}
		player.awardCurrency(charId, 10);
		if (mainCharActive) {
			Point spawnPos = player.character.getCenterPos();
			if (player.lastDamagedCharacter != null) {
				spawnPos = player.lastDamagedCharacter.getCenterPos();
			}
			createBoltsAtPos(spawnPos, 5);
		}
		
	}
	public void grantDefenseLevel() {
		defenseLevelStacks++;
		if (defenseLevelStacks >= defenseLevel) {
			defenseLevel++;
			defenseLevelStacks = 0;
			if (mainCharActive) {
				player.character.addDamageText($"DEF LV {defenseLevel}!", (int)FontType.WhiteSmall);
			}
		}
		player.awardCurrency(charId, 4);
		if (mainCharActive) {
			createBoltsAtPos(player.character.getCenterPos(), 2);
		}
	}
	public void grantSupportLevel() {
		supportLevelStacks++;
		if (supportLevelStacks >= supportLevel) {
			supportLevel++;
			supportLevelStacks = 0;
			if (mainCharActive) {
				player.character.addDamageText($"SP LV {supportLevel}!", (int)FontType.WhiteSmall);
			}
		}
		player.awardCurrency(charId, 6);
		if (mainCharActive) {
			createBoltsAtPos(player.character.getCenterPos(), 3);
		}
	}
	

	public float roundedShortExp(float exp) {
		if (exp > 255) { exp = 255; }
		ushort expBytes = (ushort)MathF.Ceiling(exp * 256f);
		return expBytes / 256f;
	}

	public void createBoltsAtPos(Point pos, int boltNum) {
		for (int i = 0; i < boltNum; i++) {
			ExpBolts bolt = new ExpBolts(player.character, pos);
			bolt.vel.y = Helpers.randomRange(-240, -180);
			bolt.vel.x = Helpers.randomRange(-120, 120);
		}
	}
}

public class ExpBolts : Actor {
	public Actor target;
	public float time;
	public bool selfDestroyed;
	public bool hommingOnActor;

	public ExpBolts(
		Actor target, Point pos
	) : base(
		"pickup_bolt_small", pos, null, true, false
	) {
		this.target = target;
		useGravity = true;
		zIndex = ZIndex.Actor;
		collider.wallOnly = true;
		collider.isTrigger = false;
	}

	public override void preUpdate() {
		base.preUpdate();
		time += speedMul;
	}

	public override void update() {
		base.update();
		if (!hommingOnActor && grounded) {
			vel.x = 0;
		}
		if (!hommingOnActor && time >= 50) {
			hommingOnActor = true;
			useGravity = false;
			vel = Point.zero;
			collider.wallOnly = false;
			collider.isTrigger = true;
		}
		if (hommingOnActor) {
			float distX = target.getCenterPos().x - pos.x; 
			float distY = target.getCenterPos().y - pos.y;
			vel = Point.zero;

			if (MathF.Abs(distX) > 5) {
				vel.x = MathF.Sign(distX) * 5 * 60;
			}
			if (MathF.Abs(distY) > 5) {
				vel.y = MathF.Sign(distY) * 5 * 60;
			}
		}
		if (time >= 60 * 4) {
			destroySelf();
		}
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (hommingOnActor && other.gameObject == target) {
			destroySelf();
			playSound("heal");
		}
	}

	public override void onDestroy() {
		base.onDestroy();
	}
}
