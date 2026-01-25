using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Atmos;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedRMCFlammableSystem))]
public sealed partial class RMCStopDropRollVisualsComponent : Component;
