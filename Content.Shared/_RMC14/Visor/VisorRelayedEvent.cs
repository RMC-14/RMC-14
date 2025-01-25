namespace Content.Shared._RMC14.Visor;

[ByRefEvent]
public readonly record struct VisorRelayedEvent<T>(Entity<CycleableVisorComponent> CycleableVisor, T Event);
