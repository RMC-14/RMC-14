namespace Content.Shared._RMC14.OnCollide;

[ByRefEvent]
public readonly record struct DamageCollideEvent(EntityUid Target);
