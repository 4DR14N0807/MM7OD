using System;
using System.Collections.Generic;
using System.Linq;
using SFML.Graphics;
using SFML.System;

namespace MMXOnline;

public class BatchDrawable : Transformable, IDrawable {
	public VertexArray vertices;
	public Texture texture;

	public BatchDrawable(Texture texture) {
		vertices = new VertexArray();
		vertices.PrimitiveType = PrimitiveType.Triangles;
		this.texture = texture;
	}

	public void Draw(IRenderTarget target, RenderStates states) {
		states.Transform *= Transform;
		states.Texture = texture;
		target.Draw(vertices, states);
	}
}

// Draw wrappers and font code.
public partial class DrawWrappers {
	public static View hudView = null!;
	public static RenderTexture renderTexture = null!;
	public static List<Action> deferredTextDraws = new List<Action>();

	public static void initHUD() {
		hudView = new View(
			new Vector2f(Global.halfScreenW, Global.halfScreenH),
			new Vector2f(Global.screenW, Global.screenH)
		);
	}
	public static void drawToHUD(IDrawable drawable) {
		renderTexture.Draw(drawable);
		renderTexture.Display();
	}

	public static void drawToHUD(Vertex[] vertices, PrimitiveType type) {
		renderTexture.Draw(vertices, type);
		renderTexture.Display();
	}

	public static void DrawTextureHUD(
		Texture texture, float sx, float sy, float sw, float sh, float dx, float dy, float alpha = 1
	) {
		if (texture == null) return;
		var sprite = new SFML.Graphics.Sprite(texture, new IntRect(((int)sx, (int)sy), ((int)sw, (int)sh)));
		sprite.Position = new Vector2f(dx, dy);
		sprite.Color = new Color(255, 255, 255, (byte)(int)(alpha * 255));
		drawToHUD(sprite);
	}

	public static void DrawTextureHUD(Texture texture, float x, float y) {
		if (texture == null) return;
		var sprite = new SFML.Graphics.Sprite(texture);
		sprite.Position = new Vector2f(x, y);
		drawToHUD(sprite);
	}

	public static void addToVertexArray(BatchDrawable bd, SFML.Graphics.Sprite sprite) {
		float sx = sprite.TextureRect.Left;
		float sy = sprite.TextureRect.Top;
		float sw = sprite.TextureRect.Width;
		float sh = sprite.TextureRect.Height;
		float dx = sprite.Position.X;
		float dy = sprite.Position.Y;
		float scale = sprite.Scale.X;
		Color color = sprite.Color;

		float width = sw * scale;
		float height = sh * scale;

		Vertex vertexTL = new Vertex(new Vector2f(dx, dy), color);
		Vertex vertexBL = new Vertex(new Vector2f(dx, dy + height), color);
		Vertex vertexBR = new Vertex(new Vector2f(dx + width, dy + height), color);
		Vertex vertexTR = new Vertex(new Vector2f(dx + width, dy), color);

		vertexTL.TexCoords = new Vector2f(sx, sy);
		vertexBL.TexCoords = new Vector2f(sx, sy + sh);
		vertexBR.TexCoords = new Vector2f(sx + sw, sy + sh);
		vertexTR.TexCoords = new Vector2f(sx + sw, sy);
		// Top left.
		bd.vertices.Append(vertexTL);
		bd.vertices.Append(vertexBL);
		bd.vertices.Append(vertexBR);
		// Bottom rigth.
		bd.vertices.Append(vertexTR);
		bd.vertices.Append(vertexTL);
		bd.vertices.Append(vertexBR);
	}
}


public class HUD {
	public GameMode gameMode;
	public Level level;

	public HUD(Level level) {
		this.level = level;
		gameMode = new(level, 0);
	}

	public void render() {
		if (level.mainPlayer == null) return;
		if (DevConsole.showConsole) {
			return;
		}

		Player? targetPlayer = Global.level.mainPlayer;
		if (Global.level.mainPlayer.isSpectator || Global.level.mainPlayer.altSpectator) {
			targetPlayer = level.specPlayer;
		}
		if (targetPlayer != null) {
			drawPlayer(targetPlayer);
		}
		drawRadar();
		gameMode.render();
	}

	public void drawPlayer(Player player) {
		if (Global.level.mainPlayer == player) {
			renderHealthAndWeapons();
		} else {
			renderHealthAndWeapon(player, GameMode.HUDHealthPosition.Left);
		}
		// Currency
		Point basePos = new(Global.screenW - 96, 27);
		if (level.levelData.isTraining()) {
			basePos = new Point(10, 106);
			if (Global.level.mainPlayer.lastCharacter is Blues) {
				basePos.y += 18;
			}
		}
		else if (!shouldDrawRadar()) {
			basePos.x += 48;
		}
		Fonts.drawText(
			FontType.WhiteSmall,
			"x", basePos.x + 9, basePos.y, Alignment.Left
		);
		Fonts.drawText(
			FontType.WhiteSmall,
			" " + player.currency.ToString(), basePos.x + 40, basePos.y, Alignment.Right
		);
		Global.sprites["hud_scrap"].drawToHUD(0, basePos.x, basePos.y);

		if (player.weapons?.Count > 1) {
			gameMode.drawWeaponSwitchHUD(player);
		}
	}

	
	public void renderHealthAndWeapons() {
		bool is1v1OrTraining = level.is1v1() || level.levelData.isTraining();
		if (!is1v1OrTraining) {
			renderHealthAndWeapon(level.mainPlayer, GameMode.HUDHealthPosition.Left);
		} else {
			renderHealthAndWeapon(level.mainPlayer, GameMode.HUDHealthPosition.Left);
			Player? rightPlayer = Global.level.players.FirstOrDefault(
				(Player player) => player.character != null && player != level.mainPlayer
			);
			if (rightPlayer != null) {
				renderHealthAndWeapon(rightPlayer, GameMode.HUDHealthPosition.Right);
			}
		}
	}

	public void renderHealthAndWeapon(Player? player, GameMode.HUDHealthPosition position) {
		if (player == null) return;
		if (level.is1v1() && player.deaths >= gameMode.playingTo) return;

		player.lastCharacter?.renderHUD(new Point(), position);
	}

	public bool shouldDrawRadar() {
		return Options.main.drawMiniMap && !level.levelData.isTraining() && !level.is1v1();
	}

	public void drawRadar() {
		if (!shouldDrawRadar()) {
			return;
		}
		float scaledW = 42;
		float scaledH = 24;

		float radarX = MathF.Floor(Global.screenW - 10 - scaledW);
		float radarY = MathF.Floor(10);

		if (Menu.inMenu) {
			DrawWrappers.DrawRectWH(
				radarX, radarY,
				scaledW, scaledH,
				true, Color.Black, 1,
				ZIndex.HUD, isWorldPos: false,
				outlineColor: Color.White
			);
			DrawWrappers.DrawRectWH(
				radarX - 1, radarY - 1,
				scaledW + 2, scaledH + 2,
				true, Color.Transparent, 1,
				ZIndex.HUD, isWorldPos: false,
				outlineColor: Color.Black
			);
			return;
		}
		// Radar starts here.
		float mapScale = 16;
		float offsetX = MathF.Round(Global.level.camCenterX / 16f) - 21;
		float offsetY = MathF.Round(Global.level.camCenterY / 16f) - 12;
		float camX = Global.level.camCenterX;
		float camY = Global.level.camCenterY;
		if (Global.level.mainPlayer.character != null) {
			if (MathF.Abs(Global.level.camCenterX - Global.level.mainPlayer.character.pos.x) < 16) {
				offsetX = MathF.Round(Global.level.mainPlayer.character.pos.x / 16f) - 21;
				camX = Global.level.mainPlayer.character.pos.x;
			}
		}
		List<(float x, float y, float r)> revealedSpots = new();
		revealedSpots.Add((camX, camY, 16 * 10));

		if (gameMode.isTeamMode) {
			Player[] allyPlayersAlive = level.players.Where(
				p => !p.isSpectator && p.deaths < gameMode.playingTo &&
				p.alliance == Global.level.mainPlayer.alliance
			).ToArray();
			foreach (Player player in allyPlayersAlive) {
				if (player.character == null) {
					continue;
				}
				revealedSpots.Add((
					player.character.pos.x,
					player.character.pos.y,
					16 * 6)
				);
			}
		}

		float scaledMapW = MathF.Round(Global.level.levelData.width / 16f);
		float scaledMapH = MathF.Round(Global.level.levelData.height / 16f);

		if (!Options.main.enableLowEndMap) {
			Global.radarRenderTexture.Clear(new Color(0, 0, 0, 0));
			Global.radarRenderTextureB.Clear();
			RenderStates states = new RenderStates(Global.radarRenderTexture.Texture);
			RenderStates statesB = new RenderStates(Global.radarRenderTextureB.Texture);
			RenderStates statesB2 = new RenderStates(Global.radarRenderTextureB.Texture);
			states.BlendMode = new BlendMode(
				BlendMode.Factor.SrcAlpha,
				BlendMode.Factor.OneMinusSrcAlpha, BlendMode.Equation.Add
			) {
				AlphaEquation = BlendMode.Equation.Max
			};
			statesB.BlendMode = new BlendMode(
				BlendMode.Factor.SrcAlpha, BlendMode.Factor.OneMinusSrcAlpha, BlendMode.Equation.Add
			) {
				AlphaEquation = BlendMode.Equation.Max
			};
			statesB2.BlendMode = new BlendMode(
				BlendMode.Factor.SrcAlpha, BlendMode.Factor.OneMinusSrcAlpha, BlendMode.Equation.Min
			);

			// The "fog of war" rect
			RectangleShape rect = new RectangleShape(new Vector2f(scaledW + 20, scaledH + 20));
			rect.Position = new Vector2f(0, 0);
			rect.FillColor = new Color(0, 0, 0, 128);
			Global.radarRenderTextureB.Draw(rect, statesB2);

			// The visible area circles
			foreach (var spot in revealedSpots) {
				float pxPos = MathF.Round(spot.x / mapScale) - offsetX;
				float pyPos = MathF.Round(spot.y / mapScale) - offsetY;
				float radius = spot.r / mapScale;
				CircleShape circle1 = new CircleShape(radius);
				circle1.FillColor = new Color(0, 0, 0, 0);
				circle1.Position = new Vector2f(pxPos - radius, pyPos - radius);
				Global.radarRenderTextureB.Draw(circle1, statesB2);
			}

			var sprite = new SFML.Graphics.Sprite(Global.radarRenderTextureB.Texture);
			Global.radarRenderTextureB.Display();
			Global.radarRenderTextureC.Clear(new Color(33, 33, 74));
			Global.radarRenderTextureC.Display();
			Global.radarRenderTextureC.Draw(sprite);
			var spriteBackground = new SFML.Graphics.Sprite(Global.radarRenderTextureC.Texture);

			HashSet<GameObject> terrainClose = new();
			int gridXStart = MathInt.Floor((camX - 700) / Global.level.cellWidth);
			int gridXEnd = MathInt.Ceiling((camX + 700) / Global.level.cellWidth);
			int gridYStart = MathInt.Floor((camY - 400) / Global.level.cellWidth);
			int gridYEnd = MathInt.Ceiling((camY + 400) / Global.level.cellWidth);

			gridXStart = Helpers.clamp(gridXStart, 0, Global.level.terrainGrid.GetLength(0) - 1);
			gridXEnd = Helpers.clamp(gridXEnd, 0, Global.level.terrainGrid.GetLength(0) - 1);
			gridYStart = Helpers.clamp(gridYStart, 0, Global.level.terrainGrid.GetLength(1) - 1);
			gridYEnd = Helpers.clamp(gridYEnd, 0, Global.level.terrainGrid.GetLength(1) - 1);

			for (int i = gridXStart; i <= gridXEnd; i++) {
				for (int j = gridYStart; j <= gridYEnd; j++) {
					lock (Global.level.terrainGrid[i, j]) {
						foreach (GameObject terrain in Global.level.terrainGrid[i, j]) {
							terrainClose.Add(terrain);
						}
					}
				}
			}

			foreach (GameObject gameObject in terrainClose) {
				if (gameObject.iDisabled || gameObject.iDestroyed) {
					continue;
				}
				if (gameObject is not Geometry geometry) {
					continue;
				}
				Color blockColor = new Color(128, 128, 255);
				if (gameObject is not Wall and not KillZone and not Ladder and not OneWay) {
					continue;
				}
				bool extend = false;
				if (gameObject is Wall) {
					extend = true;
				} else if (gameObject is KillZone) {
					extend = true;
					blockColor = new Color(255, 64, 64);
				} else if (gameObject is Ladder) {
					blockColor = new Color(255, 200, 0);
				}
				Shape shape = geometry.collider.shape;
				float pxPos = shape.minX / mapScale;
				float pyPos = shape.minY / mapScale + 1;
				float mxPos = shape.maxX / mapScale - pxPos;
				float myPos = shape.maxY / mapScale - pyPos + 1;

				if (mxPos <= 1) {
					mxPos = 1;
				}
				if (mxPos <= 1) {
					mxPos = 1;
				}
				if (pxPos <= 0) {
					pxPos -= 20;
					mxPos += 20;
				}
				if (pyPos <= 1) {
					pyPos -= 20;
					myPos += 20;
				}
				if (extend) {
					if (pxPos + mxPos >= scaledMapW) {
						mxPos = 1000;
					}
					if (pyPos + myPos >= scaledMapH) {
						myPos = 1000;
					}
					if (pyPos + myPos >= scaledMapH) {
						myPos = 1000;
					}
				}
				pxPos -= offsetX;
				pyPos -= offsetY;

				RectangleShape wRect = new RectangleShape();
				wRect.FillColor = blockColor;
				wRect.Position = new Vector2f(pxPos, pyPos);
				wRect.Size = new Vector2f(mxPos, myPos);
				Global.radarRenderTexture.Draw(wRect);
			}
			Global.radarRenderTexture.Display();
			var sprite2 = new SFML.Graphics.Sprite(Global.radarRenderTexture.Texture);

			Global.radarRenderTextureB.Clear(new Color(0, 0, 0, 0));
			RenderStates statesL = new RenderStates(Global.radarRenderTextureB.Texture);
			ShaderWrapper? outlineShader = Helpers.cloneShaderSafe("map_outline");
			if (outlineShader != null) {
				outlineShader.SetUniform("textureSize", new SFML.Graphics.Glsl.Vec2(42, 26));
				statesL.Shader = outlineShader.getShader();
			}
			Global.radarRenderTextureB.Draw(sprite2, statesL);
			Global.radarRenderTextureB.Display();
			var spriteFG = new SFML.Graphics.Sprite(Global.radarRenderTextureB.Texture);

			Global.radarRenderTexture.Clear();
			Global.radarRenderTexture.Draw(spriteBackground);
			Global.radarRenderTexture.Draw(spriteFG);
			var spriteFinal = new SFML.Graphics.Sprite(Global.radarRenderTexture.Texture);
			spriteFinal.Position = new Vector2f(radarX, radarY);

			DrawWrappers.renderTexture.Draw(spriteFinal);
			sprite.Dispose();
			sprite2.Dispose();
			spriteFG.Dispose();
			spriteBackground.Dispose();
			spriteFinal.Dispose();
		} else {
			DrawWrappers.DrawRectWH(
				radarX, radarY,
				scaledW, scaledH,
				true, new Color(33, 33, 74), 0,
				ZIndex.HUD, isWorldPos: false
			);
		}

		// Nav points.
		foreach (var navPoint in gameMode.navPoints) {
			if (!Global.sprites.ContainsKey(navPoint.sprite)) {
				continue;
			}
			Color color = new Color(255, 255, 255);
			Color outColor = new Color(255, 255, 255, 128);
			float xPos = MathF.Round(navPoint.pos.x / mapScale) - offsetX;
			float yPos = MathF.Round(navPoint.pos.y / mapScale) - offsetY;

			Line line = new Line(new Point(scaledW / 2f, scaledH / 2f), new Point(xPos, yPos));
			Rect camRect = new Rect(0, 0, scaledW - 1, scaledH);
			List<CollideData> intersectionPoints = camRect.getShape().getLineIntersectCollisions(line);
			if (intersectionPoints.Count > 0 && intersectionPoints[0].hitData?.hitPoint != null) {
				Point intersectPoint = intersectionPoints[0].hitData.hitPoint.GetValueOrDefault();
				xPos = intersectPoint.x;
				yPos = intersectPoint.y;
			}
			float dxPos = radarX + MathF.Round(xPos);
			float dyPos = radarY + MathF.Round(yPos);

			Global.sprites[navPoint.sprite].drawToHUD(
				navPoint.index, dxPos, dyPos, 1, true
			);
		}

		// Players.
		foreach (var player in level.nonSpecPlayers()) {
			if (player.character == null || player.character.destroyed) continue;
			if (player.isMainPlayer && player.isDead) continue;

			float xPos = player.character.pos.x / mapScale;
			float yPos = player.character.pos.y / mapScale;
			float xPosF = player.character.pos.x;
			float yPosF = player.character.pos.y;

			Color color;
			if (!gameMode.isTeamMode) {
				if (player.isMainPlayer) {
					color = Color.Green;
				} else if (player.alliance == level.mainPlayer.alliance) {
					color = Color.Yellow;
				} else {
					color = Color.Red;
				}
			} else {
				color = (player.alliance) switch {
					0 => new Color(0, 255, 255), // Blue
					1 => new Color(255, 64, 64), // Red
					2 => new Color(128, 255, 128), // Green
					3 => new Color(160, 128, 255), // Purple 
					4 => new Color(255, 255, 128), // Yellow
					5 => new Color(255, 128, 128), // Orange.
					_ => Color.White,
				};
			}

			foreach (var spot in revealedSpots) {
				if (player.isMainPlayer || player.alliance == Global.level.mainPlayer.alliance ||
					new Point(xPosF, yPosF).distanceTo(new Point(spot.x, spot.y)) < spot.r
				) {
					float dxPos = radarX + MathF.Round(xPos) - offsetX;
					float dyPos = radarY + MathF.Round(yPos) - 1 - offsetY;
					if (dxPos < radarX || dxPos > radarX + scaledW ||
						dyPos < radarY || dyPos > radarY + scaledH
					) {
						continue;
					}
					DrawWrappers.DrawRectWH(
						dxPos, dyPos,
						1, 2,
						true, color, 0,
						ZIndex.HUD, isWorldPos: false
					);
					break;
				}
			}
		}

		// Radar rectangle itself (with border)
		DrawWrappers.DrawRectWH(
			radarX, radarY,
			scaledW, scaledH,
			true, Color.Transparent, 1,
			ZIndex.HUD, isWorldPos: false,
			outlineColor: Color.White
		);
		DrawWrappers.DrawRectWH(
			radarX - 1, radarY - 1,
			scaledW + 2, scaledH + 2,
			true, Color.Transparent, 1,
			ZIndex.HUD, isWorldPos: false,
			outlineColor: Color.Black
		);
	}

}