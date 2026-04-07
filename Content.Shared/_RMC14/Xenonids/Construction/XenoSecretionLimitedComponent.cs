using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Construction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoConstructionSystem))]
public sealed partial class XenoSecretionLimitedComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId<XenoSecretionLimitedComponent>? Id;

    [DataField, AutoNetworkedField]
    public int Amount = 1;

    [DataField, AutoNetworkedField]
    public EntityUid? Xeno;
}
