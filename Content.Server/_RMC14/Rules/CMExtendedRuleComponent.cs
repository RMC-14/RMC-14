using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._RMC14.Rules;

[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(CMExtendedRuleSystem))]
public sealed partial class CMExtendedRuleComponent : Component
{
    [DataField]
    public List<EntProtoId> SquadIds = ["SquadAlpha", "SquadBravo", "SquadCharlie", "SquadDelta"];

    [DataField]
    public Dictionary<EntProtoId, EntityUid> Squads = new();

    [DataField]
    public Dictionary<ProtoId<JobPrototype>, int> NextSquad = new();

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

    [DataField]
    public ExtendedRuleResult Result;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? NextCheck;

    [DataField]
    public TimeSpan CheckEvery = TimeSpan.FromSeconds(5);

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

    public Dictionary<CVarDef<float>, float> OriginalCVarValues = new();

    public bool ResetCVars;
}
