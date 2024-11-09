namespace Content.Shared._RMC14.Xenonids.Evolution;

[ByRefEvent]
public readonly record struct NewXenoEvolvedEvent(Entity<XenoEvolutionComponent> OldXeno, EntityUid NewXeno);
