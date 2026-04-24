namespace Content.Shared._RMC14.ERT;

/// <summary>
/// High-level berth profile used to mirror the SS13 standard/small/big ERT shuttle routing model.
/// </summary>
public enum RMCERTShuttleDockingClass
{
    Standard,
    Small,
    Big,
}

/// <summary>
/// Maps the abstract ERT shuttle class to concrete landing-zone dock tags.
/// </summary>
public static class RMCERTDocking
{
    private static readonly string[] SmallDockClasses = ["internal", "external_side"];
    private static readonly string[] StandardDockClasses = ["internal"];
    private static readonly string[] BigDockClasses = ["external_hangar"];

    /// <summary>
    /// Returns the berth classes the shuttle may use for automatic routing and nav-console validation.
    /// </summary>
    public static string[] GetAllowedDockClasses(RMCERTShuttleDockingClass dockingClass)
    {
        return dockingClass switch
        {
            RMCERTShuttleDockingClass.Standard => StandardDockClasses,
            RMCERTShuttleDockingClass.Small => SmallDockClasses,
            RMCERTShuttleDockingClass.Big => BigDockClasses,
            _ => [],
        };
    }
}
