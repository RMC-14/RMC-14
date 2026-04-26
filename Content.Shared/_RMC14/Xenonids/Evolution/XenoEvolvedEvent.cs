using Content.Shared.FixedPoint;

namespace Content.Shared._RMC14.Xenonids.Evolution;

[ByRefEvent]
public readonly record struct XenoEvolvedEvent(Entity<XenoEvolutionComponent> Xeno, FixedPoint2 NewPointTotal);
