using Content.Shared.Explosion;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Explosion;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class RMCExplosion
{
    [DataField]
    public ProtoId<ExplosionPrototype> Type = "RMC";

    [DataField]
    public float Total;

    [DataField]
    public float Slope;

    [DataField]
    public float Max;
}
