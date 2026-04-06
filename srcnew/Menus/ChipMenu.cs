using System;
using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;

namespace MMXOnline;

public class ChipMainMenu : IMainMenu {
	public IMainMenu prevMenu;
	public SubMenu[] subMenus;
	public SubMenu activeSubMenu;

	public ChipMainMenu(IMainMenu prevMenu) {
		this.prevMenu = prevMenu;

		List<SubMenu> subMenus = [
			new ETankMenu(changeSubMenu) { pos = new(16, 32) }
		];
		activeSubMenu = subMenus[0];

		this.subMenus = subMenus.ToArray();
	}

	public void update() {
		activeSubMenu?.updateSelected();

		foreach (SubMenu subMenu in subMenus) {
			subMenu.update(subMenu == activeSubMenu);
		}
	}

	public void changeSubMenu(SubMenu? subMenu) {
		if (subMenu == null) {
			Menu.change(prevMenu);
			return;
		}
		activeSubMenu = subMenu;
	}

	public void render() {
		// Background.
		DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
		Fonts.drawText(FontType.Blue, "UPGRADE MENU", Global.screenW * 0.5f, 16, Alignment.Center);
		Fonts.drawText(
			FontType.WhiteMini,
			Global.nameCoins + ": " + Global.level.mainPlayer.currency,
			Global.screenW - 16, 16, Alignment.Right
		);

		foreach (SubMenu subMenu in subMenus) {
			subMenu.render(subMenu == activeSubMenu);
		}
	}
}

public class ETankMenu : SubMenu {
	public Player player => Global.level.mainPlayer;
	public Character? character => Global.level.mainPlayer?.character;

	public ETankMenu(Action<SubMenu?> changeMenu) : base(changeMenu) {
	}

	public override void updateSelected() {
		if (Global.input.isPressedMenu(Control.MenuBack)) {
			changeMenu(null);
			return;
		}
	}

	public override void render(bool selected) {
		renderTank(
			"HP 24", "Cost 20", "menu_etank", 0,
			pos, 13, 12, 4
		);
		renderTank(
			"EN 00", "Cost 10", "menu_tankbar_blankicon", 1,
			pos.addxy(Global.halfScreenW, 0), 0, 12, 4
		);
	}

	public void renderTank(
		string textL, string textR, string icon, int idx,
		Point pos, int ammo, int layerAmmo, int layers
	) {
		Global.sprites[icon].drawToHUD(idx, pos.x, pos.y);
		for (int i = 0; i < layers; i++) {
			int bAmmo = Math.Min(ammo - (layerAmmo * i), layerAmmo);
			if (bAmmo <= 0) {
				Global.sprites["menu_tankbar_empty"].drawToHUD(i == 0 ? 0 : 1, pos.x + 17 + i * 16, pos.y);
			} else {
				int ico = i == 0 ? 0 : 1;
				bool drawBar = true;
				if (bAmmo >= layerAmmo) {
					ico += 2;
					drawBar = false;
				}
				float posX = pos.x + 17 + i * 16;

				Global.sprites["menu_tankbar_full"].drawToHUD(ico, posX, pos.y);
				if (drawBar) {
					int size = MathInt.Round(15 * (bAmmo / (float)layerAmmo));
					DrawWrappers.DrawRectWH(
						posX + 2, pos.y + 2, size, 4,
						true, new(239, 99, 173), 0, ZIndex.HUD, false
					);
				}
			}
		}
		DrawWrappers.DrawRectWH(
			pos.x + 17, pos.y + 9, Fonts.measureText(FontType.WhiteMini, textL), 7,
			true, new(16, 16, 16), 0, ZIndex.HUD, false
		);
		Fonts.drawText(FontType.WhiteMini, textL, pos.x + 17, pos.y + 9);
	}
}