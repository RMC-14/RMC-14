using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Medical.Injectors;

[RegisterComponent, NetworkedComponent]
[Access(typeof(CMRefillableSolutionSystem))]
public sealed partial class CMSolutionRefillerComponent : Component
{
}
