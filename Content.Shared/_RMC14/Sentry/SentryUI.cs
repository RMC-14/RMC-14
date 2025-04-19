using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Sentry;

[Serializable, NetSerializable]
public enum SentryUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class SentryUpgradeBuiMsg(EntProtoId upgrade) : BoundUserInterfaceMessage
{
    public readonly EntProtoId Upgrade = upgrade;
}
