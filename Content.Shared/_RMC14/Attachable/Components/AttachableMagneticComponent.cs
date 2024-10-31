using Content.Shared._RMC14.Attachable.Systems;
using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Attachable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AttachableMagneticSystem))]
public sealed partial class AttachableMagneticComponent : Component
{
    [DataField, AutoNetworkedField]
    public SlotFlags MagnetizeToSlots = SlotFlags.SUITSTORAGE;
}
