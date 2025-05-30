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
			if (this is Character) {
				throw new Exception($"Error character {getActorTypeName()} has a null net id");
			}
			return;
		}

		byte[] networkIdBytes = Helpers.convertToBytes(netId.Value);
		if (netId < Level.firstNormalNetId && this is not Flag and not ControlPoint) {
			string msg = $"NetId {netId.Value} was not flag or system object. Was {getActorTypeName()}";
			throw new Exception(msg);
		}
		bool send = false;
		var args = new List<byte>() { networkIdBytes[0], networkIdBytes[1] };
		ushort spriteIndex = Global.spriteIndexByName.GetValueOrCreate(sprite.name, ushort.MaxValue);

		// These masks are for whether to send the following fields or not.
		bool[] mask = new bool[8];

		// Add a dummy mask.
		int maskPos = args.Count;
		args.Add(0);

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
			// We also send both changed if for some reason packets get lost.
			bool frameChanged = sprite.totalFrameNum != 0 && lastFrameIndex != frameIndex;
			// Sprite index.
			if (lastSpriteIndex != spriteIndex || frameChanged) {
				byte[] spriteBytes = BitConverter.GetBytes((ushort)spriteIndex);
				args.AddRange(spriteBytes);
				mask[2] = true;
				send = true;
			}
			// Frame index.
			if (frameChanged) {
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
		mask[5] = visible; // Visibility
		mask[6] = (xDir > -1); // xDir
		mask[7] = (yDir > -1); // yDir

		// Check if anything changed on these bools.
		if (lastXDir != xDir || lastYDir != yDir || lastVisible != visible) {
			send = true;
		}

		List<byte>? customData = getCustomActorNetData();
		if (customData != null && customData.Count > 0) {
			args.AddRange(customData);
			send = true;
		}

		// Update the mask info.
		args[maskPos] = Helpers.boolArrayToByte(mask);

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
		netDeliveryMethod = NetDeliveryMethod.ReliableOrdered;
		isPreUpdate = true;
		channel = RpcChannels.UpdateActor;
	}

	public override void invoke(params byte[] arguments) {
		if (Global.level == null || !Global.level.started) return;
		int i = 0;

		// Actor ID. Return if does not exist or we own it.
		ushort netId = BitConverter.ToUInt16(arguments.AsSpan()[0..2]);
		Actor? actor = Global.level.getActorByNetId(netId, true);
		if (actor == null || actor.ownedByLocalPlayer) {
			return;
		};
		int frameSinceLastUpdate = Global.frameCount - actor.lastNetFrame;

		i += 2;

		// Bool mask
		byte maskByte = arguments[i];
		bool[] mask = Helpers.byteToBoolArray(arguments[i]);
		i++;

		actor.visible = mask[5];
		actor.xDir = mask[6] ? 1 : -1;
		actor.yDir = mask[7] ? 1 : -1;

		// Pos.
		if (mask[0]) {
			Point newPos = new();
			newPos.x = BitConverter.ToSingle(arguments.AsSpan()[i..(i + 4)]);
			i += 4;
			newPos.y = BitConverter.ToSingle(arguments.AsSpan()[i..(i + 4)]);
			i += 4;

			if (!actor.canBeLocal &&
				actor.interplorateNetPos &&
				frameSinceLastUpdate > 2 &&
				actor.pos.round().distanceTo(newPos.round()) > 1
			) {
				actor.targetNetPos = newPos;
				newPos = newPos.subtract(actor.pos).times(0.5f);
			} else {
				actor.targetNetPos = null;
			}
			actor.changePos(newPos);
		}
		// Scale.
		if (mask[1]) {
			actor.xScale = arguments[i++] / 20f;
			actor.yScale = arguments[i++] / 20f;
		}
		// Sprite index.
		bool spriteChanged = false;
		bool spriteError = false;
		if (mask[2]) {
			int spriteIndex = BitConverter.ToUInt16(arguments.AsSpan()[i..(i + 2)]);
			if (spriteIndex >= 0 && spriteIndex < Global.spriteCount) {
				string spriteName = Global.spriteNameByIndex[spriteIndex];
				actor.changeSprite(spriteName, true);
			} else {
				spriteError = true;
			}
			spriteChanged = true;
			i += 2;
		}
		// Frame index.
		if (mask[3] && !spriteError) {
			actor.frameIndex = arguments[i++];
			spriteChanged = true;
		}
		// Angle.
		if (mask[4]) {
			actor.byteAngle = arguments[i++];
		}
		if (spriteChanged) {
			actor.updateHitboxes();
		}

		try {
			// We parse custom data here.
			if (i < arguments.Length) {
				actor.updateCustomActorNetData(arguments[i..]);
			}
		}
		catch {
			string playerName = "null";
			if (actor is Character character) {
				playerName = character.player.name;
			}
			else if (actor.netOwner?.name != null) {
				playerName = actor.netOwner.name;
			}
			Program.exceptionExtraData = (
				"Index out of bounds.\n" + 
				$"Actor type: {actor.getActorTypeName()}, " +
				$"player: {playerName}, " +
				$"args len: {arguments.Length}, " +
				$"extra args pos: {i}, " + 
				$"netId: {netId}, " +
				$"maskBool: {Convert.ToString(maskByte, 2).PadLeft(8, '0')}"
			);

			throw;
		}

		actor.lastNetUpdate = Global.time;
		actor.lastNetFrame = Global.frameCount;
	}
}
