using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Explosion.Implosion;

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class RMCImplosion
{
    [DataField]
    public float PullRange;

    [DataField]
    public float PullDistance;

    [DataField]
    public float PullSpeed;

    [DataField]
    public bool IgnoreSize = true;
}
