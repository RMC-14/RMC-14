using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CM14.Xenonids.Weeds;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoWeedsSystem))]
public sealed partial class XenoWeedableComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId Spawn;

    [DataField, AutoNetworkedField]
    public EntityUid? Entity;
}
