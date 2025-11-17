using Content.Shared._RMC14.ARES.Logs;
using Content.Shared.Access;
using Content.Shared.Database;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace  Content.Shared._RMC14.ARES.Tabs;
/// <summary>
/// A tab displayed inside an external terminal.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCARESTabComponent : Component
{
    /// <summary>
    /// The permission required for this tab.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<AccessLevelPrototype>> Permissions = new();

    /// <summary>
    /// The Category this tab will be sorted under.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId<RMCARESTabCategoryComponent> Category;
}
