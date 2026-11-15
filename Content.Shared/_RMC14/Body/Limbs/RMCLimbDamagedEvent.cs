using Content.Shared._RMC14.Body.Zones;
using Content.Shared.FixedPoint;

namespace Content.Shared._RMC14.Body.Limbs;

/// <summary>
///     raised on the mob on the server for each limb that took damage, once the damage has been split across limbs
///     spillover is damage passed on from a full limb and should never take the limb it lands on off
/// </summary>
[ByRefEvent]
public readonly record struct RMCLimbDamagedEvent(
    RMCTargetZone Zone,
    FixedPoint2 Brute,
    FixedPoint2 Burn,
    RMCLimbHit Hit,
    bool Spillover,
    EntityUid? Origin,
    EntityUid? Tool);
