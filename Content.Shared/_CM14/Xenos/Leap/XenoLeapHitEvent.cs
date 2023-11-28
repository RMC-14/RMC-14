using Content.Shared._CM14.Marines;

namespace Content.Shared._CM14.Xenos.Leap;

[ByRefEvent]
public readonly record struct XenoLeapHitEvent(Entity<MarineComponent> Hit);
