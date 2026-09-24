using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Fireman;

[RegisterComponent, NetworkedComponent]
[Access(typeof(FiremanCarrySystem))]
public sealed partial class BeingFiremanCarriedComponent : Component;
