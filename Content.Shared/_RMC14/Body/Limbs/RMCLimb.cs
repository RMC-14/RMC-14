using Content.Shared.FixedPoint;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Body.Limbs;

/// <summary>
///     the damage sitting on one limb
/// </summary>
[DataRecord]
[Serializable, NetSerializable]
public readonly record struct RMCLimb(FixedPoint2 Brute, FixedPoint2 Burn);
