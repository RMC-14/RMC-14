using Content.Shared.FixedPoint;

namespace Content.Shared._CM14.Weapons.Ranged;

[ByRefEvent]
public record struct GetGunDamageModifierEvent(FixedPoint2 Multiplier);
