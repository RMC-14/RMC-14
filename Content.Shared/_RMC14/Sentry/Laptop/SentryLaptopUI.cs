using Content.Shared._RMC14.Sentry;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Sentry.Laptop;

[Serializable, NetSerializable]
public sealed class SentryLaptopBuiState : BoundUserInterfaceState
{
    public readonly List<SentryInfo> Sentries;
    public readonly List<string> AllFactions;
    public readonly Dictionary<string, string> FactionNames;

    public SentryLaptopBuiState(
        List<SentryInfo> sentries,
        List<string> allFactions,
        Dictionary<string, string> factionNames)
    {
        Sentries = sentries;
        AllFactions = allFactions;
        FactionNames = factionNames;
    }
}

[Serializable, NetSerializable]
public sealed class SentryLaptopUnlinkBuiMsg(NetEntity sentry) : BoundUserInterfaceMessage
{
    public readonly NetEntity Sentry = sentry;
}

[Serializable, NetSerializable]
public sealed class SentryLaptopUnlinkAllBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class SentryLaptopViewBuiMsg(NetEntity sentry) : BoundUserInterfaceMessage
{
    public readonly NetEntity Sentry = sentry;
}
