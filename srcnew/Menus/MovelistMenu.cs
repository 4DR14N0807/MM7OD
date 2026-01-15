using System;
using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;

namespace MMXOnline;


public class MovelistMenuHandler {

	public static IMainMenu getCharMovelistMenu(IMainMenu prevMenu, bool inGame, int charNum, bool hasHypermode, bool hasShield) {
		if (charNum == (int)CharIds.Rock) {
			return new RockMovelistMenu(prevMenu, inGame, hasHypermode);
		} else if (charNum == (int)CharIds.Blues) {
			return new BluesMovelistMenu(prevMenu, inGame, Options.main.bluesLoadout.specialWeapon, hasShield, hasHypermode);
		} else if (charNum == (int)CharIds.Bass) {
			return new BassMovelistMenu(prevMenu, inGame, hasHypermode);
		}
		return new RockMovelistMenu(prevMenu, false, false);
	}
}

public class MovelistItem {

	public string spriteName = "";
	public int frameIndex;
	public string moveName = "";
	public string moveInput = "";
	public string? moveRequirement = null;
	public float time;

	public MovelistItem(
		string spriteName, int frameIndex, string moveName, 
		string moveInput, string? moveRequirement = null
	) {
		this.spriteName = spriteName;
		this.frameIndex = frameIndex;
		this.moveName = moveName;
		this.moveInput = controlText(moveInput);
		if (moveRequirement != null) this.moveRequirement = controlText(moveRequirement);
	}

	public virtual void update() {
		time += Global.speedMul;
	}
	public virtual void reset() {
		time = 0;
	}
	public virtual void render(Point renderPos) {
		Global.sprites[spriteName].drawToHUD(frameIndex, renderPos.x, renderPos.y);

		if (string.IsNullOrEmpty(moveName) && string.IsNullOrEmpty(moveInput)) return;

		float renderX = renderPos.x + 40;
		float renderY1 = renderPos.y - 32;
		float renderY2 = renderY1 + 9;
		float renderY3 = renderY2 + 9;
		string input = "INPUT: " + moveInput;
		
		Point size  = getSquareSize(new() { moveName, input, moveRequirement ?? ""});
		DrawWrappers.DrawRect(
			renderX - 1, renderY1, renderX - 1 + size.x, renderY1 + size.y,
			true, new Color(0, 0, 0, 100), 1, ZIndex.HUD, false, outlineColor: Color.White
		);

		Fonts.drawText(FontType.WhiteSmall, moveName, renderX, renderY1);
		Fonts.drawText(FontType.BlueSmall, input, renderX, renderY2);
		if (moveRequirement != null) Fonts.drawText(FontType.RedSmall, moveRequirement, renderX, renderY3);	
	}

	Point getSquareSize(List<string> descriptions) {
		string longestString = "";
		foreach (string str in descriptions) {
			float currentLength = Fonts.measureText(Fonts.getFontSrt(FontType.WhiteSmall), longestString);
			float nextLength = Fonts.measureText(Fonts.getFontSrt(FontType.WhiteSmall), str);

			if (currentLength < nextLength) longestString = str;
		}

		float x = Fonts.measureText(Fonts.getFontSrt(FontType.WhiteSmall), longestString) + 2;

		int lines = 2;
		int reqLines = 0;
		int extra = 0;
		if (moveRequirement != null) {
			reqLines += moveRequirement.Split("\n").Length;
			extra = 1;
		} 
		float y = (lines * 9) + (reqLines * 10) - extra;

		return new Point(x,y);
	}

	string controlText(string text, bool isController = false) {
		if (isController) isController = Control.isJoystick();
		// Menu keys.
		text = text.Replace(Control.MenuConfirm, Control.getKeyOrButtonName(Control.MenuConfirm, isController));
		text = text.Replace(Control.MenuAlt, Control.getKeyOrButtonName(Control.MenuAlt, isController));
		text = text.Replace(Control.MenuBack, Control.getKeyOrButtonName(Control.MenuBack, isController));
		text = text.Replace(Control.Scoreboard, Control.getKeyOrButtonName(Control.Scoreboard, isController));
		text = text.Replace(Control.MenuPause, Control.getKeyOrButtonName(Control.MenuPause, isController));
		text = text.Replace(Control.MenuLeft, Control.getKeyOrButtonName(Control.MenuLeft, isController));
		text = text.Replace(Control.MenuRight, Control.getKeyOrButtonName(Control.MenuRight, isController));
		text = text.Replace(Control.MenuUp, Control.getKeyOrButtonName(Control.MenuUp, isController));
		text = text.Replace(Control.MenuDown, Control.getKeyOrButtonName(Control.MenuDown, isController));
		// Normal keys.
		text = text.Replace(Control.Jump, Control.getKeyOrButtonName(Control.Jump, isController));
		text = text.Replace(Control.Shoot, Control.getKeyOrButtonName(Control.Shoot, isController));
		text = text.Replace(Control.Special1, Control.getKeyOrButtonName(Control.Special1, isController));
		text = text.Replace(Control.Dash, Control.getKeyOrButtonName(Control.Dash, isController));
		text = text.Replace(Control.WeaponLeft, Control.getKeyOrButtonName(Control.WeaponLeft, isController));
		text = text.Replace(Control.WeaponRight, Control.getKeyOrButtonName(Control.WeaponRight, isController));
		text = text.Replace(Control.Special2, Control.getKeyOrButtonName(Control.Special2, isController));

		return text;
	}
}


public class MovelistItemAnimated : MovelistItem {

	List<int> frames = new();
	int frameCount;
	int frame;

	public MovelistItemAnimated(
		string spriteName, int[] frames, string moveName,
		string moveInput, string? moveRequirement = null
	) : base(
		spriteName, frames[0], moveName, moveInput, moveRequirement
	) {
		this.frames = frames.ToList();
		frameCount = this.frames.Count;
	}

	public override void update() {
		base.update();
		if (time >= 45) {
			frame++;
			frameIndex = frames[frame % frameCount];
			time = 0;
		}
	}

	public override void reset() {
		base.reset();
		frame = 0;
		frameIndex = frames[frame];
	}
}

public class MovelistMenu : IMainMenu {

	IMainMenu prevMenu;
	bool inGame;
	public string title;
	public List<MovelistItem> moves = new();
	public int scrollIndex;
	int oldScrollIndex;
	int itemsPerIndex = 2;
	int minIndex => scrollIndex * itemsPerIndex * 2;
	int maxIndex => Math.Min(moves.Count - 1, minIndex + 4);

	public MovelistMenu(IMainMenu prevMenu, bool inGame, string title) {
		this.prevMenu = prevMenu;
		this.inGame = inGame;
		this.title = title;
	}

	int getMaxScroll() {
		return MathInt.Ceiling(moves.Count / ((decimal)itemsPerIndex * 2)) - 1;
	}

	public virtual void update() {
		for (int i = minIndex; i < maxIndex; i++) {
			moves[i].update();
		}

		Helpers.menuLeftRight(ref scrollIndex, 0, getMaxScroll(), true, true);
		if (scrollIndex != oldScrollIndex) {
			oldScrollIndex = scrollIndex;
			for (int i = minIndex; i < maxIndex; i++) {
				moves[i].reset();
			}
		}

		if (Global.input.isPressedMenu(Control.MenuBack)) {
			Menu.change(prevMenu);
		}
	}

	public virtual void render() {
		if (!inGame) {
			DrawWrappers.DrawTextureHUD(Global.textures["severbrowser"], 0, 0);
		} else {
			DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
		}

		if (moves.Count <= 0) {
			Fonts.drawText(
				inGame ? FontType.BlueMenu : FontType.OrangeMenu, title, Global.halfScreenW, 20, Alignment.Center
			);
			return;
		} 

		int index = minIndex;
		for (int i = 0; i < 2; i++) {
			for (int j = 0; j < itemsPerIndex; j++) {
				
				if (index >= moves.Count) break;
				Point renderPos = new Point(58 + (i * 175), 80 + (j * 91));
				moves[index].render(renderPos);

				index++;
			}
		}

		Fonts.drawText(
			inGame ? FontType.BlueMenu : FontType.OrangeMenu, title, Global.halfScreenW, 20, Alignment.Center
		);
	
		float arrowPosY = Global.halfScreenH - 9;
		if (Global.floorFrameCount % 60 < 30 && getMaxScroll() > 0) {
			Fonts.drawText(
				FontType.BlueMenu, ">", 360, arrowPosY, Alignment.Center
			);
			Fonts.drawText(
				FontType.BlueMenu, "<", 24, arrowPosY, Alignment.Center
			);
		}

		Fonts.drawTextEX(
			FontType.Blue, "[BACK]: Back, [MLEFT]/[MRIGHT]: Scroll",
			Global.screenW / 2, 188, Alignment.Center
		);

		Fonts.drawText(
			FontType.Blue, (scrollIndex + 1).ToString() + "/" + (getMaxScroll() + 1).ToString(),
			Global.screenW - 21, 188, Alignment.Right
		);
	}
}
