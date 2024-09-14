using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.TacticalMap;

[RegisterComponent, NetworkedComponent]
[Access(typeof(TacticalMapSystem))]
public sealed partial class TacticalMapComputerComponent : Component;
