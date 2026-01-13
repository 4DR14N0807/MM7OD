using System;
using SFML.Graphics;

namespace MMXOnline;

public class BaseTank {
	public (string name, int frame) sprite = ("menu_etank", 1);
	public bool inUse;
	public float healTime;
	public float ammo = 20;
	public float maxAmmo = 20;
	public float baseMaxAmmo = 20;
	public int ammoStacks;

	public BaseTank() {

	}

	public virtual void update(Character character) {}
	public virtual void use(Character character) {}
	public virtual bool canUse(Character character) { return false; }
	public virtual void stop(Character character) {}
	public virtual bool isFull() => ammo == maxAmmo;

	public virtual bool drawHealing(Point pos, float? customVal) {
		if (!inUse| customVal != null) {
			drawHealingInner(pos, customVal ?? ammo);
			return true;
		}
		return false;
	}

	public virtual void drawHealingInner(Point pos, float tankHealth) {
		Point topLeft = new Point(pos.x - 8, pos.y - 15);
		Point topLeftBar = new Point(pos.x - 7, topLeft.y + 2);
		Point botRightBar = new Point(pos.x + 7, topLeft.y + 14);

		Global.sprites[sprite.name].draw(
			sprite.frame, topLeft.x, topLeft.y, 1, 1, null, 1, 1, 1, ZIndex.HUD
		);
		float yPos = 12 * (1 - tankHealth / maxAmmo);
		DrawWrappers.DrawRect(
			topLeftBar.x, topLeftBar.y + yPos, botRightBar.x, botRightBar.y,
			true, new Color(0, 0, 0, 200), 1, ZIndex.HUD
		);
	}
}
