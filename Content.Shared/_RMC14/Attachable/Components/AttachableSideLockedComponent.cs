using Content.Shared._RMC14.Attachable.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Attachable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AttachableToggleableSystem))]
public sealed partial class AttachableSideLockedComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<EntityUid> AttachableList = new();

    /// <summary>
    /// The cardinal direction the attachments are locked into. In this case, direction is counted as a full 180 degrees, rather than 90.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Direction? LockedDirection;
}
