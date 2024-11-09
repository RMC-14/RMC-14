using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.JoinXeno;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(JoinXenoSystem))]
public sealed partial class JoinXenoComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId ActionId = "ActionJoinXeno";

    [DataField, AutoNetworkedField]
    public EntityUid? Action;
}
