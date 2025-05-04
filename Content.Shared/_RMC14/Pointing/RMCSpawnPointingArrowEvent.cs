using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Pointing;

[ByRefEvent]
public record struct RMCSpawnPointingArrowEvent(
    EntProtoId Arrow,
    EntityCoordinates Coordinates,
    EntityUid? Spawned = null
);
