using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.PowerLoader;

[RegisterComponent, NetworkedComponent]
[Access(typeof(PowerLoaderSystem))]
public sealed partial class ActivePowerLoaderPilotComponent : Component;
