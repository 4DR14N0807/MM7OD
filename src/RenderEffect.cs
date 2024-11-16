namespace MMXOnline;

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

	// MM7 Charges
	RBlueCharge,
	RGreenCharge,
	NCrushCharge,
}

public class RenderEffect {
	public RenderEffectType type;
	public float time;
	public float flashTime;
	public RenderEffect(RenderEffectType type, float flashTime = 0, float time = float.MaxValue) {
		this.type = type;
		this.flashTime = flashTime;
		this.time = time;
	}

	public bool isFlashing() {
		return time < flashTime;
	}
}
