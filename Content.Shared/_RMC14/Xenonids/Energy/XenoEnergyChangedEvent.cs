using Content.Shared.FixedPoint;

namespace Content.Shared._RMC14.Xenonids.Energy;

[ByRefEvent]
public record struct XenoEnergyChangedEvent(FixedPoint2 NewEnergy);
