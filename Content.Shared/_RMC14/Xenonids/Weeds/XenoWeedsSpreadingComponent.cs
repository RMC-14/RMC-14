using Content.Shared._RMC14.Xenonids.Construction;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Xenonids.Weeds;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedXenoWeedsSystem), typeof(SharedXenoConstructionSystem))]
public sealed partial class XenoWeedsSpreadingComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan SpreadDelay = TimeSpan.FromSeconds(3.33f);

    [DataField, AutoNetworkedField]
    public TimeSpan RepairedSpreadDelay = TimeSpan.FromSeconds(15);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan SpreadAt;
}
