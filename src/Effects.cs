using System;
using System.Collections.Generic;

namespace MMXOnline;

public class ChargeParticle : Actor {
	public float time;
	public ChargeParticle(
		Point pos, float time, ushort? netId
	) : base(
		"charge_part_1", new Point(pos.x, pos.y), netId, true, true
	) {
		this.time = time;
	}

	public override void update() {
		base.update();
	}

	public override void render(float x, float y) {
	}
}

public class ChargeEffect {
	public List<Point> origPoints;
	public List<ChargeParticle> chargeParts;
	public bool active = false;
	public Character character;
	string? chargePart;

	public ChargeEffect() {
		chargeParts = new List<ChargeParticle>();
		
		var point1 = new Point(Helpers.randomRange(-16, 16), Helpers.randomRange(0, 16)); //angle += 45;
		var point2 = new Point(Helpers.randomRange(-16, 16), Helpers.randomRange(0, 16)); //angle += 45;
		var point3 = new Point(Helpers.randomRange(-16, 16), Helpers.randomRange(0, 16)); //angle += 45;
		var point4 = new Point(Helpers.randomRange(-16, 16), Helpers.randomRange(0, 16)); //angle += 45;
		var point5 = new Point(Helpers.randomRange(-16, 16), Helpers.randomRange(0, 16)); //angle += 45;
		var point6 = new Point(Helpers.randomRange(-16, 16), Helpers.randomRange(0, 16)); //angle += 45;
		var point7 = new Point(Helpers.randomRange(-16, 16), Helpers.randomRange(0, 16)); //angle += 45;
		var point8 = new Point(Helpers.randomRange(-16, 16), Helpers.randomRange(0, 16)); //angle += 45;
		var point9 = new Point(Helpers.randomRange(-16, 16), Helpers.randomRange(0, 16)); //angle += 45;
		var point10 = new Point(Helpers.randomRange(-16, 16), Helpers.randomRange(0, 16)); //angle += 45;
		var point11 = new Point(Helpers.randomRange(-16, 16), Helpers.randomRange(0, 16)); //angle += 45;
		var point12 = new Point(Helpers.randomRange(-16, 16), Helpers.randomRange(0, 16)); //angle += 45;


		origPoints = new List<Point>() {
			point1, point2, point3, point4, point5, point6, point7, point8
		};

		chargeParts = new List<ChargeParticle>() {
			new ChargeParticle(point1.clone(), 0, null),
			new ChargeParticle(point2.clone(), 3, null),
			new ChargeParticle(point3.clone(), 0, null),
			new ChargeParticle(point4.clone(), 1.5f, null),
			new ChargeParticle(point5.clone(), -1.5f, null),
			new ChargeParticle(point6.clone(), -3, null),
			new ChargeParticle(point7.clone(), -1.5f, null),
			new ChargeParticle(point8.clone(), -1.5f, null),
			new ChargeParticle(point9.clone(), 0, null),
			new ChargeParticle(point10.clone(), -1.5f, null),
			new ChargeParticle(point11.clone(), 0, null),
			new ChargeParticle(point12.clone(), -1f, null)

		};
	}

	public void stop() {
		active = false;
	}

	public void reset() {
		chargeParts[0].time = Helpers.randomRange(-3, 0);
		chargeParts[1].time = Helpers.randomRange(-3, 0);
		chargeParts[2].time = Helpers.randomRange(-3, 0);
		chargeParts[3].time = Helpers.randomRange(-3, 0);
		chargeParts[4].time = Helpers.randomRange(-3, 0);
		chargeParts[5].time = Helpers.randomRange(-3, 0);
		chargeParts[6].time = Helpers.randomRange(-3, 0);
		chargeParts[7].time = Helpers.randomRange(-3, 0);
		chargeParts[8].time = Helpers.randomRange(-3, 0);
		chargeParts[9].time = Helpers.randomRange(-3, 0);
		chargeParts[10].time = Helpers.randomRange(-3, 0);
		chargeParts[11].time = Helpers.randomRange(-3, 0);

	}

	public void update(float chargeLevel, int chargeType) {
		active = true;
		for (int i = 0; i < chargeParts.Count; i++) {
			var part = chargeParts[i];
			if (part.time > 0) {
				//part.pos.x = Helpers.moveTo(part.pos.x, 0, Global.spf * 70);
				//part.pos.y = Helpers.moveTo(part.pos.y, 0, Global.spf * 70);
				part.pos.y -= Global.speedMul * 4;
			}
			chargePart = chargeLevel switch {
				2 => "charge_part_2",
				3 => "noise_crush_charge_part",
				_ => "charge_part_1"
			};
			if (chargeType == 1) {
				chargePart = chargeLevel switch {
					2 => "noise_crush_charge_part",
					3 => "charge_part_2",
					>=4 => "gravity_hold_charge_part",
					_ => "charge_part_1"
				};
			} else if (chargeType == 2) {
				chargePart = "noise_crush_charge_part";
			}
			part.changeSprite(chargePart, true);
			part.time += Global.spf * 20;
			if (part.time > 3) {
				part.time = -3;
				part.pos.x = Helpers.randomRange(-16, 16);
				part.pos.y = Helpers.randomRange(0, 16);
			}
		}
	}

	public void render(Point centerPos) {
		for (var i = 0; i < chargeParts.Count; i++) {
			var part = chargeParts[i];
			if (!active) {
				part.sprite.visible = false;
			} else if (part.time > 0) {
				part.sprite.visible = true;

				float x = centerPos.x + part.pos.x;
				float y = centerPos.y + part.pos.y;
				float halfWidth = 10;

				var rect = new Rect(x - halfWidth, y - halfWidth, x + halfWidth, y + halfWidth);
				var camRect = new Rect(Global.level.camX, Global.level.camY, Global.level.camX + Global.viewScreenW, Global.level.camY + Global.viewScreenH);
				if (rect.overlaps(camRect)) {
					part.sprite.draw((int)Math.Round(part.time), x, y, 1, 1, null, 1, 1, 1, ZIndex.Foreground);
				}
			} else {
				part.sprite.visible = false;
			}
		}
	}

	public void destroy() {
		foreach (ChargeParticle chargePart in chargeParts) {
			chargePart.destroySelf();
		}
	}

}

public class DieParticleActor : Actor {
	public DieParticleActor(string spriteName, Point pos) : base(spriteName, pos, null, true, false) {
	}

	public override void render(float x, float y) {
	}
}

public class DieEffectParticles {
	public Point centerPos;
	public float time = 0;
	public float ang = 0;
	public float alpha = 1;
	public List<Actor> dieParts = new List<Actor>();
	public List<Actor> dieParts2 = new List<Actor>();
	public bool destroyed = false;

	public DieEffectParticles(Point centerPos, int charNum) {
		this.centerPos = centerPos;
		for (var i = ang; i < ang + 360; i += 30) {
			if (i % 90 != 0 && i != 0) {
				var x = this.centerPos.x + Helpers.cosd(i) * time * 51;
				var y = this.centerPos.y + Helpers.sind(i) * time * 51;
				var diePartSprite = charNum == 1 ? "rock_die_particles" : "rock_die_particles";
				var diePart = new DieParticleActor(diePartSprite, new Point(centerPos.x, centerPos.y));
				dieParts.Add(diePart);
			}
		}

		for (var i = ang; i < ang + 360; i += 45) {
				var x = this.centerPos.x + Helpers.cosd(i) * time * 84;
				var y = this.centerPos.y + Helpers.sind(i) * time * 84;
				var diePartSprite = charNum == 1 ? "rock_die_particles2" : "rock_die_particles2";
				var diePart = new DieParticleActor(diePartSprite, new Point(centerPos.x, centerPos.y));
				dieParts2.Add(diePart);
		}

	}

	public void render(float offsetX, float offsetY) {
		var counter = 0;
		var counter2 = 0;
		for (var i = ang; i < ang + 360; i += 30) {
			if (i % 90 != 0 && i != 0) {
				if (counter >= dieParts.Count) continue;
				var diePart = dieParts[counter];
				if (diePart == null) continue;

				var x = centerPos.x + Helpers.cosd(i) * time * 51;
				var y = centerPos.y + Helpers.sind(i) * time * 51;
				float halfWidth = 10;

				var rect = new Rect(x - halfWidth, y - halfWidth, x + halfWidth, y + halfWidth);
				var camRect = new Rect(Global.level.camX, Global.level.camY, Global.level.camX + Global.viewScreenW, Global.level.camY + Global.viewScreenH);
				if (rect.overlaps(camRect)) {
					int frameIndex = (int)MathF.Round(time * 20) % diePart.sprite.totalFrameNum;
					diePart.sprite.draw(frameIndex, x + offsetX, y + offsetY, 1, 1, null, alpha, 1, 1, ZIndex.Foreground);
				}
				counter++;
			}
		}

		for (var i = ang; i < ang + 360; i += 45) {
			if (counter2 >= dieParts2.Count) continue;
			var diePart = dieParts2[counter2];
			if (diePart == null) continue;

			var x = centerPos.x + Helpers.cosd(i) * time * 84;
			var y = centerPos.y + Helpers.sind(i) * time * 84;
			float halfWidth = 10;

			var rect = new Rect(x - halfWidth, y - halfWidth, x + halfWidth, y + halfWidth);
			var camRect = new Rect(Global.level.camX, Global.level.camY, Global.level.camX + Global.viewScreenW, Global.level.camY + Global.viewScreenH);
			if (rect.overlaps(camRect)) {
				int frameIndex = (int)MathF.Round(time * 20) % diePart.sprite.totalFrameNum;
				diePart.sprite.draw(frameIndex, x + offsetX, y + offsetY, 1, 1, null, alpha, 1, 1, ZIndex.Foreground);
			}
			counter2++;	
		}
	}

	public void update() {
		time += Global.spf;
		alpha = Helpers.clamp01(1 - time * 0.5f);
		//ang += Global.spf * 100;

		if (alpha <= 0) {
			destroy();
		}
	}

	public void destroy() {
		foreach (var diePart in dieParts) {
			diePart.destroySelf();
		}
		foreach (var diePart in dieParts2) {
			diePart.destroySelf();
		}
		destroyed = true;
	}
}

public class Effect {
	public Point pos;
	public float effectTime;
	public Effect(Point pos) {
		this.pos = pos;
		Global.level.addEffect(this);
	}

	public virtual void update() {
		effectTime += Global.spf;
	}

	public virtual void render(float offsetX, float offsetY) {

	}

	public virtual void destroySelf() {
		Global.level.effects.Remove(this);
	}

}

public class ExplodeDieEffect : Effect {
	public float timer = 3;
	public float spawnTime = 0;
	public int radius;
	public Anim? exploder;
	public bool isExploderVisible;
	public bool destroyed;
	public Player? owner;
	public bool silent;
	public Actor? host;
	public bool doExplosion = true;

	public ExplodeDieEffect(Player owner, Point centerPos, Point animPos, string spriteName, int xDir,
		long zIndex, bool isExploderVisible, int radius, float maxTime,
		bool isMaverick, bool doExplosion = true) : base(centerPos)
	{
		this.owner = owner;
		this.radius = radius;
		this.doExplosion = doExplosion;
		timer = maxTime;
		if (!owner.ownedByLocalPlayer) return;

		exploder = new Anim(animPos, spriteName, xDir, owner.getNextActorNetId(), false, sendRpc: true);
		exploder.zIndex = zIndex;
		exploder.sprite.frameIndex = exploder.sprite.totalFrameNum - 1;
		exploder.visible = isExploderVisible;
		exploder.maverickFade = isMaverick;
	}

	public static ExplodeDieEffect createFromActor(Player owner, Actor actor, int radius, float maxTime, bool isMaverick, Point? overrideCenterPos = null, bool doExplosion = true) {
		return new ExplodeDieEffect(owner, overrideCenterPos ?? actor.getCenterPos(), actor.pos, actor.sprite.name, actor.xDir, actor.zIndex, true, radius, maxTime, isMaverick, doExplosion);
	}

	public override void update() {
		if (!owner?.ownedByLocalPlayer == true) {
			destroySelf();
			return;
		}

		base.update();

		if (host != null) {
			pos = host.pos;
		}

		timer -= Global.spf;
		if (timer <= 0) {
			exploder?.destroySelf();
			destroySelf();
			return;
		}

		spawnTime += Global.spf;
		if (spawnTime >= 0.125f) {
			spawnTime = 0;
			int randX = Helpers.randomRange(-radius, radius);
			int randY = Helpers.randomRange(-radius, radius);
			var randomPos = pos.addxy(randX, randY);

			if (owner != null && owner.ownedByLocalPlayer) {
				new Anim(randomPos, "explosion", 1, owner.getNextActorNetId(), true, sendRpc: true);
			}
		}
	}

	public void changeSprite(string newSprite) {
		exploder?.changeSprite(newSprite, true);
	}

	public override void destroySelf() {
		base.destroySelf();
		destroyed = true;
		if (exploder != null && !exploder.destroyed) {
			exploder.destroySelf();
		}
	}
}

public class DieEffect : Effect {
	public float timer = 100;
	public float spawnCount = 0;
	public List<DieEffectParticles> dieEffects = new List<DieEffectParticles>();
	public float repeatCount = 0;
	public int charNum;

	public DieEffect(Point centerPos, int charNum, bool sendRpc = false) : base(centerPos) {
		this.charNum = charNum;
		if (sendRpc) {
			RPC.createEffect.sendRpc(EffectIds.DieEffect, centerPos, (byte)charNum);
		}
	}

	public override void update() {
		base.update();
		var repeat = 1;
		var repeatPeriod = 0.5;
		timer += Global.spf;
		if (timer > repeatPeriod) {
			timer = 0;
			repeatCount++;
			if (repeatCount > repeat) {
			} else {
				var dieEffect = new DieEffectParticles(pos, charNum);
				dieEffects.Add(dieEffect);
			}
		}
		foreach (var dieEffect in dieEffects) {
			if (!dieEffect.destroyed)
				dieEffect.update();
		}
		if (dieEffects[0].destroyed)
			destroySelf();
	}

	public override void render(float offsetX, float offsetY) {
		foreach (var dieEffect in dieEffects) {
			if (!dieEffect.destroyed)
				dieEffect.render(offsetX, offsetY);
		}
	}
}
