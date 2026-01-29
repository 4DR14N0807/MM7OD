using System;
using System.Collections.Generic;

namespace MMXOnline;

public class FreezeMCrystalPiece : Anim {
	Point bounceVel;
	public FreezeMCrystalPiece(
		Point pos, int xDir, int yDir, ushort? netId, Point vel, Point bounceVel, int frame
	) : base(
		pos, "freezem_crystal_pieces", xDir, netId, false
	) {
		this.yDir = yDir;
		this.vel = vel;
		this.bounceVel = bounceVel;
		useGravity = true;
		frameSpeed = 0;
		frameIndex = frame;
		if (collider != null) collider.wallOnly = true;
	}

	public override void update() {
		base.update();

		visible = Global.isOnFrameCycle(2);
	}

	public override void onCollision(CollideData other) {
		base.onCollision(other);
		if (
			(other.gameObject is Wall ||
			other.gameObject is MovingPlatform ||
			other.gameObject is Actor { isSolidWall: true }) &&

			collider?.wallOnly == true
		) {
			vel = bounceVel;
			if (collider != null) collider.wallOnly = false;
		}
	}
}
