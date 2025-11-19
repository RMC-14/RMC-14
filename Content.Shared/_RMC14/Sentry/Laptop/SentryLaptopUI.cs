using Content.Shared._RMC14.Sentry;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Sentry.Laptop;

[Serializable, NetSerializable]
public sealed class SentryLaptopBuiState(List<SentryInfo> sentries) : BoundUserInterfaceState
{
    public readonly List<SentryInfo> Sentries = sentries;
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
