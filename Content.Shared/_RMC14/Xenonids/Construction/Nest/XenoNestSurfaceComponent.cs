using Content.Shared._RMC14.Xenonids.Weeds;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Construction.Nest;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoNestSystem))]
public sealed partial class XenoNestSurfaceComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId Nest = "XenoNest";

    [DataField, AutoNetworkedField]
    public TimeSpan DoAfter = TimeSpan.FromSeconds(8);

    [DataField, AutoNetworkedField]
    public Dictionary<Direction, EntityUid> Nests = new();

    [DataField, AutoNetworkedField]
    [Access(typeof(XenoNestSystem), typeof(SharedXenoWeedsSystem))]
    public EntityUid? Weedable;
}
