using System;
using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;

namespace MMXOnline;

public class GirdMenuManager {
	(Texture texture, int height, int size) menuCursorSprite;
	(Texture texture, int height, int size) defaultIconSprite;

	public (int x, int y) dpadPressed = (0, 0);
	public (int x, int y) cursorPos = (0, 0);
	public int currentMenu = 0;
	public int prevMenu;
	public (int x, int y) prevPos = (0, 0);
	public int cursorSpr = 3;

	public GirdMenuManager() {
		menuCursorSprite = (Global.textures["loadout_cursor"], 3, 18);
		defaultIconSprite = (Global.textures["loadout_default_icons"], 4, 12);
	}

	// ------------------------------------------
	// Main Menu update function.
	public void renderGirdMenu
		(Point renderPos,
		short?[] data,
		int limit = Int32.MaxValue,
		(Texture texture, int height, int size)? spriteSheet = null,
		(int x, int y)? highlighPos = null,
		bool verticalDraw = false,
		int menuId = -1,
		short?[] menuCellBg = null,
		short?[] menuDefaultSpr = null
	) {
		float borderX = limit * 18;
		float borderY = MathF.Ceiling((float)data.Length / limit) * 18;
		if (verticalDraw) {
			(borderX, borderY) = (borderY, borderX);
		}
		DrawWrappers.DrawRectWH(
			renderPos.x - 2, renderPos.y - 2,
			borderX, borderY,
			filled: false,
			new Color(20, 86, 60), 1,
			1000000L, isWorldPos: false
		);

		int k = 0;
		for (int i = 0; k < data.Length; i++) {
			for (int j = 0; j < limit && k < data.Length; j++) {
				if (data[k] != null) {
					var drawPos = new Point(j * 18, i * 18);
					bool selected = (highlighPos != null && j == highlighPos.Value.x && i == highlighPos.Value.y);
					if (verticalDraw) {
						(drawPos.x, drawPos.y) = (drawPos.y, drawPos.x);
						selected = (highlighPos != null && j == highlighPos.Value.x && i == highlighPos.Value.y);
					}
					// BG Spr.
					int cellSpr = selected ? 1 : 0;
					if (menuCellBg != null && menuCellBg[k] != null) {
						cellSpr = (int)menuCellBg[k];
					}
					// Draw BG
					drawMenuSprite(
						menuCursorSprite, cellSpr,
						renderPos.x + drawPos.x - 2, renderPos.y + drawPos.y - 2
					);
					// Draw Contents
					if (spriteSheet?.texture != null && data[k] >= 0) {
						drawMenuSprite(
							spriteSheet.Value, data[k].Value,
							renderPos.x + drawPos.x + 1, renderPos.y + drawPos.y + 1,
							cellSpr != 2 ? 1 : 0.5f
						);
					} else if (menuDefaultSpr != null && k < menuDefaultSpr.Length &&
						  menuDefaultSpr[k] != null && menuDefaultSpr[k] >= 0
					  ) {
						drawMenuSprite(
							defaultIconSprite, menuDefaultSpr[k].Value,
							renderPos.x + drawPos.x + 1, renderPos.y + drawPos.y + 1
						);
					}
				}
				// Draw cursor
				if (currentMenu == menuId && (
					!verticalDraw && j == cursorPos.x && i == cursorPos.y ||
					verticalDraw && i == cursorPos.x && j == cursorPos.y
				)) {
					var drawPos = new Point(j * 18, i * 18);
					bool selected = (highlighPos != null && j == highlighPos.Value.x && i == highlighPos.Value.y);
					int curSpr = selected ? 4 : cursorSpr;
					if (menuCellBg != null && menuCellBg[k] != null) {
						curSpr = (int)menuCellBg[k] + 3;
					}
					if (verticalDraw) {
						(drawPos.x, drawPos.y) = (drawPos.y, drawPos.x);
						selected = (highlighPos != null && j == highlighPos.Value.x && i == highlighPos.Value.y);
					}
					drawMenuSprite(
						menuCursorSprite, data[k] != null ? curSpr : curSpr + 3,
						renderPos.x + drawPos.x - 2, renderPos.y + drawPos.y - 2
					);
				}
				k++;
			}
		}
	}

	// ------------------------------------------
	// Update a specific Menu
	public void menuUpdate(
		short?[] data,
		int limit,
		(int menu, int max)?[] neighbours,
		Action<int> onSelect = null,
		Action<int> onSelect2 = null,
		int vertical = -1,
		bool allowNull = false,
		bool allowNull2 = false
	) {
		if (onSelect != null && Global.input.isPressedMenu("jump")) {
			menuSelect(onSelect, data, limit, vertical, allowNull);
			return;
		} else if (onSelect2 != null && Global.input.isPressedMenu("shoot")) {
			menuSelect(onSelect2, data, limit, vertical, allowNull2);
			return;
		}

		cursorPos.x += dpadPressed.x;
		cursorPos.y += dpadPressed.y;

		if (dpadPressed.x != 0 || dpadPressed.y != 0) {
			Global.playSound("menuX2");
		}
		if (cursorPos.y < 0) {
			cursorPos.y = (data.Length / limit) - 1;
		} else if (cursorPos.y >= MathF.Ceiling((float)data.Length / (float)limit)) {
			cursorPos.y = 0;
		}
		if (cursorPos.x >= limit) {
			cursorPos.x = 0;
			if (neighbours[1] != null) {
				currentMenu = neighbours[1].Value.menu;
				cursorPos.x = neighbours[1].Value.max;
			}
		} else if (cursorPos.x < 0) {
			cursorPos.x = limit - 1;
			if (neighbours[0] != null) {
				currentMenu = neighbours[0].Value.menu;
				cursorPos.x = neighbours[0].Value.max - 1;
			}
		}
	}

	public void menuSelect(Action<int> selectAction, short?[] data, int limit, int vertical, bool allowNull = false) {
		int target = (cursorPos.x + cursorPos.y * limit);
		if (vertical > 0) {
			target = (cursorPos.y + cursorPos.x * vertical);
		}
		if (data[target] != null || allowNull) {
			selectAction(target);
		}
	}

	// ------------------------------------------
	// General Menu Functions
	public void updateDPad() {
		dpadPressed = (0, 0);
		if (Global.input.isPressedMenu("up")) { dpadPressed.y--; }
		if (Global.input.isPressedMenu("down")) { dpadPressed.y++; }
		if (Global.input.isPressedMenu("left")) { dpadPressed.x--; }
		if (Global.input.isPressedMenu("right")) { dpadPressed.x++; }
	}

	public void drawMenuSprite(
		(Texture texture, int height, int size) spriteSheet,
		int spriteNum, float posX, float posY, float alpha = 1
	) {
		DrawWrappers.DrawTextureHUD(
			spriteSheet.texture,
			4 + (spriteSheet.size + 4) * (spriteNum / spriteSheet.height),
			4 + (spriteSheet.size + 4) * (spriteNum % spriteSheet.height),
			spriteSheet.size, spriteSheet.size,
			posX, posY, alpha
		);
	}
}