using System;
using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;

namespace MMXOnline;

public class SubMenu {
	public Point pos;
	public Action<SubMenu?> changeMenu;

	public SubMenu(Action<SubMenu?> changeMenu) {
		this.changeMenu = changeMenu;
	}

	public virtual void updateSelected() {
		
	}

	public virtual void update(bool selected) {
		
	}

	public virtual void render(bool selected) {
		
	}
}

public class GridSubMenu<T> : SubMenu {
	public Point cursorPos;
	public string sprite;
	public int?[,] icons;
	private T?[,] data;
	public bool allowNull;
	public bool warpX;
	public bool warpY;
	public Action<T?>? onSelect;
	public Action<T?>? onAlt;
	public Action<T?> onExit;

	public GridSubMenu(
		Action<SubMenu?> changeMenu,
		string sprite, int?[,] icons, T?[,] data, bool? allowNull = false,
		Action<T?>? onSelect = null, Action<T?>? onAlt = null, Action<T?>? onExit = null
	) : base(
		changeMenu
	) {
		this.sprite = sprite;
		this.icons = icons;
		this.data = data;
		this.allowNull = allowNull ?? false;
		this.onSelect = onSelect;
		this.onAlt = onAlt;
		this.onExit = onExit ?? defatulExit;
	}

	public override void updateSelected() {
		base.updateSelected();
		if (onSelect != null && Global.input.isPressedMenu(Control.MenuConfirm)) {
			menuSelect(onSelect, pos);
			return;
		}
		else if (onAlt != null && Global.input.isPressedMenu(Control.MenuAlt)) {
			menuSelect(onAlt, pos);
			return;
		}
		else if (Global.input.isPressedMenu(Control.MenuBack)) {
			menuSelect(onExit, pos);
			return;
		}
		updateDPad();
	}

	// Default exit function, by default it just exits the submenu.
	// Made the default here to not repeat this hundred of time times over.
	public void defatulExit(T? target) {
		changeMenu(null);
	}

	// ------------------------------------------
	// General Menu Functions.
	public void menuSelect(Action<T?> selectAction, Point pos) {
		T? target = data[(int)pos.x, (int)pos.y];

		if (target != null || allowNull) {
			selectAction(target);
		}
	}

	public void updateDPad() {
		Point menuDir = Global.input.getMenuDir();
		cursorPos += menuDir;
		int sizeX = data.GetLength(0);
		int sizeY = data.GetLength(1);

		if (cursorPos.x >= sizeX) {
			if (warpX) { cursorPos.x = 0; }
			else { cursorPos.x = sizeX - 1; }
		}
		if (cursorPos.x < 0) {
			if (warpX) { cursorPos.x = sizeX - 1; }
			else { cursorPos.x = 0; }
		}
		if (cursorPos.y >= sizeY) {
			if (warpY) { cursorPos.y = 0; }
			else { cursorPos.y = sizeY - 1; }
		}
		if (cursorPos.y < 0) {
			if (warpY) { cursorPos.x = sizeY - 1; }
			else { cursorPos.y = 0; }
		}
	}

	public override void render(bool selected) {
		
	}
}