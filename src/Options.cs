﻿using System;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace MMXOnline;

public class Options {
	public string playerName;
	public float musicVolume = 1;
	public float soundVolume = 1;
	public bool showWeaponHUD = true;
	public int? regionIndex;
	public bool logTelemetry = true;
	public bool showFPS = false;
	//public bool showInGameMenuHUD = false;
	public bool showSysReqPrompt = true;
	public bool enableDeveloperConsole;
	public bool disableChat;
	public int maxFPS = 60;
	public bool cheatWarningShown;
	public bool disableDoubleDash;
	public int preferredCharacter = 10;
	public bool showMashProgress;
	public bool killOnLoadoutChange = true;
	public bool killOnCharChange = true;
	public float networkTimeoutSeconds = 3;
	public bool autoCreateDocFolderPromptShown = false;
	public bool enableHyperMusic = true;

	public int getNetworkTimeoutMs() {
		networkTimeoutSeconds = Helpers.clamp(networkTimeoutSeconds, 1, 5);
		return MathInt.Round(networkTimeoutSeconds * 1000);
	}

	// Video settings
	public bool fullScreen = false;
	public bool integerFullscreen = true;
	public uint windowScale = 2;
	public bool fastShaders;
	public bool multithreadMode;
	public bool vsync;
	public bool drawMiniMap = true;
	public int textQuality = 0;
	public int fontType = 0; // 0 = bitmap only, 1 = bitmap + vector, 2 = vector only
	public int? detectedGraphicsPreset = null;
	public bool enablePostProcessing = true;
	public int particleQuality = 2;
	public bool enableMapSprites = true;
	public bool enableLowEndMap;
	public bool enableSmallBars;
	public bool smallBarsEx;
	public bool oldNavPoints;

	public bool lowQualityParticles() { return particleQuality == 0; }

	public bool shouldUseOptimizedAssets() {
		return false; //Global.useOptimizedAssetsOverride ?? useOptimizedAssets;
	}

	// X
	public bool useRandomLoadout;
	public int gridModeX;
	public int hyperChargeSlot;
	public bool novaStrikeSpecial;
	public bool gigaCrushSpecial;
	public XLoadout xLoadout = XLoadout.getDefault();

	// Zero
	public bool swapAirAttacks;
	public bool showGigaAttackCooldown;
	public ZeroLoadout zeroLoadout = new ZeroLoadout();

	// Vile
	public int weaponOrderingVile;
	public bool swapGoliathInputs;
	public bool blockMechSlotScroll;
	public bool mk5PuppeteerHoldOrToggle;
	public bool lockInAirCannon = true;
	public VileLoadout vileLoadout = new VileLoadout();

	// Axl
	public int gridModeAxl;
	public bool aimAnalog = false;
	public float aimSensitivity = 0.5f;
	public int axlAimMode = 0;
	public bool lockOnSound = false;
	public bool backwardsAimInvert = false;
	public bool axlSeparateAimDownAndCrouch = false;
	public bool moveInDiagAim = true;
	public int aimKeyFunction = 1;  //0 = aim backwards, 1 = lock position, 2 = lock aim
	public bool aimKeyToggle = false;
	public bool showRollCooldown;
	public AxlLoadout axlLoadout = new AxlLoadout();

	// Sigma
	public int sigmaWeaponSlot;
	public bool puppeteerHoldOrToggle;
	public SigmaLoadout sigmaLoadout = new SigmaLoadout();
	public bool maverickStartFollow = true;
	public bool puppeteerCancel;

	// Rock
	public bool useRandomRockLoadout;
	public RockLoadout rockLoadout = new RockLoadout();
	public int gridModeRock;
	public bool rushSpecial = true;
	public bool downJumpSlide = true;
	public bool wheelDoubleTap = false;

	// ProtoMan
	public bool useRandomBluesLoadout;
	public BluesLoadout bluesLoadout = BluesLoadout.createDefault();
	public bool protoShieldHold;
	public int coreHeatDisplay;
	public bool altBluesSlideInput;
	public bool reverseBluesDashInput;

	// Bass
	public BassLoadout bassLoadout = BassLoadout.createDefault();
	public int gridModeBass;
	public bool useRandomBassLoadout;
	
	// Punchy Zero
	public PZeroLoadout pzeroLoadout = new PZeroLoadout();

	private static Options _main = null!;

	public static Options main {
		get {
			if (_main == null) {
				string text = Helpers.ReadFromFile("options.json");
				if (string.IsNullOrEmpty(text)) {
					_main = new Options();
				} else {
					try {
						_main = JsonConvert.DeserializeObject<Options>(text);
					} catch {
						throw new Exception("Your options.json file is corrupted, or does no longer work with this version. Please delete it and launch the game again.");
					}
				}

				_main.validate();

				if (Global.debug) {
					_main.axlAimMode = Global.overrideAimMode ?? _main.axlAimMode;
					_main.fullScreen = Global.overrideFullscreen ?? _main.fullScreen;
					_main.maxFPS = MathInt.Clamp(_main.maxFPS, 30, Global.fpsCap);
					_main.fontType = Global.fontTypeOverride ?? _main.fontType;
				}
			}

			return _main;
		}
	}

	public void validate() {
		if (playerName != null && playerName.Length > Global.maxPlayerNameLength) {
			playerName = playerName.Substring(0, Global.maxPlayerNameLength);
		}
		playerName = Helpers.censor(playerName);
		playerName = Regex.Replace(playerName, @"[^\u0000-\u007F]+", "?"); //Remove non ASCII chars to prevent possible issues

		hyperChargeSlot = Helpers.clamp(hyperChargeSlot, 0, 2);
		sigmaWeaponSlot = Helpers.clamp(sigmaWeaponSlot, 0, 2);
		preferredCharacter = Helpers.clamp(preferredCharacter, (int)CharIds.Rock, (int)CharIds.Bass);

		xLoadout.validate();
		zeroLoadout.validate();
		vileLoadout.validate();
		axlLoadout.validate();
		sigmaLoadout.validate();
		rockLoadout.validate();
		bluesLoadout.validate();
		bassLoadout.validate();
	}

	public static bool isValidLANIP(string LANIPPrefix) {
		if (!LANIPPrefix.EndsWith(".")) return false;
		string fullIP = LANIPPrefix + "1";
		return IPAddress.TryParse(fullIP, out _);
	}

	public string getSpecialAirAttack() {
		return !swapAirAttacks ? "zero_attack_air2" : "zero_attack_air";
	}

	public string getAirAttack() {
		if (Global.level?.mainPlayer?.isSigma == true) return "attack_air";
		return swapAirAttacks ? "attack_air2" : "attack_air";
	}

	public bool useMouseAim {
		get { return axlAimMode == 2; }
	}

	public void saveToFile() {
		string text = JsonConvert.SerializeObject(_main);
		Helpers.WriteToFile("options.json", text);
	}

	public Region getRegion() {
		if (Global.regions == null || Global.regions.Count == 0) return null;
		if (regionIndex == null || regionIndex.Value >= Global.regions.Count) {
			regionIndex = 0;
		}
		return Global.regions[regionIndex.Value];
	}

	public Region getRegionOrDefault() {
		if (Global.regions == null || Global.regions.Count == 0) return null;
		if (regionIndex == null) {
			return Global.regions.ElementAtOrDefault(0);
		}
		if (regionIndex.Value >= Global.regions.Count) {
			regionIndex = 0;
		}
		return Global.regions[regionIndex.Value];
	}

	public bool isDeveloperConsoleEnabled() {
		if (Global.debug) {
			return true;
		} else {
			return enableDeveloperConsole;
		}
	}
}
