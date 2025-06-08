using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Tackle;

[RegisterComponent, NetworkedComponent]
[Access(typeof(TackleSystem))]
public sealed partial class RMCDisarmableComponent : Component;
