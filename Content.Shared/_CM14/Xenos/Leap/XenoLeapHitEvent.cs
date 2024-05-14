namespace Content.Shared._CM14.Xenos.Leap;

[ByRefEvent]
public readonly record struct XenoLeapHitEvent(XenoLeapingComponent Leaping, EntityUid Hit);
