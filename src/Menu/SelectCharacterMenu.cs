using System;
using ProtoBuf;
using SFML.Graphics;

namespace MMXOnline;

[ProtoContract]
public class PlayerCharData {
	[ProtoMember(1)] public int charNum = (int)CharIds.Rock;
	[ProtoMember(2)] public int armorSet = 0;
	[ProtoMember(3)] public int alliance = -1;
	[ProtoMember(4)] public bool isRandom;

	public bool xSelected { get { return charNum == 0; } }

	public int uiSelectedCharIndex;

	public PlayerCharData() {
		isRandom = true;
	}

	public PlayerCharData(int charNum) {
		this.charNum = charNum;
	}
}

public enum CharIds {

	//Adrian: When adding new chars, remember to add them in charHeartTanks and the code crash bomb,
	//and spawnCharatPoint().
	X,
	Zero,
	Vile,
	Axl,
	Sigma,
	Rock,
	Blues,
	Bass,
	PunchyZero,
	BusterZero,
	// Non-standard chars start here.
	WolfSigma = 100,
	ViralSigma,
	KaiserSigma,
	RagingChargeX,
	// Non-vanilla chars start here.
}

public class CharSelection {
	public string name;
	public int mappedCharNum;
	public int mappedCharArmor;
	public int mappedCharMaverick;
	public string sprite;
	public int frameIndex;
	public Point offset = new Point(0, 48);

	public static int sigmaIndex => Options.main?.sigmaLoadout?.sigmaForm ?? 0;

	public static CharSelection[] selections => [
		new CharSelection("Megaman", 5, 0, 0, "rock_idle", 0),
		new CharSelection("Protoman", 6, 0, 0, "blues_idle", 0 ),
		new CharSelection("Bass", 7, 0, 0, "bass_idle", 0),
	];

	public static CharSelection[] selections1v1 => [
		new CharSelection("Megaman", 5, 0, 0, "rock_idle", 0),
		new CharSelection("Protoman", 6, 0, 0, "blues_idle", 0 ),
		new CharSelection("Bass", 7, 0, 0, "bass_idle", 0),
	];

	public CharSelection(
		string name, int mappedCharNum, int mappedCharArmor,
		int mappedCharMaverick, string sprite, int frameIndex
	) {
		this.name = name;
		this.mappedCharNum = mappedCharNum;
		this.mappedCharArmor = mappedCharArmor;
		this.mappedCharMaverick = mappedCharMaverick;
		this.sprite = sprite;
		this.frameIndex = frameIndex;
	}
}

public class SelectCharacterMenu : IMainMenu {
	public IMainMenu prevMenu;
	public int selectArrowPosY;
	public const int startX = 30;
	public int startY = 46;
	public const int lineH = 10;
	public const uint fontSize = 24;

	public bool is1v1;
	public bool isOffline;
	public bool isInGame;
	public bool isInGameEndSelect;
	public bool isTeamMode;
	public bool isHost;
	private FontType fontOption = FontType.OrangeMenu;

	public Action completeAction;

	public static PlayerCharData playerData = new PlayerCharData(Options.main.preferredCharacter);

	public SelectCharacterMenu(PlayerCharData playerData) {
		SelectCharacterMenu.playerData = playerData;
	}

	public SelectCharacterMenu(int charNum) {
		SelectCharacterMenu.playerData = new PlayerCharData(charNum);
	}

	public CharSelection[] charSelections;

	public SelectCharacterMenu(
		IMainMenu prevMenu, bool is1v1, bool isOffline, bool isInGame,
		bool isInGameEndSelect, bool isTeamMode, bool isHost, Action completeAction
	) {
		this.prevMenu = prevMenu;
		this.is1v1 = is1v1;
		this.isOffline = isOffline;
		this.isInGame = isInGame;
		this.isInGameEndSelect = isInGameEndSelect;
		this.completeAction = completeAction;
		this.isTeamMode = isTeamMode;
		this.isHost = isHost;

		if (isInGame) fontOption = FontType.BlueMenu;

		charSelections = is1v1 ? CharSelection.selections1v1 : CharSelection.selections;
		playerData.charNum = isInGame ? Global.level.mainPlayer.newCharNum : Options.main.preferredCharacter;

		if (is1v1) {
			playerData.uiSelectedCharIndex = Array.FindIndex(
				charSelections, c => c.mappedCharNum == playerData.charNum && c.mappedCharArmor == playerData.armorSet
			);
		} else {
			playerData.uiSelectedCharIndex = Array.FindIndex(
				charSelections, c => c.mappedCharNum == playerData.charNum
			);
		}
	}

	public Player mainPlayer { get { return Global.level.mainPlayer; } }

	public void update() {
		if (Global.input.isPressedMenu(Control.MenuConfirm) || (Global.quickStartOnline && !isInGame)) {
			if (!isInGame && Global.quickStartOnline) {
				playerData.charNum = Global.quickStartOnlineClientCharNum;
			}
			if (isInGame && !isInGameEndSelect) {
				if (!Options.main.killOnCharChange && !Global.level.mainPlayer.isDead) {
					Global.level.gameMode.setHUDErrorMessage(mainPlayer, "Change will apply on next death", playSound: false);
					mainPlayer.delayedNewCharNum = playerData.charNum;
				} else if (mainPlayer.newCharNum != playerData.charNum) {
					mainPlayer.newCharNum = playerData.charNum;
					Global.serverClient?.rpc(RPC.switchCharacter, (byte)mainPlayer.id, (byte)playerData.charNum);
					mainPlayer.forceKill();
				}
			}

			completeAction.Invoke();
			return;
		}

		Helpers.menuLeftRightInc(
			ref playerData.uiSelectedCharIndex, 0, charSelections.Length - 1, true, playSound: true
		);
		try {
			playerData.charNum = charSelections[playerData.uiSelectedCharIndex].mappedCharNum;
			playerData.armorSet = charSelections[playerData.uiSelectedCharIndex].mappedCharArmor;
		} catch (IndexOutOfRangeException) {
			playerData.uiSelectedCharIndex = 0;
			playerData.charNum = charSelections[0].mappedCharNum;
			playerData.armorSet = charSelections[0].mappedCharArmor;
		}

		if (!isInGameEndSelect) {
			if (!isInGame) {
				if (Global.input.isPressedMenu(Control.MenuBack)) {
					Global.serverClient = null;
					Menu.change(prevMenu);
				}
			} else {
				if (Global.input.isPressedMenu(Control.MenuBack)) {
					Menu.change(prevMenu);
				}
			}
		} else {
			if (Global.input.isPressedMenu(Control.MenuBack)) {
				if (Global.isHost) {
					Menu.change(prevMenu);
				}
			} else if (Global.input.isPressedMenu(Control.MenuPause)) {
				if (!Global.isHost) {
					Menu.change(new ConfirmLeaveMenu(this, "Are you sure you want to leave?", () => {
						Global._quickStart = false;
						Global.leaveMatchSignal = new LeaveMatchSignal(LeaveMatchScenario.LeftManually, null, null);
					}));
				}
			}
		}
	}

	public void render() {
		if (!charSelections.InRange(playerData.uiSelectedCharIndex)) {
			playerData.uiSelectedCharIndex = 0;
		}
		CharSelection charSelection = charSelections[playerData.uiSelectedCharIndex];

		if (!isInGame) {
			DrawWrappers.DrawTextureHUD(Global.textures["severbrowser"], 0, 0);
		} else {
			DrawWrappers.DrawTextureHUD(Global.textures["pausemenu"], 0, 0);
		}

		// DrawWrappers.DrawTextureHUD(
		//	Global.textures["cursor"], startX - 10, menuOptions[(int)selectArrowPosY].pos.y - 1
		//);
		if (!isInGame) {
			Fonts.drawText(
				fontOption, "Select Character".ToUpper(),
				Global.halfScreenW, 20, alignment: Alignment.Center
			);
		} else {
			if (Global.level.gameMode.isOver) {
				DrawWrappers.DrawRect(
					Global.halfScreenW - 90, 18, Global.halfScreenW + 90, 33,
					true, new Color(0, 0, 0, 100), 1, ZIndex.HUD, false, outlineColor: Color.White
				);
				Fonts.drawText(
					fontOption, "Select Character".ToUpper(),
					Global.halfScreenW, 20, alignment: Alignment.Center
				);
			} else {
				DrawWrappers.DrawRect(
					Global.halfScreenW - 67, 18, Global.halfScreenW + 67, 33,
					true, new Color(0, 0, 0, 100), 1, ZIndex.HUD, false, outlineColor: Color.White
				);
				Fonts.drawText(
					fontOption, "Select Character".ToUpper(),
					Global.halfScreenW, 20, alignment: Alignment.Center
				);
			}
		}

		// Draw character + box
		var charPosX1 = Global.halfScreenW;
		var charPosY1 = 85;
		Global.sprites["playerbox"].drawToHUD(0, charPosX1, charPosY1+2);
		string sprite = charSelection.sprite;
		int frameIndex = charSelection.frameIndex;
		float yOff = Global.sprites[sprite].frames[0].offset.y;
		float xOff = Global.sprites[sprite].frames[0].offset.x;
		Global.sprites[sprite].drawToHUD(
			frameIndex,
			charPosX1 + xOff,
			charPosY1 + yOff + 25
		);

		// Draw text

		if (Global.frameCount % 60 < 30) {
			Fonts.drawText(
				FontType.Blue, "<", Global.halfScreenW - 60, Global.halfScreenH + 22,
				Alignment.Center
			);
			Fonts.drawText(
				FontType.Blue, ">", Global.halfScreenW + 60, Global.halfScreenH + 22,
				Alignment.Center
			);
		}
		Fonts.drawText(
			FontType.Blue, charSelection.name, Global.halfScreenW, Global.halfScreenH + 22,
			alignment: Alignment.Center
		);

		string[] description = playerData.charNum switch {
			(int)CharIds.Rock => new string[]{
				"A versatile character\nthat can do a variety of roles\nthanks to his Variable Weapon System.",
			},
			(int)CharIds.Blues =>  new string[]{
				"Mid range character who has\ngood attack and defense\nBut it's limited by it's unstable core."
			},
			(int)CharIds.Bass =>  new string[]{
				"(Unfinished)"
			},
			_ => new string[] { "ERROR" }
		};
		if (description.Length > 0) {
			DrawWrappers.DrawRect(
				25, startY + 98, Global.screenW - 25, startY + 137,
				true, new Color(0, 0, 0, 200), 1, ZIndex.HUD, false, outlineColor: new Color(255, 255, 255, 200)
			);
			for (int i = 0; i < description.Length; i++) {
				Fonts.drawText(
					FontType.WhiteSmall, description[i],
					Global.halfScreenW, startY + 93 + (10 * (i + 1)), alignment: Alignment.Center
				);
			}
		}
		
		
		/*
		if (!isInGame) {
			Fonts.drawTextEX(
				FontType.Grey, "[OK]: Continue, [BACK]: Back\n[MLEFT]/[MRIGHT]: Change character",
				Global.screenW * 0.5f, 182, Alignment.Center
			);
		} else {
			if (!Global.isHost) {
				Fonts.drawTextEX(
					FontType.Grey, "[ESC]: Quit\n[MLEFT]/[MRIGHT]: Change character",
					Global.screenW * 0.5f, 190, Alignment.Center
				);
			} else {
				Fonts.drawTextEX(
					FontType.Grey, "[OK]: Continue, [BACK]: Back\n[MLEFT]/[MRIGHT]: Change character",
					Global.screenW * 0.5f, 190, Alignment.Center
				);
			}
		}
		*/
	}
}
