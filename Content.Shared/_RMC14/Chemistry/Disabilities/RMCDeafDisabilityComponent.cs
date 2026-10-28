using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chemistry.Disabilities;

// TODO RMC14: cm13 also grants this from flashbangs at 15+ ear damage, & cures it with brain surgery
[RegisterComponent, NetworkedComponent]
public sealed partial class RMCDeafDisabilityComponent : Component;
