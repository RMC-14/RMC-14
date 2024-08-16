using Robust.Shared.Map;

namespace Content.Shared._RMC14.Dropship.Weapon;

[ByRefEvent]
public record struct LaserTargetEvent(
    EntityUid User,
    EntityCoordinates Coordinates,
    EntityUid? Target,
    bool Handled = false
);
