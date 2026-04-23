namespace Content.Shared._RMC14.ERT;

public enum RMCERTShuttleDockingClass
{
    Standard,
    Small,
    Big,
}

public static class RMCERTDocking
{
    private static readonly string[] StandardDockClasses = ["internal"];
    private static readonly string[] SmallDockClasses = ["internal", "external_side"];
    private static readonly string[] BigDockClasses = ["internal", "external_side", "external_hangar"];

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
