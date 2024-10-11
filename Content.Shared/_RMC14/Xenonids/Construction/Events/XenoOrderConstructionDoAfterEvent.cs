using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Construction.Events;

[Serializable, NetSerializable]
public sealed partial class XenoOrderConstructionDoAfterEvent : SimpleDoAfterEvent
{
    [DataField]
    public EntProtoId StructureId = "HiveCoreXeno";

    [DataField]
    public NetCoordinates Coordinates;

    public XenoOrderConstructionDoAfterEvent(EntProtoId structureId, NetCoordinates coordinates)
    {
        StructureId = structureId;
        Coordinates = coordinates;
    }
}
