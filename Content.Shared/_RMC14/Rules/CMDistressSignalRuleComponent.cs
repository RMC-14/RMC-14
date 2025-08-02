using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.Radio;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Rules;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class CMDistressSignalRuleComponent : Component
{
    [DataField]
    public List<EntProtoId> SquadIds = new() { "SquadAlpha", "SquadBravo", "SquadCharlie", "SquadDelta" };

    [DataField]
    public List<EntProtoId> ExtraSquadIds = new() { "SquadIntel", "SquadFORECON" };

    [DataField]
    public Dictionary<EntProtoId, EntityUid> Squads = new();

    [DataField]
    public EntityUid? XenoMap;

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
    public EntProtoId<IFFFactionComponent> SurvivorFaction = "FactionSurvivor";

    [DataField, AutoPausedField]
    public TimeSpan? QueenDiedCheck;

    [DataField]
    public TimeSpan QueenDiedDelay = TimeSpan.FromMinutes(10);

    [DataField]
    public DistressSignalRuleResult? Result;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? NextCheck;

    [DataField]
    public TimeSpan CheckEvery = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan? AbandonedAt;

    [DataField]
    public TimeSpan AbandonedDelay = TimeSpan.FromMinutes(5);

    [DataField]
    public SoundSpecifier HijackSong = new SoundCollectionSpecifier("RMCHijack", AudioParams.Default.WithVolume(-8));

    [DataField]
    public bool HijackSongPlayed;

    [DataField]
    public SoundSpecifier MajorMarineAudio = new SoundCollectionSpecifier("RMCMarineMajor");

    [DataField]
    public SoundSpecifier MinorMarineAudio = new SoundCollectionSpecifier("RMCMarineMinor");

    [DataField]
    public SoundSpecifier MajorXenoAudio = new SoundCollectionSpecifier("RMCXenoMajor");

    [DataField]
    public SoundSpecifier MinorXenoAudio = new SoundCollectionSpecifier("RMCXenoMinor");

    // TODO RMC14
    // [DataField]
    // public SoundSpecifier AllDiedAudio = new SoundCollectionSpecifier("CMAllDied");

    [DataField]
    public EntProtoId? LandingZoneGas = "RMCLandingZoneGas";

    [DataField]
    public ProtoId<JobPrototype> CivilianSurvivorJob = "CMSurvivor";

    [DataField]
    public List<(ProtoId<JobPrototype> Job, int Amount)> SurvivorJobs = new()
    {
        ("CMSurvivorEngineer", 4),
        ("CMSurvivorDoctor", 3),
        ("CMSurvivorSecurity", 2),
        ("CMSurvivorCorporate", 2),
        ("CMSurvivorScientist", 2),
        ("CMSurvivor", -1),
    };

    [DataField]
    public List<ProtoId<JobPrototype>> IgnoreMaximumSurvivorJobs = new() { "RMCSurvivorCommandingOfficer" };

    [DataField]
    public Dictionary<ProtoId<JobPrototype>, List<(ProtoId<JobPrototype> Insert, int Amount)>>? SurvivorJobInserts;

    [DataField]
    public Dictionary<ProtoId<JobPrototype>, ProtoId<JobPrototype>>? SurvivorJobOverrides;

    [DataField]
    public TimeSpan AresGreetingDelay = TimeSpan.FromSeconds(5);

    [DataField]
    public SoundSpecifier AresGreetingAudio = new SoundPathSpecifier("/Audio/_RMC14/Announcements/ARES/ares_online.ogg");

    [DataField]
    public bool AresGreetingDone;

    [DataField]
    public TimeSpan AresMapDelay = TimeSpan.FromSeconds(20);

    [DataField]
    public bool AresMapDone;

    [DataField]
    public TimeSpan? StartTime;

    [DataField]
    public bool ScalingDone;

    [DataField]
    public double Scale = 1;

    [DataField]
    public double MaxScale = 1;

    [DataField]
    public TimeSpan? EndAtAllClear;

    [DataField]
    public TimeSpan AllClearEndDelay = TimeSpan.FromMinutes(3);

    [DataField]
    public ProtoId<RadioChannelPrototype> AllClearChannel = "MarineCommand";

    [DataField]
    public TimeSpan RoundEndCheckDelay = TimeSpan.FromMinutes(1);

    [DataField]
    public ResPath Thunderdome = new("/Maps/_RMC14/thunderdome.yml");

    public List<string> AuxiliaryMaps = new() {
        "/Maps/_RMC14/OCP-583.yml",
        "/Maps/_RMC14/admin_fax.yml"
    };

    [DataField]
    public ProtoId<JobPrototype> XenoSurvivorCorpseJob = "CMSurvivor";

    [DataField]
    public TimeSpan XenoSurvivorCorpseBurstDelay = TimeSpan.FromSeconds(0);

    [DataField]
    public TimeSpan? ForceEndAt;

    [DataField]
    public LocId? CustomRoundEndMessage;

    [DataField]
    public bool SpawnPlanet = true;

    [DataField]
    public bool SpawnSurvivors = true;

    [DataField]
    public bool SpawnXenos = true;

    [DataField]
    public bool DoJobSlotScaling = true;

    [DataField]
    public bool AutoEnd = true;

    [DataField]
    public bool StartARESAnnouncements = true;

    [DataField]
    public bool Bioscan = true;

    [DataField]
    public bool SetHunger = true;

    [DataField]
    public bool RequireXenoPlayers = true;

    [DataField]
    public bool QueenBoostRemoved;

    [DataField]
    public bool RecalculatedPower;
}
