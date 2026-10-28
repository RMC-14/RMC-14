using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chemistry.Disabilities;

[RegisterComponent, NetworkedComponent, Access(typeof(RMCDeafDisabilitySystem))]
public sealed partial class RMCDeafDisabilityComponent : Component;
