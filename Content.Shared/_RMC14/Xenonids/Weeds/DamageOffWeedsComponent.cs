using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Weeds;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(SharedXenoWeedsSystem))]
public sealed partial class DamageOffWeedsComponent : Component
{
    [DataField, AutoNetworkedField]
    public DamageSpecifier Damage = new();

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan? DamageAt;

    [DataField, AutoNetworkedField]
    public TimeSpan Every = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public bool RestingStopsDamage = true;
}
