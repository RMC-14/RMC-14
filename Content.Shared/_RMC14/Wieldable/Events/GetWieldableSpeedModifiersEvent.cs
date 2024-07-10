
namespace Content.Shared._RMC14.Wieldable.Events;

[ByRefEvent]
public record struct GetWieldableSpeedModifiersEvent(
    float Walk,
    float Sprint
);
