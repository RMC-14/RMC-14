using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Weeds;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoWeedsSystem))]
public sealed partial class XenoWallWeedsComponent : Component
{
    /// <summary>
    /// The weed tile which caused these wall weeds to grow.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? SourceWeeds;

    /// <summary>
    /// The entity that the wall weeds are attached to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? AttachedTo;
}
