using Content.Shared._RMC14.Attachable.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Attachable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AttachableAutoPickupSystem))]
public sealed partial class AttachableAutoPickupComponent : Component
{
    [DataField, AutoNetworkedField]
    public string SlotId = "suitstorage";

    [DataField, AutoNetworkedField]
    public bool Removing;
}
