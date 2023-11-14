using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Xenos.Construction;

[Serializable, NetSerializable]
public enum XenoChooseStructureUI : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class XenoChooseStructureBuiMessage : BoundUserInterfaceMessage
{
    public readonly EntProtoId StructureId;

    public XenoChooseStructureBuiMessage(EntProtoId structureId)
    {
        StructureId = structureId;
    }
}
