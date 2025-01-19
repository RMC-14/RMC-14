using Robust.Shared.Map;

namespace Content.Shared._RMC14.OrbitalCannon;

[ByRefEvent]
public readonly record struct OrbitalBombardmentFireEvent(
    Entity<OrbitalCannonComponent> Cannon,
    EntityUid Warhead,
    int Fuel,
    MapCoordinates Coordinates
);
