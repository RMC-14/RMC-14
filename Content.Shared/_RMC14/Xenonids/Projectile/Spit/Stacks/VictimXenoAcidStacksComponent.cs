using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Projectile.Spit.Stacks;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(XenoSpitSystem))]
public sealed partial class VictimXenoAcidStacksComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Current;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan LastIncrement;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan LastDecrement;

    [DataField, AutoNetworkedField]
    public TimeSpan IncrementFor = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public TimeSpan DecrementEvery = TimeSpan.FromSeconds(4);
}
