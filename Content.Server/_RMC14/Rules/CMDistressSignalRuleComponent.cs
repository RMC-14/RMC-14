using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._RMC14.Rules;

[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(CMDistressSignalRuleSystem))]
public sealed partial class CMDistressSignalRuleComponent : Component
{
    [DataField]
    public List<EntProtoId> SquadIds = ["SquadAlpha", "SquadBravo", "SquadCharlie", "SquadDelta"];

    [DataField]
    public Dictionary<EntProtoId, EntityUid> Squads = new();

    [DataField]
    public EntityUid XenoMap;

    [DataField]
    public EntProtoId HiveId = "CMXenoHive";

    [DataField]
    public EntityUid Hive;

    // TODO RMC14
    [DataField]
    public bool Hijack;

    [DataField]
    public ProtoId<JobPrototype> QueenJob = "CMXenoQueen";

    [DataField]
    public EntProtoId QueenEnt = "CMXenoQueen";

    [DataField]
    public ProtoId<JobPrototype> XenoSelectableJob = "CMXenoSelectableXeno";

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

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? NextCheck;

    [DataField]
    public TimeSpan CheckEvery = TimeSpan.FromSeconds(5);

    [DataField, AutoNetworkedField]
    public TimeSpan? AbandonedAt;

    [DataField, AutoNetworkedField]
    public TimeSpan AbandonedDelay = TimeSpan.FromMinutes(5);

    [DataField]
    public SoundSpecifier HijackSong = new SoundCollectionSpecifier("RMCHijack", AudioParams.Default.WithVolume(-8));

    [DataField]
    public bool HijackSongPlayed;

    // TODO RMC14
    // [DataField]
    // public SoundSpecifier MajorMarineAudio = new SoundCollectionSpecifier("CMMarineMajor");
    //
    // [DataField]
    // public SoundSpecifier MinorMarineAudio = new SoundCollectionSpecifier("CMMarineMinor");
    //
    // [DataField]
    // public SoundSpecifier MajorXenoAudio = new SoundCollectionSpecifier("CMXenoMajor");
    //
    // [DataField]
    // public SoundSpecifier MinorXenoAudio = new SoundCollectionSpecifier("CMXenoMinor");
    //
    // [DataField]
    // public SoundSpecifier AllDiedAudio = new SoundCollectionSpecifier("CMAllDied");
}
