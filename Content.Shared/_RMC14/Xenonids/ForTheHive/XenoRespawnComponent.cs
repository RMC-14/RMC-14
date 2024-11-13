using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.ForTheHive;

[RegisterComponent, NetworkedComponent]
public sealed partial class XenoRespawnComponent : Component
{
    [DataField]
    public EntityUid? Hive;

    [DataField]
    public TimeSpan RespawnAt;

    [DataField]
    public bool RespawnAtCorpse = false;

    [DataField]
    public EntityCoordinates? CorpseLocation;

    [DataField]
    public EntProtoId Larva = "CMXenoLarva";
}
