using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chemistry.Generation;

[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedRMCSynthesisSimulatorSystem))]
public sealed partial class RMCSynthesisSimulatorActiveComponent : Component;
