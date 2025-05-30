using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Construction;

[Serializable, NetSerializable]
public sealed partial class RMCConstructionBuildDoAfterEvent : SimpleDoAfterEvent
{
    [DataField(required: true)]
    public EntProtoId Prototype;

    [DataField(required: true)]
    public int Amount;

    [DataField(required: true)]
    public int MaterialCost;

    [DataField(required: true)]
    public NetCoordinates Coordinates;

    [DataField(required: true)]
    public Direction Direction;

    public RMCConstructionBuildDoAfterEvent(EntProtoId prototype, int amount, int materialCost, NetCoordinates coordinates, Direction direction)
    {
        Prototype = prototype;
        Amount = amount;
        MaterialCost = materialCost;
        Coordinates = coordinates;
        Direction = direction;
    }
}
