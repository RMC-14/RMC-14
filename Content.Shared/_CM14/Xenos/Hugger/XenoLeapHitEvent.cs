using Content.Shared._CM14.Marines;

namespace Content.Shared._CM14.Xenos.Hugger;

[ByRefEvent]
public readonly record struct XenoLeapHitEvent(Entity<MarineComponent> Hit);
