using Content.Shared.FixedPoint;

namespace Content.Shared._RMC14.Pain;

public sealed partial class PainLevelSpecifier
{
    public Dictionary<string, (FixedPoint2, FixedPoint2)> DamageDict { get; set; } = new();
}
