using Content.Shared.FixedPoint;

namespace Content.Shared._RMC14.Evasion;

[ByRefEvent]
public record struct EvasionRefreshModifiersEvent(
    Entity<EvasionComponent> Entity,
    FixedPoint2 Evasion,
    FixedPoint2 EvasionFriendly
);