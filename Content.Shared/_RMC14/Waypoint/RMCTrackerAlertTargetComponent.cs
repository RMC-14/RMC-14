using Content.Shared.Alert;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Waypoint;

[RegisterComponent]
public sealed partial class RMCTrackerAlertTargetComponent : Component
{
    [DataField(required: true)]
    public ProtoId<AlertPrototype> AlertPrototype;

    [DataField]
    public int Priority;
}
