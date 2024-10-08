using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Standing;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCStandingSystem))]
public sealed partial class DropItemsOnRestComponent : Component;
