namespace Content.Shared._RMC14.Explosion;

[ByRefEvent]
public readonly record struct CMClusterSpawnedEvent(List<EntityUid> Spawned);
