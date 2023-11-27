namespace Content.Shared._CM14.Xenos.Evolution;

[ByRefEvent]
public readonly record struct NewXenoEvolvedComponent(Entity<XenoEvolutionComponent> OldXeno);
