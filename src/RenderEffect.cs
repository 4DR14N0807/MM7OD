﻿namespace MMXOnline;

public enum RenderEffectType {
	None,
	Hit,
	Flash,
	//StockedCharge,
	Invisible,
	InvisibleFlash,
	BlueShadow,
	RedShadow,
	Trail,
	GreenShadow,
	PurpleShadow,
	YellowShadow,
	OrangeShadow,
	//StockedSaber,
	BoomerangKTrail,
	SpeedDevilTrail,
	StealthModeBlue,
	StealthModeRed,
	Shake,
	ChargeGreen,
	ChargeOrange,
	ChargePink,
	ChargeYellow,
	ChargeBlue,
	ChargePurple,
	ChargeGreenBass,
	ChargeGreenBass2,
	ChargePurpleBass,

	// MM7 Charges
	RBlueCharge,
	RGreenCharge,
	NCrushCharge,
}

public class RenderEffect {
	public RenderEffectType type;
	public float time;
	public float maxTime;
	public float flashTime;
	public float cycleTime;

	public RenderEffect(RenderEffectType type, float flashTime = 0, float time = float.MaxValue, float cycleTime = -1) {
		this.type = type;
		this.flashTime = flashTime;
		this.maxTime = time;

		if (cycleTime >= flashTime) {
			this.cycleTime = cycleTime;
		} else {
			this.cycleTime = flashTime;
		}
	}

	public bool isFlashing() {
		return time % (flashTime * 2) >= cycleTime;
	}
}
