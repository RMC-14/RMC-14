
namespace Content.Shared._RMC14.Wieldable.Events;

[ByRefEvent]
public record struct GetWieldableSpeedModifiersEvent(
    float UnwieldedWalk,
    float UnwieldedSprint,
    float WieldedWalk,
    float WieldedSprint
);
