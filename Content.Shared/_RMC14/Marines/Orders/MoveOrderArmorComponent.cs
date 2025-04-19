using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Marines.Orders;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedMarineOrdersSystem))]
public sealed partial class MoveOrderArmorComponent : Component;
