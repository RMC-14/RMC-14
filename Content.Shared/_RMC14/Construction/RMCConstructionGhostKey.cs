using Content.Shared._RMC14.Construction.Prototypes;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Construction;

[Serializable, NetSerializable]
public readonly struct RMCConstructionGhostKey : IEquatable<RMCConstructionGhostKey>
{
    public readonly ProtoId<RMCConstructionPrototype> Prototype;
    public readonly NetCoordinates Coordinates;
    public readonly Direction Direction;

    public RMCConstructionGhostKey(ProtoId<RMCConstructionPrototype> prototype, NetCoordinates coordinates, Direction direction)
    {
        Prototype = prototype;
        Coordinates = coordinates;
        Direction = direction;
    }

    public bool Equals(RMCConstructionGhostKey other)
    {
        return Prototype.Equals(other.Prototype) &&
               Coordinates.Equals(other.Coordinates) &&
               Direction == other.Direction;
    }

    public override bool Equals(object? obj)
    {
        return obj is RMCConstructionGhostKey other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Prototype, Coordinates, (int) Direction);
    }
}
