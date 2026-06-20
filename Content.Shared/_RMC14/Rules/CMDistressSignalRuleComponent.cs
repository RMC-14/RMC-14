using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.Damage;
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
    public bool ScuttleUnlocked;

    [DataField]
    public bool ScuttleDetonated;

    [DataField]
    public bool ScuttleFinalSequenceStarted;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? ScuttleUnlockAt;

    [DataField]
    public TimeSpan ScuttleProgress;

    [DataField]
    public bool ScuttleOneThirdAnnounced;

    [DataField]
    public bool ScuttleHalfwayAnnounced;

    [DataField]
    public bool ScuttleTwoThirdsAnnounced;

    [DataField]
    public bool ScuttleFirstOverloadAnnounced;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? ScuttleFinalDetonateAt;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? ScuttleRoundEndAt;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? ScuttleFinalStartedAt;

    [DataField]
    public bool ScuttleFinalMeltdownAnnounced;

    [DataField]
    public bool ScuttleFinalNuclearSoundPlayed;

    [DataField]
    public bool ScuttleFinalCinematicStarted;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan? ScuttleNextHeatPulseAt;

    [DataField]
    public TimeSpan ScuttleUnlockDelay = TimeSpan.FromSeconds(15);

    [DataField]
    public TimeSpan ScuttleMinDuration = TimeSpan.FromMinutes(5);

    [DataField]
    public TimeSpan ScuttleMaxDuration = TimeSpan.FromMinutes(15);

    [DataField]
    public int ScuttleTotalReactors;

    [DataField]
    public TimeSpan ScuttleFinalMeltdownDelay = TimeSpan.FromSeconds(7);

    [DataField]
    public TimeSpan ScuttleFinalNuclearSoundDelay = TimeSpan.FromSeconds(12);

    [DataField]
    public TimeSpan ScuttleFinalCinematicDelay = TimeSpan.FromSeconds(22);

    [DataField]
    public TimeSpan ScuttleFinalSequenceDelay = TimeSpan.FromSeconds(15);

    [DataField]
    public TimeSpan ScuttleHeatPulseEvery = TimeSpan.FromSeconds(6);

    [DataField]
    public int ScuttleStageFireRange = 1;

    [DataField]
    public int ScuttleStageFireIntensity = 8;

    [DataField]
    public int ScuttleStageFireDuration = 18;

    [DataField]
    public int ScuttleFinalFireRange = 2;

    [DataField]
    public int ScuttleFinalFireIntensity = 12;

    [DataField]
    public int ScuttleFinalFireDuration = 35;

    [DataField]
    public int ScuttleStageShakeIntensity = 4;

    [DataField]
    public int ScuttleStageShakeDuration = 2;

    [DataField]
    public int ScuttleMeltdownShakeIntensity = 4;

    [DataField]
    public int ScuttleMeltdownShakeDuration = 20;

    [DataField]
    public int ScuttleNuclearShakeIntensity = 4;

    [DataField]
    public int ScuttleNuclearShakeDuration = 110;

    [DataField]
    public float ScuttleHeatRadius = 3.5f;

    [DataField]
    public float ScuttleSuperheatRadius = 5f;

    [DataField]
    public float ScuttleHeatJoules = 45000f;

    [DataField]
    public float ScuttleSuperheatJoules = 75000f;

    [DataField]
    public EntProtoId ScuttleFire = "RMCTileFire";

    [DataField]
    public DamageSpecifier ScuttleHeatDamage = new() { DamageDict = { { "Heat", 5 } } };

    [DataField]
    public DamageSpecifier ScuttleSuperheatDamage = new() { DamageDict = { { "Heat", 10 } } };

    [DataField]
    public SoundSpecifier ScuttleStageSound = new SoundPathSpecifier("/Audio/Machines/warning_buzzer.ogg");

    [DataField]
    public SoundSpecifier ScuttleDetonationSound = new SoundPathSpecifier("/Audio/Effects/explosionfar.ogg");

    [DataField]
    public SoundSpecifier ScuttleNoticeSound = new SoundPathSpecifier("/Audio/_RMC14/Announcements/Marine/notice2.ogg");

    [DataField]
    public SoundSpecifier ScuttleRumbleSound = new SoundPathSpecifier(
        "/Audio/Magic/rumble.ogg",
        AudioParams.Default.WithVolume(3f));

    [DataField]
    public List<SoundSpecifier> ScuttleCreakSounds = new()
    {
        new SoundPathSpecifier("/Audio/_RMC14/Scuttle/creak1.ogg"),
        new SoundPathSpecifier("/Audio/_RMC14/Scuttle/creak2.ogg"),
        new SoundPathSpecifier("/Audio/_RMC14/Scuttle/creak3.ogg"),
    };

    [DataField]
    public List<SoundSpecifier> ScuttleNuclearDetonationSounds = new()
    {
        new SoundPathSpecifier("/Audio/_RMC14/Scuttle/nuclear_detonation1.ogg", AudioParams.Default.WithVolume(4f)),
        new SoundPathSpecifier("/Audio/_RMC14/Scuttle/nuclear_detonation2.ogg", AudioParams.Default.WithVolume(4f)),
    };

    [DataField]
    public bool MarinesLanded;

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

    [DataField]
    public SoundSpecifier SelfDestructAudio = new SoundCollectionSpecifier("RMCSelfDestruct");

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
    public Dictionary<ProtoId<JobPrototype>, List<(ProtoId<JobPrototype> Variant, int Amount)>>? SurvivorJobVariants;

    [DataField]
    public Dictionary<ProtoId<JobPrototype>, ProtoId<JobPrototype>>? SurvivorJobOverrides;

    [DataField]
    public Dictionary<ProtoId<JobPrototype>, List<(ProtoId<JobPrototype> Special, int Amount)>>? SurvivorJobVariantScenarios;

    [DataField]
    public TimeSpan AresGreetingDelay = TimeSpan.FromSeconds(5);

    [DataField]
    public SoundSpecifier AresGreetingAudio = new SoundPathSpecifier("/Audio/_RMC14/Announcements/ARES/ares_online.ogg");

    [DataField]
    public bool AresGreetingDone;

    [DataField]
    public bool AresPreflightDone;

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
        "/Maps/_RMC14/admin_fax.yml"
    };

    [DataField]
    public ProtoId<JobPrototype> XenoSurvivorCorpseJob = "CMSurvivorHost";

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
    public bool AutoEnd = false;

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
