using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Armor;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCArmorVariantComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<string, EntProtoId> Types = new();

    [DataField, AutoNetworkedField]
    public EntProtoId DefaultType = string.Empty;
}
