using Content.Shared.Explosion;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.OrbitalCannon;

[DataRecord]
[Serializable, NetSerializable]
public readonly record struct OrbitalCannonExplosion(
    ProtoId<ExplosionPrototype> Type,
    float Total,
    float Slope,
    float Max,
    TimeSpan Delay,
    EntProtoId? Fire,
    int FireRange = 18,
    int Intensity = 80,
    int Duration = 70
);
