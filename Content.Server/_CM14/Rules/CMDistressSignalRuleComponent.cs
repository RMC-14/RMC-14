using Content.Shared._CM14.Weapons.Ranged.IFF;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._CM14.Rules;

[RegisterComponent, AutoGenerateComponentPause]
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

    [DataField]
    public ProtoId<JobPrototype> QueenJob = "CMXenoQueen";

    [DataField]
    public EntProtoId QueenEnt = "CMXenoQueen";

    [DataField]
    public ProtoId<JobPrototype> XenoSelectableJob = "CMXenoSelectableXenomorph";

    [DataField]
    public EntProtoId LarvaEnt = "CMXenoLarva";

    [DataField]
    public EntProtoId<IFFFactionComponent> MarineFaction = "FactionMarine";

    [DataField, AutoPausedField]
    public TimeSpan? QueenDiedCheck;

    [DataField]
    public TimeSpan QueenDiedDelay = TimeSpan.FromMinutes(10);

    [DataField]
    public DistressSignalRuleResult Result;

    [DataField]
    public SoundSpecifier MajorMarineAudio = new SoundCollectionSpecifier("CMMarineMajor");

    [DataField]
    public SoundSpecifier MinorMarineAudio = new SoundCollectionSpecifier("CMMarineMinor");

    [DataField]
    public SoundSpecifier MajorXenoAudio = new SoundCollectionSpecifier("CMXenoMajor");

    [DataField]
    public SoundSpecifier MinorXenoAudio = new SoundCollectionSpecifier("CMXenoMinor");

    [DataField]
    public SoundSpecifier AllDiedAudio = new SoundCollectionSpecifier("CMAllDied");
}
