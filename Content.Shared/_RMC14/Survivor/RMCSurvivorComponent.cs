using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Survivor;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SurvivorSystem))]
public sealed partial class RMCSurvivorComponent : Component;
