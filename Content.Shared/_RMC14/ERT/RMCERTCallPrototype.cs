using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.ERT;

[Prototype("rmcERTCall")]
/// <summary>
/// Data-driven definition of an ERT option, including roster, shuttle source, announcements and request checks.
/// </summary>
public sealed partial class RMCERTCallPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("name", required: true)]
    private LocId _name = default!;

    public string Name => Loc.GetString(_name);

    [DataField("organization")]
    private LocId? _organization;

    public string Organization => _organization == null
        ? string.Empty
        : Loc.GetString(_organization.Value);

    [DataField]
    public List<ProtoId<NpcFactionPrototype>> NpcFactions = [];

    [DataField]
    public EntProtoId<IFFFactionComponent>? IffFaction;

    [DataField("category")]
    private LocId _category = "rmc-ert-category-response";

    public string Category => Loc.GetString(_category);

    [DataField]
    public bool Enabled = true;

    [DataField]
    public bool AdminSelectable = true;

    [DataField("adminButtonLabel")]
    private LocId? _adminButtonLabel;

    public string? AdminButtonLabel => _adminButtonLabel == null
        ? null
        : Loc.GetString(_adminButtonLabel.Value);

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
    public List<LocId> Objectives = [];

    [DataField]
    public List<LocId> Features = [];
}

[DataDefinition]
/// <summary>
/// One role entry within an ERT call's potential roster.
/// </summary>
public sealed partial class RMCERTRoleEntry
{
    [DataField(required: true)]
    public string Id = string.Empty;

    [DataField("name", required: true)]
    private LocId _name = default!;

    public string Name => Loc.GetString(_name);

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
/// <summary>
/// Optional announcement and sound hooks for each stage of an ERT request lifecycle.
/// </summary>
public sealed partial class RMCERTAnnouncementSet
{
    [DataField]
    public LocId? RequestAdmin;

    [DataField]
    public LocId? Dispatch;

    [DataField]
    public LocId? Recruiting;

    [DataField]
    public LocId? Launch;

    [DataField]
    public LocId? Arrival;

    [DataField]
    public LocId? Denied;

    [DataField]
    public LocId? Cancelled;

    [DataField]
    public LocId? Failed;

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
/// <summary>
/// Request gating and recruitment limits for a single ERT call.
/// </summary>
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
