using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Chemistry.Generation;

[Serializable, NetSerializable]
public enum RMCResearchComputerUi : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class RMCResearchComputerBrokerClearanceBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCResearchComputerRequestXAccessBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCResearchComputerReprintContractBuiMsg : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class RMCResearchComputerTakeContractBuiMsg : BoundUserInterfaceMessage
{
    public int Index;
    public RMCResearchComputerTakeContractBuiMsg(int index) => Index = index;
}

[Serializable, NetSerializable]
public sealed class RMCResearchComputerReadDocumentBuiMsg : BoundUserInterfaceMessage
{
    public int Id;
    public RMCResearchComputerReadDocumentBuiMsg(int id) => Id = id;
}

[Serializable, NetSerializable]
public sealed class RMCResearchComputerPrintDocumentBuiMsg : BoundUserInterfaceMessage
{
    public int Id;
    public RMCResearchComputerPrintDocumentBuiMsg(int id) => Id = id;
}

[Serializable, NetSerializable]
public sealed class RMCResearchComputerPublishDocumentBuiMsg : BoundUserInterfaceMessage
{
    public int Id;
    public RMCResearchComputerPublishDocumentBuiMsg(int id) => Id = id;
}

[Serializable, NetSerializable]
public sealed class RMCResearchComputerUnpublishDocumentBuiMsg : BoundUserInterfaceMessage
{
    public int Id;
    public RMCResearchComputerUnpublishDocumentBuiMsg(int id) => Id = id;
}

[Serializable, NetSerializable]
public sealed class RMCResearchComputerAnnounceDocumentBuiMsg : BoundUserInterfaceMessage
{
    public int Id;
    public RMCResearchComputerAnnounceDocumentBuiMsg(int id) => Id = id;
}
