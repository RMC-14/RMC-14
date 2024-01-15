using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Medical.Injectors;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMRefillableSolutionSystem))]
public sealed partial class CMRefillableSolutionComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public string Solution;

    [DataField(required: true), AutoNetworkedField]
    public Dictionary<string, FixedPoint2> Reagents = new();
}
