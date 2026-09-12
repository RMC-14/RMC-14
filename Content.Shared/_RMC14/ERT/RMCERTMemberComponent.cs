using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.ERT;

/// <summary>
/// Attached to spawned responders so the request can track and clean them up as a group.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
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
