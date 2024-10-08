using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Construction.Events;

[Serializable, NetSerializable]
public sealed partial class XenoSecreteStructureDoAfterEvent : SimpleDoAfterEvent
{
    [DataField]
    public NetCoordinates Coordinates;

    [DataField]
    public EntProtoId StructureId = "WallXenoResin";

    [DataField]
    public NetEntity? Effect;

    public XenoSecreteStructureDoAfterEvent(NetCoordinates coordinates, EntProtoId structureId, NetEntity? effect = null)
    {
        Coordinates = coordinates;
        StructureId = structureId;
        Effect = effect;
    }
}
