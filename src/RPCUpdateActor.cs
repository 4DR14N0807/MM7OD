﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using Lidgren.Network;

namespace MMXOnline;

public partial class Actor {
	public virtual List<byte>? getCustomActorNetData() {
		return null;
	}

	public virtual void updateCustomActorNetData(byte[] data) { }

	public void sendActorNetData() {
		if (netId == null) {
			return;
		}
		byte[] networkIdBytes = Helpers.convertToBytes(netId.Value);
		if ((netId == 10 || netId == 11) && this is not Flag) {
			string msg = string.Format(
				"NetId {0} was not flag. Was {1}", netId.Value.ToString(), this.GetType().ToString()
			);
			throw new Exception(msg);
		}
		bool send = false;
		var args = new List<byte>() { networkIdBytes[0], networkIdBytes[1] };
		ushort spriteIndex = Global.spriteIndexByName.GetValueOrCreate(sprite.name, ushort.MaxValue);

		// These masks are for whether to send the following fields or not.
		bool[] mask = new bool[8];

		// Add the mask
		args.Add(Helpers.boolArrayToByte(mask));

		// Pos.
		if (!isStatic && lastPos != pos) {
			byte[] xBytes = BitConverter.GetBytes(pos.x);
			byte[] yBytes = BitConverter.GetBytes(pos.y);
			args.AddRange(xBytes);
			args.AddRange(yBytes);
			mask[0] = true;
			send = true;
		}
		// Scale.
		if (syncScale) {
			args.Add((byte)MathF.Round(xScale * 20));
			args.Add((byte)MathF.Round(yScale * 20));
			mask[1] = true;
			send = true;
		}
		// Do not send sprite data if not in the sprite table.
		if (spriteIndex != ushort.MaxValue) {
			// Sprite index.
			if (lastSpriteIndex != spriteIndex) {
				byte[] spriteBytes = BitConverter.GetBytes((ushort)spriteIndex);
				args.AddRange(spriteBytes);
				mask[2] = true;
				send = true;
			}
			// Frame index.
			if (sprite.totalFrameNum != 0 && lastFrameIndex != frameIndex) {
				args.Add((byte)frameIndex);
				mask[3] = true;
				send = true;
			}
		}
		// Angle.
		if (angleSet && lastAngle != byteAngle) {
			args.Add((byte)MathF.Round(byteAngle));
			mask[4] = true;
			send = true;
		}
		// The rest are just contain actual bool data.
		mask[4] = visible;                      // Visibility
		mask[5] = xDir <= -1 ? false : true;    // xDir
		mask[6] = yDir <= -1 ? false : true;    // yDir

		// Check if anything changed on these bools.
		if (lastXDir != xDir || lastYDir != yDir || lastVisible != visible) {
			send = true;
		}

		List<byte>? customData = getCustomActorNetData();
		if (customData != null) {
			args.AddRange(customData);
			send = true;
		}
		// Send if anything changed.
		// Otherwise skip.
		if (send) {
			Global.serverClient?.rpc(RPC.updateActor, args.ToArray());
		}

		lastPos = pos;
		lastSpriteIndex = spriteIndex;
		lastFrameIndex = frameIndex;
		lastXDir = xDir;
		lastYDir = yDir;
		lastAngle = byteAngle;
		lastVisible = visible;
	}
}

public class RPCUpdateActor : RPC {
	public RPCUpdateActor() {
		netDeliveryMethod = NetDeliveryMethod.ReliableSequenced;
		isPreUpdate = true;
	}

	public override void invoke(params byte[] arguments) {
		if (Global.level == null || !Global.level.started) return;
		int i = 0;

		// Actor ID. Return if does not exist or we own it.
		ushort netId = BitConverter.ToUInt16([ arguments[0], arguments[1] ]);
		Actor? actor = Global.level.getActorByNetId(netId, true);
		if (actor == null || actor.ownedByLocalPlayer) {
			return;
		};
		i += 2;

		// Bool mask
		bool[] mask = Helpers.byteToBoolArray(arguments[i]);
		i++;

		actor.visible = mask[5];
		actor.xDir = mask[6] ? 1 : -1;
		actor.yDir = mask[7] ? 1 : -1;

		// Pos.
		if (mask[0]) {
			float posX = BitConverter.ToSingle(arguments[i..(i + 4)]);
			i += 4;
			float posY = BitConverter.ToSingle(arguments[i..(i + 4)]);
			i += 4;

			actor.pos.x = posX;
			actor.pos.y = posY;
		}
		// Scale.
		if (mask[1]) {
			actor.xScale = arguments[i++] / 20f;
			actor.yScale = arguments[i++] / 20f;
		}
		// Sprite index.
		if (mask[2]) {
			int spriteIndex = BitConverter.ToUInt16(arguments[i..(i + 2)]);
			if (spriteIndex >= 0 && spriteIndex < Global.spriteCount) {
				string spriteName = Global.spriteNameByIndex[index];
				actor.changeSprite(spriteName, true);
			}
			i += 2;
		}
		// Frame index.
		if (mask[3]) {
			actor.frameIndex = arguments[i++];
		}
		// Angle.
		if (mask[4]) {
			actor.byteAngle = BitConverter.ToSingle(arguments[i..(i + 4)]);
			i += 4;
		}

		try {
			// We parse custom data here.
			if (i < arguments.Length) {
				actor.updateCustomActorNetData(arguments[i..]);
			}
		}
		catch (IndexOutOfRangeException exception) {
			string playerName = "null";
			if (actor is Character character) {
				playerName = character.player.name;
			}
			else if (actor.netOwner?.name != null) {
				playerName = actor.netOwner.name;
			}
			string msg = (
				"Index out of bounds.\n" + 
				$"Actor type: {actor.GetType()}, " +
				$"args len: {arguments.Length}, " +
				$"extra args pos: {i}, " + 
				$"netId: {netId} " +
				$"maskBool: {netId.ToString()} " +
				$"player: {playerName}"
			);

			throw new Exception(msg, exception.InnerException);
		}

		actor.lastNetUpdate = Global.time;
	}
}
