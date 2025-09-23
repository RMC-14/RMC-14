using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._RMC14.Shuttles;

[RegisterComponent]
public sealed partial class RMCSpawnEntityOnFTLStartComponent : Component
{
    [DataField]
    public HashSet<MapCoordinates> Coordinates = new();

    [DataField]
    public EntProtoId SpawnedEntity = "FloorDeathEntity";
}
