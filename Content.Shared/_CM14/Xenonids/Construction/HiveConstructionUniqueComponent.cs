using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CM14.Xenonids.Construction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoConstructionSystem))]
public sealed partial class HiveConstructionUniqueComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId Id;
}
