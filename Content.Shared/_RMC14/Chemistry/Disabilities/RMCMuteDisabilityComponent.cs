using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chemistry.Disabilities;

// TODO RMC14: cm13 cures this with brain surgery
[RegisterComponent, NetworkedComponent, Access(typeof(RMCMuteDisabilitySystem))]
public sealed partial class RMCMuteDisabilityComponent : Component;
