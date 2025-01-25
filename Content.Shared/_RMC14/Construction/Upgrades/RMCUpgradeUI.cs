using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Construction.Upgrades;

[Serializable, NetSerializable]
public enum RMCConstructionUpgradeUiKey
{
    Key,
}

[Serializable, NetSerializable]
public sealed class RMCConstructionUpgradeBuiMsg(EntProtoId upgrade) : BoundUserInterfaceMessage
{
    public readonly EntProtoId Upgrade = upgrade;
}
