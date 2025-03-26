using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Weeds;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoWeedsSystem))]
public sealed partial class XenoWallWeedsComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Weeds;
}
