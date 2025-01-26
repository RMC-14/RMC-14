namespace Content.Shared._RMC14.Visor;

[ByRefEvent]
public readonly record struct DeactivateVisorEvent(Entity<CycleableVisorComponent> CycleableVisor, EntityUid? User);
