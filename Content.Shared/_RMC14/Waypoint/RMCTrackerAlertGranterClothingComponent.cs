using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Waypoint;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCTrackerAlertGranterClothingComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<AlertPrototype>, RMCTrackerAlert> Alerts = [];
}
