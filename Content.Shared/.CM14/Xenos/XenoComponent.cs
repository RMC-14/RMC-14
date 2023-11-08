using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.CM14.Xenos;

[RegisterComponent, NetworkedComponent]
public sealed partial class XenoComponent : Component
{
    [DataField]
    public TimeSpan EvolveIn;

    [DataField]
    public List<EntProtoId> EvolvesTo = new();

    [DataField]
    public EntProtoId EvolveActionId = "ActionXenoEvolve";

    [DataField]
    public EntityUid? EvolveAction;
}
