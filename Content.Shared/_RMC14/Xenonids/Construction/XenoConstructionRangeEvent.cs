using Content.Shared.FixedPoint;

namespace Content.Shared._RMC14.Xenonids.Construction;

[ByRefEvent]
public record struct XenoConstructionRangeEvent(FixedPoint2 Range);
