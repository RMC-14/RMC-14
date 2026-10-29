using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Biomass;

[Serializable, NetSerializable]
public enum RMCBiomassAnalyzerUi
{
    Key,
}

[Serializable, NetSerializable]
public sealed class RMCBiomassAnalyzerEjectOrganBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCBiomassAnalyzerProcessOrganBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCBiomassAnalyzerToggleAutoProcessBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCBiomassAnalyzerEnqueuePrintBuiMsg(EntProtoId<RMCBiomassUpgradeComponent> upgradeId, int amount) : BoundUserInterfaceMessage
{
    public readonly EntProtoId<RMCBiomassUpgradeComponent> UpgradeId = upgradeId;
    public readonly int Amount = amount;
}

[Serializable, NetSerializable]
public sealed class RMCBiomassAnalyzerToggleQueueBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCBiomassAnalyzerRemoveFromQueueBuiMsg(int index) : BoundUserInterfaceMessage
{
    public readonly int Index = index;
}
