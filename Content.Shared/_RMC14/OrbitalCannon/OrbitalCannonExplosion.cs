using Content.Shared.Explosion;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.OrbitalCannon;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class OrbitalCannonExplosion
{
    [DataField]
    public ProtoId<ExplosionPrototype>? Type;

    [DataField]
    public float Total;

    [DataField]
    public float Slope;

    [DataField]
    public float Max;

    [DataField]
    public TimeSpan Delay;

    [DataField]
    public EntProtoId? Fire;

    [DataField]
    public int FireRange = 18;

    [DataField]
    public int Intensity = 80;

    [DataField]
    public int Duration = 70;

    [DataField]
    public int Times = 1;

    [DataField]
    public int TimesPer = 1;

    [DataField]
    public TimeSpan DelayPer;

    [DataField]
    public int Spread;

    [DataField]
    public bool CheckProtectionPer;

    [DataField]
    public EntProtoId? ExplosionEffect;
}
