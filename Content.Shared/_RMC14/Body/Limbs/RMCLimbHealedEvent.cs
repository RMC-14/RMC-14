using Content.Shared._RMC14.Body.Zones;
using Content.Shared.FixedPoint;

namespace Content.Shared._RMC14.Body.Limbs;

/// <summary>
///     raised on the mob on the server for each limb that healed, once the heal has been split across limbs
///     a full heal like rejuvenate doesn't raise this so listen for that separately
/// </summary>
[ByRefEvent]
public readonly record struct RMCLimbHealedEvent(
    RMCTargetZone Zone,
    FixedPoint2 Brute,
    FixedPoint2 Burn,
    EntityUid? Origin,
    EntityUid? Tool);
