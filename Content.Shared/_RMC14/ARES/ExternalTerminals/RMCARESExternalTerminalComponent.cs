using Content.Shared._RMC14.ARES.Logs;
using Content.Shared._RMC14.ARES.Tabs;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.Access;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.ARES.ExternalTerminals;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class RMCARESExternalTerminalComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId<IFFFactionComponent> Faction = "FactionMarine";

    [DataField, AutoNetworkedField]
    public EntityUid? ARESCore;

    [DataField, AutoNetworkedField]
    public HashSet<EntProtoId<RMCARESTabCategoryComponent>> ShownCategories = new();

    [DataField, AutoNetworkedField]
    public HashSet<EntProtoId<RMCARESTabComponent>> ShownTabs = new();

    [DataField, AutoNetworkedField]
    public HashSet<EntProtoId<RMCARESLogTypeComponent>> ShownLogs = new();

    [DataField, AutoNetworkedField]
    public EntProtoId<RMCARESLogTypeComponent>? SelectedLogPage;

    [DataField, AutoNetworkedField]
    public List<string> Logs = new();

    [DataField, AutoNetworkedField]
    public int LogsLength = 0;

    [DataField, AutoNetworkedField]
    public bool ShowsLogs = false;

    [DataField, AutoNetworkedField]
    public string LoggedInUser = "";

    [DataField, AutoNetworkedField]
    public bool LoggedIn = false;

    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<AccessLevelPrototype>> Accesses = new();

    [DataField, AutoNetworkedField]
    public Color Color = Color.Blue;
}
