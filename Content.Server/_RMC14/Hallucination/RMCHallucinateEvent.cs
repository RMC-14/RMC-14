namespace Content.Server._RMC14.Hallucination;

[ByRefEvent]
public record struct RMCHallucinateEvent(Entity<RMCHallucinatingComponent> Victim, bool Handled = false);
