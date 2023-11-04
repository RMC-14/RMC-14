using Robust.Shared.Prototypes;

namespace Content.Server.CM14.Rules;

[RegisterComponent]
[Access(typeof(CMRuleSystem))]
public sealed partial class CMRuleComponent : Component
{
    [DataField]
    public int PlayersPerXeno = 2; // TODO CM14

    [DataField]
    public List<EntProtoId> SquadIds = new() { "SquadAlpha", "SquadBeta", "SquadCharlie", "SquadDelta" };

    [DataField]
    public Dictionary<EntProtoId, EntityUid> Squads = new();

    [DataField]
    public int NextSquad = 0;

    [DataField]
    public EntityUid XenoMap;
}
