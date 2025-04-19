using Content.Shared.Inventory;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Armor.Magnetic;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCMagneticSystem))]
public sealed partial class RMCMagneticArmorComponent : Component
{
    [DataField, AutoNetworkedField]
    public SlotFlags AllowMagnetizeToSlots = SlotFlags.SUITSTORAGE;
}
