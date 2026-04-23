using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.ERT;

[Serializable, NetSerializable]
/// <summary>
/// Source that originated the ERT request.
/// </summary>
public enum RMCERTRequestSource : byte
{
    Console,
    Handheld,
    Admin,
    Ares,
}

[Serializable, NetSerializable]
/// <summary>
/// Server-side lifecycle stages for an ERT request.
/// </summary>
public enum RMCERTRequestState : byte
{
    Requested,
    PendingAdmin,
    PendingDispatch,
    Recruiting,
    Spawning,
    Launching,
    Arrived,
    Completed,
    Denied,
    Cancelled,
    Failed,
}

/// <summary>
/// Shared localization helpers for ERT enums that are displayed in admin UI.
/// </summary>
public static class RMCERTLoc
{
    public static string GetSource(RMCERTRequestSource source)
    {
        return source switch
        {
            RMCERTRequestSource.Console => Loc.GetString("rmc-ert-source-console"),
            RMCERTRequestSource.Handheld => Loc.GetString("rmc-ert-source-handheld"),
            RMCERTRequestSource.Admin => Loc.GetString("rmc-ert-source-admin"),
            RMCERTRequestSource.Ares => Loc.GetString("rmc-ert-source-ares"),
            _ => source.ToString(),
        };
    }

    public static string GetState(RMCERTRequestState state)
    {
        return state switch
        {
            RMCERTRequestState.Requested => Loc.GetString("rmc-ert-state-requested"),
            RMCERTRequestState.PendingAdmin => Loc.GetString("rmc-ert-state-pending-admin"),
            RMCERTRequestState.PendingDispatch => Loc.GetString("rmc-ert-state-pending-dispatch"),
            RMCERTRequestState.Recruiting => Loc.GetString("rmc-ert-state-recruiting"),
            RMCERTRequestState.Spawning => Loc.GetString("rmc-ert-state-spawning"),
            RMCERTRequestState.Launching => Loc.GetString("rmc-ert-state-launching"),
            RMCERTRequestState.Arrived => Loc.GetString("rmc-ert-state-arrived"),
            RMCERTRequestState.Completed => Loc.GetString("rmc-ert-state-completed"),
            RMCERTRequestState.Denied => Loc.GetString("rmc-ert-state-denied"),
            RMCERTRequestState.Cancelled => Loc.GetString("rmc-ert-state-cancelled"),
            RMCERTRequestState.Failed => Loc.GetString("rmc-ert-state-failed"),
            _ => state.ToString(),
        };
    }
}
