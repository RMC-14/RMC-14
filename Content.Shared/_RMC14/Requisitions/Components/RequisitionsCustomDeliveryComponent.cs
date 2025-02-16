using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Requisitions.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedRequisitionsSystem))]
public sealed partial class RequisitionsCustomDeliveryComponent : Component;
