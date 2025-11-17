using Content.Shared.Access;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.ARES.Tabs;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCARESTabCategoryComponent : Component
{
    /// <summary>
    /// The permission required to display this category
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<AccessLevelPrototype>> Permissions = new();
}
