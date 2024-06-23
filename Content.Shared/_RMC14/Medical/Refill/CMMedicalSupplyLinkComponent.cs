using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.Refill;

[RegisterComponent, NetworkedComponent]
[Access(typeof(CMSolutionRefillerComponent))]
public sealed partial class CMMedicalSupplyLinkComponent : Component;
