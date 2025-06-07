// ReSharper disable CheckNamespace

using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.Chemistry.Reagent;

public sealed partial class ReagentPrototype
{
    [DataField]
    public bool Unknown;

    [DataField]
    public FixedPoint2? Overdose;

    // Variables for Explosive Reagents
    [DataField]
    public bool Explosive;

    [DataField]
    public FixedPoint2 Power;

    [DataField]
    public FixedPoint2 FalloffMod;

    // Chem fire support

    [DataField]
    public bool Flammable;

    // These three are Flamer related, Demolitions scanner may need to check for them though

    [DataField]
    public FixedPoint2 Intensity;

    [DataField]
    public FixedPoint2 Duration;

    [DataField]
    public FixedPoint2 FireRange;

    // These three are for flammable solutions that don't go in Flamer tanks

    [DataField]
    public FixedPoint2 IntensityMod;

    [DataField]
    public FixedPoint2 DurationMod;

    [DataField]
    public FixedPoint2 RadiusMod;

}
