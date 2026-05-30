using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Hive;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(SharedXenoHiveSystem))]
public sealed partial class InvincibleHiveStructureComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId? Replace;

    [DataField, AutoNetworkedField]
    public EntProtoId? BlockerId;

    [DataField, AutoNetworkedField]
    public EntityUid? Blocker;

    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromMinutes(30);

    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan ReplaceAt;

    [DataField, AutoNetworkedField]
    public Color Color = Color.FromHex("#D800FF");
}
