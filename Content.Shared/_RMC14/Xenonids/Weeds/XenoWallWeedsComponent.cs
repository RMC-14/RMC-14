using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Weeds;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoWeedsSystem))]
public sealed partial class XenoWallWeedsComponent : Component
{
    /// <summary>
    /// The source node causing the wall weeds to grow.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Weeds;

    /// <summary>
    /// The surface that the wall weeds are growing on.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? WeededSurface;
}
