using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Inventory;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedCMInventorySystem))]
public sealed partial class CMVirtualItemComponent : Component;
