using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chemistry.Disabilities;

[RegisterComponent, NetworkedComponent, Access(typeof(RMCVisionDisabilitySystem))]
public sealed partial class RMCBlindDisabilityComponent : Component;
