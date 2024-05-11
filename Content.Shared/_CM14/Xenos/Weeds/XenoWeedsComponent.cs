using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._CM14.Xenos.Weeds;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoWeedsSystem))]
public sealed partial class XenoWeedsComponent : Component
{
    [DataField]
    public int Range = 5;

    [DataField, AutoNetworkedField]
    public bool IsSource = true;

    [DataField, AutoNetworkedField]
    public EntityUid? Source;

    [DataField]
    public EntProtoId Spawns = "XenoWeeds";
}
