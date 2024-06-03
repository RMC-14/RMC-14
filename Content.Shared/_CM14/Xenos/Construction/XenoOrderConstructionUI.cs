using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Xenos.Construction;

[Serializable, NetSerializable]
public enum XenoOrderConstructionUI : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class XenoOrderConstructionBuiMsg(EntProtoId structureId) : BoundUserInterfaceMessage
{
    public readonly EntProtoId StructureId = structureId;
}
