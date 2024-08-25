using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Inventory;

// TODO RMC14 add to rifle holster
// TODO RMC14 add to machete scabbard pouch
// TODO RMC14 add to all large scabbards (machete scabbard, katana scabbard, m63 holster rig)
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedCMInventorySystem))]
public sealed partial class CMHolsterComponent : Component
{
    // List of entities that can be (un)holstered
    [DataField]
    public List<EntityUid> Contents = new();
}
