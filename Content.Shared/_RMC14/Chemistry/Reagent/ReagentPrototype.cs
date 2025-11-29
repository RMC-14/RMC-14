// ReSharper disable CheckNamespace

using Content.Shared.FixedPoint;

namespace Content.Shared.Chemistry.Reagent;

public partial class ReagentPrototype
{
    [DataField]
    public bool Unknown;

    [DataField]
    public FixedPoint2? Overdose;

    [DataField]
    public FixedPoint2? CriticalOverdose;

    [DataField]
    public FixedPoint2 Intensity;

    [DataField]
    public FixedPoint2 Duration;

    [DataField]
    public bool Toxin;

    [DataField]
    public bool Alcohol;
}
