namespace Content.Shared._RMC14.Explosion;

[ByRefEvent]
public record struct RMCTriggerEvent(EntityUid? User, bool Handled);
