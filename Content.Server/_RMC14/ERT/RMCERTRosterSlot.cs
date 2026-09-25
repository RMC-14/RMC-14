using System;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.ERT;

/// <summary>
/// Materialized roster entry created from a call prototype before ghost-role recruitment starts.
/// </summary>
[Serializable]
public sealed class RMCERTRosterSlot
{
    /// <summary>
    /// Role id copied from the call prototype entry.
    /// </summary>
    public string RoleId = string.Empty;

    /// <summary>
    /// Localized role name shown to admins and responders.
    /// </summary>
    public string RoleName = string.Empty;

    /// <summary>
    /// Ghost-role entity prototype chosen for this slot.
    /// </summary>
    public EntProtoId GhostRoleEntity;

    /// <summary>
    /// Whether this slot represents the response team leader.
    /// </summary>
    public bool Leader;

    /// <summary>
    /// Higher-priority slots are spawned and seated first.
    /// </summary>
    public int Priority;

    /// <summary>
    /// Role tags used to choose spawn markers and reserved seats.
    /// </summary>
    public List<string> RoleTags = [];

    /// <summary>
    /// Preferred seat tags used when assigning this responder to the shuttle.
    /// </summary>
    public List<string> SeatTags = [];
}
