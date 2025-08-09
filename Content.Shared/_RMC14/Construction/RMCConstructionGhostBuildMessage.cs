using Content.Shared._RMC14.Construction.Prototypes;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Construction;

[Serializable, NetSerializable]
public sealed class RMCConstructionGhostBuildMessage : EntityEventArgs
{
    public ProtoId<RMCConstructionPrototype> Prototype { get; }
    public int Amount { get; }
    public NetCoordinates Coordinates { get; }
    public Direction Direction { get; }
    public int GhostId { get; }

    public RMCConstructionGhostBuildMessage(ProtoId<RMCConstructionPrototype> prototype,
        int amount, NetCoordinates coordinates, Direction direction, int ghostId)
    {
        Prototype = prototype;
        Amount = amount;
        Coordinates = coordinates;
        Direction = direction;
        GhostId = ghostId;
    }
}
