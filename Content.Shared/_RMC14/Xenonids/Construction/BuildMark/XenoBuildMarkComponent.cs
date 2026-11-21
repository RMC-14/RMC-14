using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Construction.BuildMark;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(XenoBuildMarkSystem))]
public sealed partial class XenoBuildMarkComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Planter;

    [DataField, AutoNetworkedField]
    public EntProtoId? Structure;

    [DataField, AutoNetworkedField]
    public bool Queued;

    [DataField]
    public bool PlanterInRange;

    [DataField]
    public bool RequiresSupport;

    [DataField]
    public Dictionary<EntProtoId, string> States = new();

    [DataField]
    public string DefaultState = "wall";

    [DataField]
    public Color QueuedColor = Color.Yellow;
}
