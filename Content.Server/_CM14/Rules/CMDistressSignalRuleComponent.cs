using Robust.Shared.Prototypes;

namespace Content.Server._CM14.Rules;

[RegisterComponent]
[Access(typeof(CMDistressSignalRuleSystem))]
public sealed partial class CMDistressSignalRuleComponent : Component
{
    [DataField]
    public int PlayersPerXeno = 4;

    [DataField]
    public List<EntProtoId> SquadIds = ["SquadAlpha", "SquadBravo", "SquadCharlie", "SquadDelta"];

    [DataField]
    public Dictionary<EntProtoId, EntityUid> Squads = new();

    [DataField]
    public int NextSquad;

    [DataField]
    public EntityUid XenoMap;

    [DataField]
    public EntProtoId HiveId = "CMXenoHive";

    [DataField]
    public EntityUid Hive;

    // TODO CM14
    [DataField]
    public bool XenosEverOnShip;
}
