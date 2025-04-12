using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Intel;

[RegisterComponent, NetworkedComponent]
[Access(typeof(IntelSystem))]
public sealed partial class IntelRecoverCorpsesAreaComponent : Component;
