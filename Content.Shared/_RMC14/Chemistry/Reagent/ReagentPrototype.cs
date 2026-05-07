// ReSharper disable CheckNamespace

using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

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
    public int Intensity;

    [DataField]
    public int Duration;

    [DataField]
    public int Radius;

    [DataField]
    public EntProtoId FireEntity = "RMCTileFire";

    [DataField]
    public FixedPoint2 IntensityMod;

    [DataField]
    public FixedPoint2 DurationMod;

    [DataField]
    public FixedPoint2 RadiusMod;

    [DataField]
    public bool FireSpread;

    [DataField]
    public bool Toxin;

    [DataField]
    public bool Alcohol;
}
