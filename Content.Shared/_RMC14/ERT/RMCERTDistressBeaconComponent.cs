using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
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
    public List<EntProtoId<RMCERTCallComponent>> AllowedCalls = [];

    /// <summary>
    /// Optional automatic response policy. Null requires an administrator's decision.
    /// This setting is only used by the server.
    /// </summary>
    [DataField]
    public RMCERTAutoResolutionOptions? AutoResolution;

    [DataField]
    public string Recipient = "High Command";

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
