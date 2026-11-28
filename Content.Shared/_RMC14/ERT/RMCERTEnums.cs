using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.ERT;

/// <summary>
/// Source that originated the ERT request.
/// </summary>
[Serializable, NetSerializable]
public enum RMCERTRequestSource : byte
{
    /// <summary>
    /// Request created from a marine communications console.
    /// </summary>
    Console,

    /// <summary>
    /// Request created from a handheld distress beacon.
    /// </summary>
    Handheld,

    /// <summary>
    /// Request created directly by an admin command or admin UI action.
    /// </summary>
    Admin,

    /// <summary>
    /// Request created by ARES automation or event logic.
    /// </summary>
    Ares,
}

/// <summary>
/// Server-side lifecycle stages for an ERT request.
/// </summary>
[Serializable, NetSerializable]
public enum RMCERTRequestState : byte
{
    /// <summary>
    /// Initial submitted state before admin review is fully materialized.
    /// </summary>
    Requested,

    /// <summary>
    /// Waiting for admin approval or denial.
    /// </summary>
    PendingAdmin,

    /// <summary>
    /// Approved and waiting to begin spawn/recruitment setup.
    /// </summary>
    PendingDispatch,

    /// <summary>
    /// Ghost roles are open for responders to join.
    /// </summary>
    Recruiting,

    /// <summary>
    /// Roster members and shuttle content are being spawned.
    /// </summary>
    Spawning,

    /// <summary>
    /// Shuttle launch has started and arrival is pending.
    /// </summary>
    Launching,

    /// <summary>
    /// Response team has arrived at its destination.
    /// </summary>
    Arrived,

    /// <summary>
    /// Request finished successfully and its active content can be cleaned up.
    /// </summary>
    Completed,

    /// <summary>
    /// Request was rejected before dispatch.
    /// </summary>
    Denied,

    /// <summary>
    /// Approved or active request was cancelled before completion.
    /// </summary>
    Cancelled,

    /// <summary>
    /// Request failed because setup, recruitment or launch could not complete.
    /// </summary>
    Failed,
}

/// <summary>
/// Shared localization helpers for ERT enums that are displayed in admin UI.
/// </summary>
public static class RMCERTLoc
{
    public static string GetSource(RMCERTRequestSource source)
    {
        var name = source.ToString();
        return Loc.TryGetString($"rmc-ert-source-{CaseConversion.PascalToKebab(name)}", out var text) ? text : name;
    }

    public static string GetState(RMCERTRequestState state)
    {
        var name = state.ToString();
        return Loc.TryGetString($"rmc-ert-state-{CaseConversion.PascalToKebab(name)}", out var text) ? text : name;
    }
}
