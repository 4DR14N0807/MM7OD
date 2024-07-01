using System;
using System.Collections.Generic;
using ProtoBuf;

namespace MMXOnline;

public enum BassWeaponIds {
	BassBuster,
	IceWall,
	CopyVision,
	SpreadDrill,
	WaveBurner,
	RemoteMine,
	LightingBolt,
	TenguBlade,
	MagicCard,
}

[ProtoContract]
public class BassLoadout {
	[ProtoMember(1)] public int weapon1;
	[ProtoMember(2)] public int weapon2;
	[ProtoMember(3)] public int weapon3;

	public List<int> getBassWeaponIndices() {
		return new List<int>() { weapon1, weapon2, weapon3 };
	}

	public static BassLoadout createDefault() {
		return new BassLoadout() {
			weapon1 = 0, weapon2 = 1, weapon3 = 2
		};
	}

	public void validate() {
		if (weapon1 < 0 || weapon1 > 9) weapon1 = 0;
		if (weapon2 < 0 || weapon2 > 9) weapon2 = 0;
		if (weapon3 < 0 || weapon3 > 9) weapon3 = 0;

		if ((weapon1 == weapon2 && weapon1 >= 0) ||
			(weapon1 == weapon3 && weapon2 >= 0) ||
			(weapon2 == weapon3 && weapon3 >= 0)) {
			weapon1 = 0;
			weapon2 = 1;
			weapon3 = 2;
		}
	}
}
