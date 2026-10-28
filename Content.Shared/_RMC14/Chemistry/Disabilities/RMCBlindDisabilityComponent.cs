using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chemistry.Disabilities;

// TODO RMC14: cm13 cures this with eye surgery & the autodoc
[RegisterComponent, NetworkedComponent, Access(typeof(RMCVisionDisabilitySystem))]
public sealed partial class RMCBlindDisabilityComponent : Component;
