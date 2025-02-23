using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.TacticalMap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedTacticalMapSystem), typeof(TacMapMarineAlertSystem))]
public sealed partial class TacMapMarineAlertComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<AlertPrototype> Alert = "MarineTacMapAlert";

    [DataField, AutoNetworkedField]
    public TimeSpan NextUpdateTime;

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(2);
}
