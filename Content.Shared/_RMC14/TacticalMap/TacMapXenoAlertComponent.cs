using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.TacticalMap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedTacticalMapSystem), typeof(TacMapXenoAlertSystem))]
public sealed partial class TacMapXenoAlertComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<AlertPrototype> Alert = "XenoTacMapAlert";

    [DataField, AutoNetworkedField]
    public TimeSpan NextUpdateTime;

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(2);
}
