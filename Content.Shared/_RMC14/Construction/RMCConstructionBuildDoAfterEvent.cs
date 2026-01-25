using Content.Shared._RMC14.Construction.Prototypes;
using Content.Shared.DoAfter;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Construction;

[Serializable, NetSerializable]
public sealed partial class RMCConstructionBuildDoAfterEvent : SimpleDoAfterEvent
{
    [DataField(required: true)]
    public RMCConstructionPrototype Prototype;

    [DataField(required: true)]
    public int Amount;

    [DataField(required: true)]
    public NetCoordinates Coordinates;

    [DataField(required: true)]
    public Direction Direction;
    public RMCConstructionBuildDoAfterEvent(RMCConstructionPrototype prototype, int amount, NetCoordinates coordinates, Direction direction)
    {
        Prototype = prototype;
        Amount = amount;
        Coordinates = coordinates;
        Direction = direction;
    }
}
