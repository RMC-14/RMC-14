using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.TacticalMap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedTacticalMapSystem), typeof(AreaInfoSystem))]
public sealed partial class AreaInfoComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<AlertPrototype> Alert = "AreaInfo";

    [DataField, AutoNetworkedField]
    public TimeSpan NextUpdateTime;

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(2);
}
