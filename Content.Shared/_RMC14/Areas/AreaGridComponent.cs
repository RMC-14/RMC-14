using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Areas;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AreaSystem))]
public sealed partial class AreaGridComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<Vector2i, EntProtoId<AreaComponent>> Areas = new();

    [DataField, AutoNetworkedField]
    public Dictionary<Vector2i, Color> Colors = new();

    [DataField, AutoNetworkedField]
    public Dictionary<EntProtoId<AreaComponent>, EntityUid> AreaEntities = new();

    [DataField, AutoNetworkedField]
    public Dictionary<Vector2i, string> Labels = new();
}
