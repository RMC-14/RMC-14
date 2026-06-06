using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Construction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoConstructionSystem))]
public sealed partial class XenoSecretionListComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<EntProtoId<XenoSecretionLimitedComponent>, HashSet<NetEntity>> Built = new();
}
