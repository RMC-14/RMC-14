namespace Content.Shared._RMC14.OrbitalCannon;

[ByRefEvent]
public readonly record struct OrbitalCannonSafetyChangedEvent(bool Engaged);
