using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.Sleeper;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedSleeperSystem))]
public sealed partial class SleeperResearchUpgradeComponent : Component;
