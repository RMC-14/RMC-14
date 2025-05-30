using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Hands;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCHandsSystem))]
public sealed partial class DropOnUseInHandComponent : Component;
