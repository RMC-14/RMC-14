using Content.Shared.FixedPoint;

namespace Content.Shared._RMC14.Xenonids.Evolution;

[ByRefEvent]
public readonly record struct XenoDevolvedEvent(EntityUid Xeno, FixedPoint2 NewPointTotal);
