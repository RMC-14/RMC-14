using Content.Shared._RMC14.Body.Zones;
using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Body.Limbs;

/// <summary>
///     what a limb is, how much it can take, what it hangs off and how likely a random hit lands on it
/// </summary>
[DataRecord]
[Serializable, NetSerializable]
public readonly record struct RMCLimbDefinition(FixedPoint2 MaxHealth, RMCTargetZone? Parent, float HitWeight);
