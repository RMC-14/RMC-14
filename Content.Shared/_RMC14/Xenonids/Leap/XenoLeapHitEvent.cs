namespace Content.Shared._RMC14.Xenonids.Leap;

[ByRefEvent]
public readonly record struct XenoLeapHitEvent(XenoLeapingComponent Leaping, EntityUid Hit);
