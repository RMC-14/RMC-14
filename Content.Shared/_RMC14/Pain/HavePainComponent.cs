using Content.Shared.FixedPoint;

namespace Content.Shared._RMC14.Pain;

public sealed partial class HavePainComponent : Component
{
    [DataField]
    public FixedPoint2 PainReductionDecreaseRate = FixedPoint2.New(0.25f);

    [DataField]
    public FixedPoint2 BrutePainMultiplier = FixedPoint2.New(1);
    [DataField]
    public FixedPoint2 BurnPainMultiplier = FixedPoint2.New(1.2f);
    [DataField]
    public FixedPoint2 ToxPainMultiplier = FixedPoint2.New(1.5f);

    [DataField]
    public FixedPoint2 PainSpeedVerySlow = FixedPoint2.New(4.5f);
    [DataField]
    public FixedPoint2 PainSpeedSlow = FixedPoint2.New(3.75f);
    [DataField]
    public FixedPoint2 PainSpeedHigh = FixedPoint2.New(2.75f);
    [DataField]
    public FixedPoint2 PainSpeedMed = FixedPoint2.New(1.5f);
    [DataField]
    public FixedPoint2 PainSpeedLow = FixedPoint2.New(1.0f);
}
