namespace Content.Shared._RMC14.Medical.Pain;

[ByRefEvent]
public readonly record struct PainLevelChangedEvent(EntityUid Ent, int OldLevel, int NewLevel);
