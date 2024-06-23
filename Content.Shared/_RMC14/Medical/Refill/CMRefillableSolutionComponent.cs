using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Medical.Refill;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CMRefillableSolutionSystem))]
public sealed partial class CMRefillableSolutionComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public string Solution = string.Empty;

    [DataField(required: true), AutoNetworkedField]
    public Dictionary<ProtoId<ReagentPrototype>, FixedPoint2> Reagents = new();
}
