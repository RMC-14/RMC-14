using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.ERT;

[Prototype("rmcERTCall")]
public sealed partial class RMCERTCallPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public string Name = string.Empty;

    [DataField]
    public string Organization = string.Empty;

    [DataField]
    public List<ProtoId<NpcFactionPrototype>> NpcFactions = [];

    [DataField]
    public EntProtoId<IFFFactionComponent>? IffFaction;

    [DataField]
    public string Category = "Response";

    [DataField]
    public bool Enabled = true;

    [DataField]
    public bool AdminSelectable = true;

    [DataField]
    public int RandomWeight;

    [DataField]
    public HashSet<RMCERTRequestSource> AllowedSources = [RMCERTRequestSource.Console, RMCERTRequestSource.Admin];

    [DataField]
    public ResPath? ShuttleMap;

    [DataField]
    public EntProtoId? ShuttleSpawner;

    [DataField]
    public bool AutoLaunch;

    [DataField]
    public float LaunchDelay = 10f;

    [DataField]
    public List<string> LandingTags = [];

    [DataField]
    public List<string> DeniedLandingTags = [];

    [DataField(required: true)]
    public List<RMCERTRoleEntry> Roles = [];

    [DataField]
    public RMCERTAnnouncementSet Announcements = new();

    [DataField]
    public RMCERTRequirementSet Requirements = new();

    [DataField]
    public List<string> Objectives = [];

    [DataField]
    public List<string> Features = [];
}

[DataDefinition]
public sealed partial class RMCERTRoleEntry
{
    [DataField(required: true)]
    public string Id = string.Empty;

    [DataField(required: true)]
    public string Name = string.Empty;

    [DataField]
    public ProtoId<JobPrototype>? Job;

    [DataField(required: true)]
    public EntProtoId GhostRoleEntity;

    [DataField]
    public int Min = 1;

    [DataField]
    public int Max = 1;

    [DataField]
    public bool Required = true;

    [DataField]
    public bool Leader;

    [DataField]
    public List<string> RoleTags = [];

    [DataField]
    public List<string> SeatTags = [];

    [DataField]
    public int Priority;
}

[DataDefinition]
public sealed partial class RMCERTAnnouncementSet
{
    [DataField]
    public string? RequestAdmin;

    [DataField]
    public string? Dispatch;

    [DataField]
    public string? Recruiting;

    [DataField]
    public string? Launch;

    [DataField]
    public string? Arrival;

    [DataField]
    public string? Denied;

    [DataField]
    public string? Cancelled;

    [DataField]
    public string? Failed;

    [DataField]
    public SoundSpecifier? RequestSound;

    [DataField]
    public SoundSpecifier? DispatchSound;

    [DataField]
    public SoundSpecifier? RecruitingSound;

    [DataField]
    public SoundSpecifier? LaunchSound;

    [DataField]
    public SoundSpecifier? ArrivalSound;

    [DataField]
    public SoundSpecifier? DeniedSound;

    [DataField]
    public SoundSpecifier? CancelledSound;

    [DataField]
    public SoundSpecifier? FailedSound;
}

[DataDefinition]
public sealed partial class RMCERTRequirementSet
{
    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromMinutes(10);

    [DataField]
    public TimeSpan? MinRoundTime;

    [DataField]
    public TimeSpan RecruitmentDuration = TimeSpan.FromSeconds(30);

    [DataField]
    public bool DisallowDuringEvacuation = true;

    [DataField]
    public bool DisallowDuringHijack = true;

    [DataField]
    public int MaxCallsPerRound = 1;

    [DataField]
    public int MinRequiredSlots;
}
