namespace Content.Shared._RMC14.Dropship;

/// <summary>
/// High-level destination profile used by restricted RMC shuttle routing.
/// </summary>
public enum RMCShuttleDockingClass
{
    /// <summary>
    /// Normal shuttle profile that docks at standard internal pads.
    /// </summary>
    Standard,

    /// <summary>
    /// Compact shuttle profile allowed to use smaller side or internal pads.
    /// </summary>
    Small,

    /// <summary>
    /// Large shuttle profile that requires external hangar-class pads.
    /// </summary>
    Big,
}

public enum RMCShuttleLandingClass
{
    Internal,
    ExternalSide,
    ExternalHangar,
}

/// <summary>
/// Maps a shuttle profile to compatible landing pad classes.
/// </summary>
public static class RMCShuttleDocking
{
    private static readonly RMCShuttleLandingClass[] SmallDockClasses = [RMCShuttleLandingClass.Internal, RMCShuttleLandingClass.ExternalSide];
    private static readonly RMCShuttleLandingClass[] StandardDockClasses = [RMCShuttleLandingClass.Internal];
    private static readonly RMCShuttleLandingClass[] BigDockClasses = [RMCShuttleLandingClass.ExternalHangar];

    /// <summary>
    /// Returns the landing classes the shuttle may use for automatic routing and nav-console validation.
    /// </summary>
    public static RMCShuttleLandingClass[] GetAllowedDockClasses(RMCShuttleDockingClass dockingClass)
    {
        return dockingClass switch
        {
            RMCShuttleDockingClass.Standard => StandardDockClasses,
            RMCShuttleDockingClass.Small => SmallDockClasses,
            RMCShuttleDockingClass.Big => BigDockClasses,
            _ => [],
        };
    }
}
