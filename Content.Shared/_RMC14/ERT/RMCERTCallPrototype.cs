using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.NPC.Prototypes;
using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.ERT;

/// <summary>
/// Data-driven definition of an ERT option, including roster, shuttle source, announcements and request checks.
/// </summary>
[Prototype("rmcERTCall")]
public sealed partial class RMCERTCallPrototype : IPrototype
{
    /// <summary>
    /// Prototype id referenced by requests, admin messages, beacons and other ERT configuration.
    /// </summary>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Localization id for the response team name shown in admin UI and responder briefings.
    /// </summary>
    [DataField("name", required: true)]
    private LocId _name = default!;

    /// <summary>
    /// Localized display name for this ERT call.
    /// </summary>
    public string Name => Loc.GetString(_name);

    /// <summary>
    /// Optional localization id for the organization responsible for this response team.
    /// </summary>
    [DataField("organization")]
    private LocId? _organization;

    /// <summary>
    /// Localized organization label, or an empty string when this call has no organization.
    /// </summary>
    public string Organization => _organization == null
        ? string.Empty
        : Loc.GetString(_organization.Value);

    /// <summary>
    /// NPC factions assigned to responders spawned for this call.
    /// </summary>
    [DataField]
    public List<ProtoId<NpcFactionPrototype>> NpcFactions = [];

    /// <summary>
    /// Optional IFF faction applied to spawned responders and the staged shuttle.
    /// </summary>
    [DataField]
    public EntProtoId<IFFFactionComponent>? IffFaction;

    /// <summary>
    /// Localization id for the admin UI category this call is grouped under.
    /// </summary>
    [DataField("category")]
    private LocId _category = "rmc-ert-category-response";

    /// <summary>
    /// Localized admin UI category label for this call.
    /// </summary>
    public string Category => Loc.GetString(_category);

    /// <summary>
    /// Whether the call can currently be selected or randomly chosen.
    /// </summary>
    [DataField]
    public bool Enabled = true;

    /// <summary>
    /// Whether admins can select this call directly from the dispatch UI or force-call command.
    /// </summary>
    [DataField]
    public bool AdminSelectable = true;

    /// <summary>
    /// Optional localization id overriding the force-call button label in the admin UI.
    /// </summary>
    [DataField("adminButtonLabel")]
    private LocId? _adminButtonLabel;

    /// <summary>
    /// Localized force-call button label, or null to use the call name.
    /// </summary>
    public string? AdminButtonLabel => _adminButtonLabel == null
        ? null
        : Loc.GetString(_adminButtonLabel.Value);

    /// <summary>
    /// Weighted-random selection weight when admins approve a request without choosing a specific call.
    /// </summary>
    [DataField]
    public int RandomWeight;

    /// <summary>
    /// Request sources that are allowed to create this call.
    /// </summary>
    [DataField]
    public HashSet<RMCERTRequestSource> AllowedSources = [RMCERTRequestSource.Console, RMCERTRequestSource.Admin];

    /// <summary>
    /// Shuttle map loaded for the response team before launch.
    /// </summary>
    [DataField]
    public ResPath? ShuttleMap;

    /// <summary>
    /// Optional mapped grid spawner used to place the shuttle on a physical start pad.
    /// </summary>
    [DataField]
    public EntProtoId? ShuttleSpawner;

    /// <summary>
    /// Optional start pad or spawner marker used to choose where the shuttle is staged.
    /// </summary>
    [DataField]
    public EntProtoId? ShuttleSpawnMarker;

    /// <summary>
    /// Cargo entity prototypes spawned onto the shuttle before recruitment or launch.
    /// </summary>
    [DataField]
    public List<EntProtoId> ShuttleCargo = [];

    /// <summary>
    /// Number of cargo entries to spawn from <see cref="ShuttleCargo"/>.
    /// </summary>
    [DataField]
    public int ShuttleCargoCount;

    /// <summary>
    /// Whether the request should launch automatically after recruitment completes.
    /// </summary>
    [DataField]
    public bool AutoLaunch;

    /// <summary>
    /// Delay in seconds before an automatic launch attempt is made.
    /// </summary>
    [DataField]
    public float LaunchDelay = 10f;

    /// <summary>
    /// Optional FTL travel time override used for this call's shuttle.
    /// </summary>
    [DataField]
    public TimeSpan? ShuttleTravelTime;

    /// <summary>
    /// Landing destination tags this call's shuttle is allowed to use.
    /// </summary>
    [DataField]
    public List<string> LandingTags = [];

    /// <summary>
    /// Landing destination tags this call's shuttle is forbidden from using.
    /// </summary>
    [DataField]
    public List<string> DeniedLandingTags = [];

    /// <summary>
    /// Role definitions used to build the ghost-role recruitment roster.
    /// </summary>
    [DataField]
    public List<RMCERTRoleEntry> Roles = [];

    /// <summary>
    /// Optional announcement hooks for request, dispatch, arrival and failure states.
    /// </summary>
    [DataField]
    public RMCERTAnnouncementSet Announcements = new();

    /// <summary>
    /// Round-state and recruitment requirements that gate this call.
    /// </summary>
    [DataField]
    public RMCERTRequirementSet Requirements = new();

    /// <summary>
    /// Localization ids listed as responder objectives in the ghost-role briefing.
    /// </summary>
    [DataField]
    public List<LocId> Objectives = [];

    /// <summary>
    /// Localization ids listed as notable team features in the ghost-role briefing.
    /// </summary>
    [DataField]
    public List<LocId> Features = [];
}

/// <summary>
/// One role entry within an ERT call's potential roster.
/// </summary>
[DataDefinition]
public sealed partial class RMCERTRoleEntry
{
    /// <summary>
    /// Stable role id used for roster tracking and member metadata.
    /// </summary>
    [DataField(required: true)]
    public string Id = string.Empty;

    /// <summary>
    /// Localization id for the role name shown to admins and responders.
    /// </summary>
    [DataField("name", required: true)]
    private LocId _name = default!;

    /// <summary>
    /// Localized display name for this roster role.
    /// </summary>
    public string Name => Loc.GetString(_name);

    /// <summary>
    /// Optional job prototype associated with the spawned responder.
    /// </summary>
    [DataField]
    public ProtoId<JobPrototype>? Job;

    /// <summary>
    /// Weighted entity pool used to choose the ghost-role spawner for this role.
    /// </summary>
    [DataField(required: true)]
    public List<RMCERTWeightedGhostRoleEntity> GhostRoleEntityPool = [];

    /// <summary>
    /// Minimum number of slots this role contributes to the planned roster.
    /// </summary>
    [DataField]
    public int Min = 1;

    /// <summary>
    /// Maximum number of slots this role can contribute to the planned roster.
    /// </summary>
    [DataField]
    public int Max = 1;

    /// <summary>
    /// Whether the minimum slots for this role must be filled for the call to launch.
    /// </summary>
    [DataField]
    public bool Required = true;

    /// <summary>
    /// Whether this role should be treated as the team leader in briefing and metadata.
    /// </summary>
    [DataField]
    public bool Leader;

    /// <summary>
    /// Tags used to match this role to spawn markers and reserved seats.
    /// </summary>
    [DataField]
    public List<string> RoleTags = [];

    /// <summary>
    /// Preferred seat tags used when assigning this role to a shuttle seat.
    /// </summary>
    [DataField]
    public List<string> SeatTags = [];

    /// <summary>
    /// Higher-priority roles are planned and seated before lower-priority roles.
    /// </summary>
    [DataField]
    public int Priority;
}

/// <summary>
/// Weighted ghost-role entity choice within an ERT role entry.
/// </summary>
[DataDefinition]
public sealed partial class RMCERTWeightedGhostRoleEntity
{
    /// <summary>
    /// Ghost-role entity prototype that may be spawned for a roster slot.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId Entity;

    /// <summary>
    /// Relative selection weight for this entity within the role's pool.
    /// </summary>
    [DataField]
    public int Weight = 1;
}

/// <summary>
/// Optional announcement and sound hooks for each stage of an ERT request lifecycle.
/// </summary>
[DataDefinition]
public sealed partial class RMCERTAnnouncementSet
{
    /// <summary>
    /// Optional marine-facing announcement played when a request is created.
    /// Falls back to the default distress beacon announcement when unset.
    /// </summary>
    [DataField]
    public RMCERTStageAnnouncement? Request;

    /// <summary>
    /// Optional admin-facing announcement text shown when a request is created.
    /// </summary>
    [DataField]
    public LocId? RequestAdmin;

    /// <summary>
    /// Announcement played when a call is approved and prepared for dispatch.
    /// </summary>
    [DataField]
    public RMCERTStageAnnouncement? Dispatch;

    /// <summary>
    /// Announcement played when the response team arrives at its destination.
    /// </summary>
    [DataField]
    public RMCERTStageAnnouncement? Arrival;

    /// <summary>
    /// Announcement played when the request is denied.
    /// </summary>
    [DataField]
    public RMCERTStageAnnouncement? Denied;

    /// <summary>
    /// Announcement played when an in-progress request is cancelled.
    /// </summary>
    [DataField]
    public RMCERTStageAnnouncement? Cancelled;

    /// <summary>
    /// Announcement played when the request fails before completion.
    /// </summary>
    [DataField]
    public RMCERTStageAnnouncement? Failed;
}

/// <summary>
/// Marine-facing announcement data for one public ERT lifecycle stage.
/// </summary>
[DataDefinition]
public sealed partial class RMCERTStageAnnouncement
{
    /// <summary>
    /// Optional localization id for the announcement title.
    /// </summary>
    [DataField]
    public LocId? Title;

    /// <summary>
    /// Optional localization id for the announcement body.
    /// </summary>
    [DataField]
    public LocId? Message;

    /// <summary>
    /// Optional pool of announcement body localization ids, selected randomly when present.
    /// </summary>
    [DataField]
    public List<LocId> Messages = [];

    /// <summary>
    /// Optional sound played with the announcement.
    /// </summary>
    [DataField]
    public SoundSpecifier? Sound;

    /// <summary>
    /// Whether <see cref="Title"/> is passed through as a raw localized title instead of a formatted call string.
    /// </summary>
    [DataField]
    public bool RawTitle = true;
}

/// <summary>
/// Request gating and recruitment limits for a single ERT call.
/// </summary>
[DataDefinition]
public sealed partial class RMCERTRequirementSet
{
    /// <summary>
    /// Per-call cooldown applied between successful requests of this call.
    /// </summary>
    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Optional minimum round time required before this call can be approved.
    /// </summary>
    [DataField]
    public TimeSpan? MinRoundTime;

    /// <summary>
    /// Time ghost roles remain open before automatic launch or manual dispatch.
    /// </summary>
    [DataField]
    public TimeSpan RecruitmentDuration = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Whether this call is blocked while evacuation is active.
    /// </summary>
    [DataField]
    public bool DisallowDuringEvacuation = true;

    /// <summary>
    /// Whether this call is blocked while hijack is active.
    /// </summary>
    [DataField]
    public bool DisallowDuringHijack = true;

    /// <summary>
    /// Maximum number of non-terminal requests for this call allowed in one round.
    /// </summary>
    [DataField]
    public int MaxCallsPerRound = 1;

    /// <summary>
    /// Minimum number of recruited required slots needed before launch.
    /// </summary>
    [DataField]
    public int MinRequiredSlots;

    /// <summary>
    /// Maximum total roster slots that can be planned for this call.
    /// </summary>
    [DataField]
    public int MaxSlots;
}
