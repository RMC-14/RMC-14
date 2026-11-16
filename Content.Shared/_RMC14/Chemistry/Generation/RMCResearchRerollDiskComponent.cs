using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chemistry.Generation;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedRMCResearchComputerSystem))]
public sealed partial class RMCResearchRerollDiskComponent : Component;
