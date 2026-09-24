using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Construction;

[Serializable, NetSerializable]
public enum XenoChooseStructureUI : byte
{
    Key
}

[Serializable, NetSerializable]
public sealed class XenoChooseStructureBuiState(bool showNodeCount, int nodeCount, int nodeMax) : BoundUserInterfaceState
{
    public readonly bool ShowNodeCount = showNodeCount;
    public readonly int NodeCount = nodeCount;
    public readonly int NodeMax = nodeMax;
}

[Serializable, NetSerializable]
public sealed class XenoChooseStructureBuiMsg(EntProtoId structureId) : BoundUserInterfaceMessage
{
    public readonly EntProtoId StructureId = structureId;
}
