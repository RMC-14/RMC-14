using Content.Shared._RMC14.Attachable.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Attachable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AttachableToggleableSystem))]
public sealed partial class AttachableDirectionLockedComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<EntityUid> AttachableList = new();

    [DataField, AutoNetworkedField]
    public Direction? LockedDirection;
}

