using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Content.Shared.Damage;

namespace Content.Shared._RMC14.Xenonids.Acid;

[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(SharedXenoAcidSystem))]
public sealed partial class DamageableCorrodingComponent : Component
{
    [DataField]
    public EntityUid Acid;

    [DataField]
    public float Dps;

    [DataField]
    public DamageSpecifier Damage;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextDamageAt;
}
