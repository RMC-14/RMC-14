using System.Numerics;
using Robust.Shared.GameStates;
using Content.Shared._RMC14.Weapons.Ranged.IFF;
using Content.Shared.NPC.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.ERT;

[RegisterComponent]
/// <summary>
/// Handheld distress beacon settings that control which specific ERT calls it can request.
/// </summary>
public sealed partial class RMCERTDistressBeaconComponent : Component
{
    /// <summary>
    /// Explicit ERT call prototypes this beacon may request; empty means all enabled handheld calls.
    /// </summary>
    [DataField]
    public List<ProtoId<RMCERTCallPrototype>> AllowedCalls = [];

    // This is only used server-side when building prompts and admin text.
    // The localized key itself does not need to go over component state.
    /// <summary>
    /// Localization id for the title shown in the reason prompt and admin request text.
    /// </summary>
    [DataField("requestTitle")]
    private LocId _requestTitle = "rmc-ert-beacon-request-title-handheld";

    /// <summary>
    /// Localized title used when this beacon opens a distress request prompt.
    /// </summary>
    public string RequestTitle => Loc.GetString(_requestTitle);

    /// <summary>
    /// Localization id for the authority or organization receiving this beacon request.
    /// </summary>
    [DataField("recipient")]
    private LocId _recipient = "rmc-ert-recipient-high-command";

    /// <summary>
    /// Localized recipient label used in request prompts and admin text.
    /// </summary>
    public string Recipient => Loc.GetString(_recipient);

    /// <summary>
    /// Whether the user must provide a non-empty reason before sending the request.
    /// </summary>
    [DataField]
    public bool ReasonRequired = true;

    /// <summary>
    /// Whether the beacon becomes spent after a successful request.
    /// </summary>
    [DataField]
    public bool SingleUse = true;

    /// <summary>
    /// Whether a denied request clears the spent/cooldown state so the beacon can be reused.
    /// </summary>
    [DataField]
    public bool ResetOnDeny = true;

    /// <summary>
    /// Time before this beacon can send another request.
    /// </summary>
    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Round time when this beacon last sent a request.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? LastUsed;

    /// <summary>
    /// Whether this beacon has already consumed its single-use request.
    /// </summary>
    [DataField]
    public bool Spent;

    /// <summary>
    /// Maximum number of characters accepted for the request reason.
    /// </summary>
    [DataField]
    public int ReasonLimit = 200;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
/// <summary>
/// Marks a shuttle grid as belonging to an active ERT request and carries routing metadata onto the shuttle.
/// </summary>
public sealed partial class RMCERTShuttleComponent : Component
{
    /// <summary>
    /// Request that owns this shuttle until arrival, return or cleanup.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Guid RequestId;

    /// <summary>
    /// Prototype id of the selected ERT call that spawned or configured this shuttle.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? Call;

    /// <summary>
    /// Localized organization label copied from the selected call.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string? Organization;

    /// <summary>
    /// NPC factions assigned to responders and propagated to the shuttle for routing context.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<ProtoId<NpcFactionPrototype>> NpcFactions = [];

    /// <summary>
    /// Optional IFF faction associated with this shuttle's responders.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId<IFFFactionComponent>? IffFaction;

    /// <summary>
    /// Landing destination tags this shuttle may use while under ERT control.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<string> LandingTags = [];

    /// <summary>
    /// Whether normal hijack behavior is blocked for this ERT shuttle.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool NoHijack = true;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
/// <summary>
/// Seat metadata used by ERT spawning to reserve specialist seats before launch.
/// </summary>
public sealed partial class RMCERTSeatComponent : Component
{
    /// <summary>
    /// Seat tags describing which roster roles can prefer this seat.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<string> SeatTags = [];

    /// <summary>
    /// Role tags that should reserve this seat before generic assignment.
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<string> ReservedRoleTags = [];

    /// <summary>
    /// Higher-priority seats are considered before lower-priority seats.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Priority;

    /// <summary>
    /// Responder currently assigned to this seat.
    /// </summary>
    [DataField, AutoNetworkedField]
    public NetEntity? OccupiedBy;

    /// <summary>
    /// Round time when a temporary seat reservation expires.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    public TimeSpan? ReservationExpires;
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
/// <summary>
/// Attached to spawned responders so the request can track and clean them up as a group.
/// </summary>
public sealed partial class RMCERTMemberComponent : Component
{
    /// <summary>
    /// Request that spawned and owns this responder.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Guid RequestId;

    /// <summary>
    /// Prototype id of the ERT call this member belongs to.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string Call = string.Empty;

    /// <summary>
    /// Roster role id assigned to this member.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string Role = string.Empty;

    /// <summary>
    /// Localized team or organization label shown in responder context.
    /// </summary>
    [DataField, AutoNetworkedField]
    public string Team = string.Empty;
}

[RegisterComponent]
/// <summary>
/// Map marker used as a physical staging pad for loading an ERT shuttle onto the request source map.
/// </summary>
public sealed partial class RMCERTShuttleStartPadComponent : Component
{
    /// <summary>
    /// Local offset from the marker used when placing a loaded shuttle grid.
    /// </summary>
    [DataField]
    public Vector2 Offset;
}

[RegisterComponent]
/// <summary>
/// Spawn marker metadata used to place responders on the shuttle before seat assignment.
/// </summary>
public sealed partial class RMCERTSpawnPointComponent : Component
{
    /// <summary>
    /// Role tags this spawn point is best suited for.
    /// </summary>
    [DataField]
    public List<string> RoleTags = [];

    /// <summary>
    /// Seat tags copied to seat assignment when a responder spawns here.
    /// </summary>
    [DataField]
    public List<string> SeatTags = [];

    /// <summary>
    /// Higher-priority spawn points are considered before lower-priority points.
    /// </summary>
    [DataField]
    public int Priority;
}
