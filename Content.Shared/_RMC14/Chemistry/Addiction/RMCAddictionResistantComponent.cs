using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chemistry.Addiction;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCAddictionSystem))]
public sealed partial class RMCAddictionResistantComponent : Component;
