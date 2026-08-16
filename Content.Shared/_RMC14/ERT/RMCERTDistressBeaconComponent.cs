using Robust.Shared.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.ERT;

/// <summary>
/// Handheld distress beacon settings that control which specific ERT calls it can request.
/// </summary>
[RegisterComponent, NetworkedComponent]
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
