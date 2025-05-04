namespace Content.Shared._RMC14.OrbitalCannon;

[ByRefEvent]
public readonly record struct OrbitalCannonChangedEvent(Entity<OrbitalCannonComponent> Cannon, bool Warhead, int Fuel);
