namespace Content.Shared._RMC14.Visor;

[ByRefEvent]
public record struct ActivateVisorEvent(
    Entity<CycleableVisorComponent> CycleableVisor,
    EntityUid? User,
    bool Handled = false
);
