namespace Content.Shared._CM14.Xenonids.Leap;

[ByRefEvent]
public readonly record struct XenoLeapHitEvent(XenoLeapingComponent Leaping, EntityUid Hit);
