using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using static Content.Shared._RMC14.TacticalMap.TacticalMapComponent;

namespace Content.Shared._RMC14.TacticalMap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(SharedTacticalMapSystem))]
public sealed partial class TacticalMapUserComponent : Component
{
    public override bool SendOnlyToOwner => true;

    [DataField, AutoNetworkedField]
    public EntProtoId ActionId = "RMCActionOpenTacticalMap";

    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    [DataField, AutoNetworkedField]
    public bool LiveUpdate;

    [DataField, AutoNetworkedField]
    public bool Marines;

    [DataField, AutoNetworkedField]
    public bool Xenos;

    [DataField, AutoNetworkedField]
    public Dictionary<int, TacticalMapBlip> MarineBlips = new();

    [DataField, AutoNetworkedField]
    public Dictionary<int, TacticalMapBlip> XenoBlips = new();
}
