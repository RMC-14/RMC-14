using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Inventory;

// TODO CM14 add to rifle holster
// TODO CM14 add to machete scabbard pouch
// TODO CM14 add to all large scabbards (machete scabbard, katana scabbard, m39 holster rig)
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedCMInventorySystem))]
public sealed partial class CMHolsterComponent : Component;
