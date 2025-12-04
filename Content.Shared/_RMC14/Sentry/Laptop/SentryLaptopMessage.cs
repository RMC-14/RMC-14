using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Sentry.Laptop;

[Serializable, NetSerializable]
public record SentryInfo(
    NetEntity Id,
    string Name,
    SentryMode Mode,
    float Health,
    float MaxHealth,
    int Ammo,
    int MaxAmmo,
    string Location,
    NetEntity? Target,
    HashSet<string> FriendlyFactions,
    string? CustomName = null,
    float VisionRadius = 5.0f,
    float MaxDeviation = 75.0f,
    HashSet<string>? HumanoidAdded = null
);

[Serializable, NetSerializable]
public sealed class SentryLaptopSetFactionsBuiMsg(NetEntity sentry, List<string> factions) : BoundUserInterfaceMessage
{
    public NetEntity Sentry = sentry;
    public List<string> Factions = factions;
}

[Serializable, NetSerializable]
public sealed class SentryLaptopToggleFactionBuiMsg(NetEntity sentry, string faction, bool targeted) : BoundUserInterfaceMessage
{
    public NetEntity Sentry = sentry;
    public string Faction = faction;
    public bool Targeted = targeted;
}

[Serializable, NetSerializable]
public sealed class SentryLaptopResetTargetingBuiMsg(NetEntity sentry) : BoundUserInterfaceMessage
{
    public NetEntity Sentry = sentry;
}

[Serializable, NetSerializable]
public sealed class SentryLaptopTogglePowerBuiMsg(NetEntity sentry) : BoundUserInterfaceMessage
{
    public NetEntity Sentry = sentry;
}

[Serializable, NetSerializable]
public sealed class SentryLaptopViewCameraBuiMsg(NetEntity sentry) : BoundUserInterfaceMessage
{
    public NetEntity Sentry = sentry;
}

[Serializable, NetSerializable]
public sealed class SentryLaptopCloseCameraBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class SentryLaptopSetNameBuiMsg(NetEntity sentry, string name) : BoundUserInterfaceMessage
{
    public NetEntity Sentry = sentry;
    public string Name = name;
}

[Serializable, NetSerializable]
public sealed class SentryLaptopGlobalToggleFactionBuiMsg(string faction, bool targeted) : BoundUserInterfaceMessage
{
    public string Faction = faction;
    public bool Targeted = targeted;
}

[Serializable, NetSerializable]
public sealed class SentryLaptopGlobalResetTargetingBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class SentryLaptopGlobalTogglePowerBuiMsg(bool powerOn) : BoundUserInterfaceMessage
{
    public bool PowerOn = powerOn;
}

[Serializable, NetSerializable]
public sealed class SentryLaptopGlobalSetFactionsBuiMsg(List<string> factions) : BoundUserInterfaceMessage
{
    public List<string> Factions = factions;
}

[Serializable, NetSerializable]
public sealed class SentryAlertEvent : BoundUserInterfaceMessage
{
    public NetEntity Sentry { get; }
    public SentryAlertType AlertType { get; }
    public string Message { get; }
    public string Color { get; }
    public int FontSize { get; }

    public SentryAlertEvent(NetEntity sentry, SentryAlertType alertType, string message, string color, int fontSize)
    {
        Sentry = sentry;
        AlertType = alertType;
        Message = message;
        Color = color;
        FontSize = fontSize;
    }
}

[Serializable, NetSerializable]
public enum SentryAlertType
{
    LowAmmo,
    CriticalHealth,
    TargetAcquired,
    Damaged
}

[Serializable, NetSerializable]

public static class SentryFactions
{
    public static List<string> AllFactions = new();
    public static Dictionary<string, string> FactionNames = new();
}
