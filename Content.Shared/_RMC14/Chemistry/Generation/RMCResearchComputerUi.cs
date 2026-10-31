using Content.Shared._RMC14.Dialog;
using Content.Shared.Paper;
using Robust.Shared.Prototypes;
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

[Serializable, NetSerializable]
public sealed class RMCResearchComputerDocumentContentBuiMsg : BoundUserInterfaceMessage
{
    public EntProtoId Prototype;
    public string Content;
    public List<StampDisplayInfo> StampedBy;
    public Color Color;
    public Color TextColor;
    public int Thickness;

    public RMCResearchComputerDocumentContentBuiMsg(
        EntProtoId prototype,
        string content,
        List<StampDisplayInfo> stampedBy,
        Color color,
        Color textColor,
        int thickness)
    {
        Prototype = prototype;
        Content = content;
        StampedBy = stampedBy;
        Color = color;
        TextColor = textColor;
        Thickness = thickness;
    }
}

[Serializable, NetSerializable]
public sealed record RMCResearchComputerScanConfirmEvent(NetEntity User, NetEntity Paper);

[Serializable, NetSerializable]
public sealed record RMCResearchComputerScanExistingCategoryEvent(NetEntity User, NetEntity Paper);

[Serializable, NetSerializable]
public sealed record RMCResearchComputerScanNewCategoryEvent(NetEntity User, NetEntity Paper);

[Serializable, NetSerializable]
public sealed record RMCResearchComputerScanCategoryEvent(NetEntity User, NetEntity Paper, string Category);

[Serializable, NetSerializable]
public sealed record RMCResearchComputerScanCategoryInputEvent(NetEntity User, NetEntity Paper, string Message = "") : DialogInputEvent(Message);

[Serializable, NetSerializable]
public sealed record RMCResearchComputerAnnounceInputEvent(NetEntity User, int Id, string Message = "") : DialogInputEvent(Message);
