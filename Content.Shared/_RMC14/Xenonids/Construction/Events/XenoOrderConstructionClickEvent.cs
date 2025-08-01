using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Construction.Events;

[Serializable, NetSerializable]
public sealed class XenoOrderConstructionClickEvent : EntityEventArgs
{
    public NetCoordinates Target { get; }
    public EntProtoId StructureId { get; }

    public XenoOrderConstructionClickEvent(NetCoordinates target, EntProtoId structureId)
    {
        Target = target;
        StructureId = structureId;
    }
}
