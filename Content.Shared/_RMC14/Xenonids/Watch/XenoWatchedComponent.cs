using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Watch;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoWatchSystem))]
public sealed partial class XenoWatchedComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> Watching = new();
}
