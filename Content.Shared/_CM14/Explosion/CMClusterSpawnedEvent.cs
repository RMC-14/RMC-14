namespace Content.Shared._CM14.Explosion;

[ByRefEvent]
public readonly record struct CMClusterSpawnedEvent(List<EntityUid> Spawned);
