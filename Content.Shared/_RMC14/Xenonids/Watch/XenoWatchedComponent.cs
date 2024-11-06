using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Watch;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedWatchXenoSystem))]
public sealed partial class XenoWatchedComponent : Component
{
    [DataField]
    public HashSet<EntityUid> Watching = new();
}
