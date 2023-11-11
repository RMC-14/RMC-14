using Robust.Shared.Prototypes;

namespace Content.Server._CM14.Rules;

[RegisterComponent]
[Access(typeof(CMRuleSystem))]
public sealed partial class CMRuleComponent : Component
{
    [DataField]
    public int PlayersPerXeno = 2; // TODO CM14

    [DataField]
    public List<EntProtoId> SquadIds = new() { "SquadAlpha", "SquadBravo", "SquadCharlie", "SquadDelta" };

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
}
