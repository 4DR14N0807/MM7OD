﻿using System;
using System.Collections.Generic;
using System.IO;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace MMXOnline;

public partial class Global {
	public static RenderWindow window = null!;
	public static bool fullscreen;

	public static Dictionary<int, (RenderTexture, RenderTexture)> renderTextures = new();
	public static RenderTexture screenRenderTexture = null!;
	public static RenderTexture srtBuffer1 = null!;
	public static RenderTexture srtBuffer2 = null!;
	public static RenderTexture radarRenderTexture = null!;
	public static RenderTexture radarRenderTextureB = null!;
	public static RenderTexture radarRenderTextureC = null!;

	// Normal (small) camera
	public static RenderTexture screenRenderTextureS = null!;
	public static RenderTexture srtBuffer1S = null!;
	public static RenderTexture srtBuffer2S = null!;

	// Large camera
	public static RenderTexture screenRenderTextureL = null!;
	public static RenderTexture srtBuffer1L = null!;
	public static RenderTexture srtBuffer2L = null!;

	public static View view = null!;
	public static View backgroundView = null!;

	public static uint screenW = 384;
	public static uint screenH = 216;

	public static uint viewScreenW { get { return (uint)MathF.Ceiling(screenW * viewSize); } }
	public static uint viewScreenH { get { return (uint)MathF.Ceiling(screenH * viewSize); } }

	public static uint halfViewScreenW { get { return viewScreenW / 2; } }
	public static uint halfViewScreenH { get { return viewScreenH / 2; } }

	public static uint halfScreenW = screenW / 2;
	public static uint halfScreenH = screenH / 2;

	public static uint windowW;
	public static uint windowH;

	public static float viewSize = 1;
	internal static List<(uint width, uint height)> renderTextureQueue = new();
	internal static HashSet<int> renderTextureQueueKeys = new();

	public static void changeWindowSize(uint windowScale) {
		windowW = screenW * windowScale;
		windowH = screenH * windowScale;
		if (window != null) {
			window.Size = new Vector2u(windowW, windowH);
		}
	}

	public static void initMainWindow(Options options) {
		fullscreen = options.fullScreen;

		changeWindowSize(options.windowScale);

		screenRenderTextureS = new RenderTexture(screenW, screenH);
		srtBuffer1S = new RenderTexture(screenW, screenH);
		srtBuffer2S = new RenderTexture(screenW, screenH);

		screenRenderTextureL = new RenderTexture(screenW * 2, screenH * 2);
		srtBuffer1L = new RenderTexture(screenW * 2, screenH * 2);
		srtBuffer2L = new RenderTexture(screenW * 2, screenH * 2);

		var viewPort = new FloatRect(0, 0, 1, 1);

		if (!fullscreen) {
			window = new RenderWindow(new VideoMode(windowW, windowH), "MM7 Online: Deathmatch");
			window.SetVerticalSyncEnabled(options.vsync);
			if (Global.hideMouse) window.SetMouseCursorVisible(false);
		} else {
			uint desktopWidth = VideoMode.DesktopMode.Width;
			uint desktopHeight = VideoMode.DesktopMode.Height;
			window = new RenderWindow(new VideoMode(desktopWidth, desktopHeight), "MM7 Online: Deathmatch", Styles.None);
			window.SetVerticalSyncEnabled(options.vsync);
			window.Position = new Vector2i(0, 0);
			window.Size = new Vector2u(desktopWidth, desktopHeight);
			viewPort = getFullScreenViewPort();
			#if WINDOWS
				IntPtr handle = window.SystemHandle;
				const int GWL_STYLE = -16;
				const UInt32 WS_POPUP = 0x80000000;
				UInt32 currentStyle = Program.GetWindowLong(handle, GWL_STYLE);
				Program.SetWindowLong(handle, GWL_STYLE, currentStyle & ~(WS_POPUP));
			#endif
		}

		if (!File.Exists(Global.assetPath + "assets/menu/icon.png")) {
			throw new Exception("Error loading icon asset file, posible missing assets.");
		}

		var image = new Image(Global.assetPath + "assets/menu/icon.png");
		window.SetIcon(image.Size.X, image.Size.Y, image.Pixels);

		view = new View(new Vector2f(0, 0), new Vector2f(screenW, screenH));
		view.Viewport = viewPort;

		DrawWrappers.initHUD();
		DrawWrappers.hudView.Viewport = viewPort;

		window.SetView(view);
		/* This is unneeded as frameskip exists.
		if (Global.overrideFPS != null) {
			window.SetFramerateLimit((uint)Global.overrideFPS);
		} else {
			window.SetFramerateLimit((uint)options.maxFPS);
		}
		*/
		window.SetActive();
	}

	public static FloatRect getFullScreenViewPort() {
		float desktopWidth = VideoMode.DesktopMode.Width;
		float desktopHeight = VideoMode.DesktopMode.Height;
		float heightMultiple = VideoMode.DesktopMode.Height / (float)screenH;

		if (Options.main.integerFullscreen) {
			heightMultiple = MathF.Floor(VideoMode.DesktopMode.Height / (float)screenH);
		}
		float extraWidthPercent = (desktopWidth - screenW * heightMultiple) / desktopWidth;
		float extraHeightPercent = (desktopHeight - screenH * heightMultiple) / desktopHeight;

		return new FloatRect(extraWidthPercent / 2f, extraHeightPercent / 2f, 1f - extraWidthPercent, 1f - extraHeightPercent);
	}

	public static float getDebugFontScale() {
		if (fullscreen) {
			return (float)screenH / getFullScreenViewPort().Height;
		}
		if (window != null) {
			float xSize = (float)screenH / window.Size.Y;
			float ySize = (float)screenW / window.Size.X;

			if (xSize > ySize) {
				return xSize;
			}
			return ySize;
		}
		return 1;
	}
}
