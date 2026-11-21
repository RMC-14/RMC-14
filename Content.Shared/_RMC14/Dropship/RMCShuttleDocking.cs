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

/// <summary>
/// Maps an abstract shuttle profile to concrete landing class tags.
/// </summary>
public static class RMCShuttleDocking
{
    private static readonly string[] SmallDockClasses = ["internal", "external_side"];
    private static readonly string[] StandardDockClasses = ["internal"];
    private static readonly string[] BigDockClasses = ["external_hangar"];

    /// <summary>
    /// Returns the landing classes the shuttle may use for automatic routing and nav-console validation.
    /// </summary>
    public static string[] GetAllowedDockClasses(RMCShuttleDockingClass dockingClass)
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
