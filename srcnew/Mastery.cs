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
	public float mapExp;
	public int mapLvLimit = 30;
	public int mapLevel = 1;
	public int mapLevelStacks;
	public bool mainCharActive => player == Global.level.mainPlayer && player.character != null;

	public MasteryTracker(Player player, CharIds charId) {
		this.player = player;
		this.charId = (int)charId;
	}

	public void addDamageExp(float value, bool sendRpc = false) {
		if (sendRpc) {
			RPC.creditExp.sendRpc(player, charId, 0, value);	
		}
		if (!player.ownedByLocalPlayer) { return; }
		value = roundedShortExp(value);
		damageExp += value;
		while (damageExp >= damageLvLimit) {
			damageExp -= damageLvLimit;
			grantDamageLevel();
		}
	}
	public void addDefenseExp(float value, bool sendRpc = false) {
		value = roundedShortExp(value);
		if (sendRpc) {
			RPC.creditExp.sendRpc(player, charId, 1, value);	
		}
		if (!player.ownedByLocalPlayer) { return; }
		defenseExp += value;
		while (defenseExp >= defenseLvLimit) {
			defenseExp -= defenseLvLimit;
			grantDefenseLevel();
		}
	}
	public void addSupportExp(float value, bool sendRpc = false) {
		value = roundedShortExp(value);
		if (sendRpc) {
			RPC.creditExp.sendRpc(player, charId, 2, value);
		}
		if (!player.ownedByLocalPlayer) { return; }
		supportExp += value;
		while (supportExp >= supportLvLimit) {
			supportExp -= supportLvLimit;
			grantSupportLevel();
		}
	}
	public void addMapExp(float value, bool sendRpc = false) {
		value = roundedShortExp(value);
		if (sendRpc) {
			RPC.creditExp.sendRpc(player, charId, 3, value);
		}
		if (!player.ownedByLocalPlayer) { return; }
		mapExp += value;
		while (mapExp >= mapLvLimit) {
			mapExp -= mapLvLimit;
			grantMapLevel();
		}
	}

	public void grantDamageLevel() {
		damageLevelStacks++;
		if (damageLevelStacks >= MathF.Ceiling(damageLevel / 5f)) {
			damageLevel++;
			damageLevelStacks = 0;
			if (mainCharActive) {
				player.character.addDamageText($"ATK L{damageLevel}!", (int)FontType.RedSmall);
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
		if (defenseLevelStacks >= MathF.Ceiling(defenseLevel / 5f)) {
			defenseLevel++;
			defenseLevelStacks = 0;
			if (mainCharActive) {
				player.character.addDamageText($"DEF L{defenseLevel}!", (int)FontType.BlueSmall);
			}
		}
		player.awardCurrency(charId, 4);
		if (mainCharActive) {
			createBoltsAtPos(player.character.getCenterPos(), 2);
		}
	}
	public void grantSupportLevel() {
		supportLevelStacks++;
		if (supportLevelStacks >= MathF.Ceiling(supportLevel / 5f)) {
			supportLevel++;
			supportLevelStacks = 0;
			if (mainCharActive) {
				player.character.addDamageText($"SP L{supportLevel}!", (int)FontType.GreenSmall);
			}
		}
		player.awardCurrency(charId, 6);
		if (mainCharActive) {
			createBoltsAtPos(player.character.getCenterPos(), 3);
		}
	}
	public void grantMapLevel() {
		mapLevelStacks++;
		if (mapLevelStacks >= MathF.Ceiling(mapLevel / 5f)) {
			mapLevel++;
			mapLevelStacks = 0;
			if (mainCharActive) {
				player.character.addDamageText($"MAP L{mapLevel}!", (int)FontType.PurpleSmall);
			}
		}
		player.awardCurrency(charId, 2);
		if (mainCharActive) {
			createBoltsAtPos(player.character.getCenterPos(), 1);
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
