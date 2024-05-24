using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Medical.Refill;

[RegisterComponent, NetworkedComponent]
[Access(typeof(CMSolutionRefillerComponent))]
public sealed partial class CMMedicalSupplyLinkComponent : Component;
