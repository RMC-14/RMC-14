using Content.Shared.FixedPoint;

namespace Content.Shared._RMC14.Weapons.Ranged;

[ByRefEvent]
public record struct GetDamageFalloffEvent(
    FixedPoint2 FalloffMultiplier,
    float Range
);
