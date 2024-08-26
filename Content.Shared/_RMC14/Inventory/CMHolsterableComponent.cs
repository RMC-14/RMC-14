using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Inventory;

// Component denoting if an item can be inserted into a holster
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedCMInventorySystem))]
public sealed partial class CMHolsterableComponent : Component;
