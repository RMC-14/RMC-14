using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Xenos.Construction;

[Serializable, NetSerializable]
public enum XenoOrderConstructionUI : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class XenoOrderConstructionBuiMessage : BoundUserInterfaceMessage
{
    public readonly EntProtoId StructureId;

    public XenoOrderConstructionBuiMessage(EntProtoId structureId)
    {
        StructureId = structureId;
    }
}
