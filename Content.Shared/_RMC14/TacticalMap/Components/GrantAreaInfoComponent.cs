using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.TacticalMap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AreaInfoSystem))]
public sealed partial class GrantAreaInfoComponent : Component, IClothingSlots
{
    [DataField, AutoNetworkedField]
    public SlotFlags Slots { get; set; } = SlotFlags.EARS;
}
