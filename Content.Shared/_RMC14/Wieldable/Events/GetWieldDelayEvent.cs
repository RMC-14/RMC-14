
namespace Content.Shared._RMC14.Wieldable.Events;

[ByRefEvent]
public record struct GetWieldDelayEvent(
    TimeSpan Delay
);
