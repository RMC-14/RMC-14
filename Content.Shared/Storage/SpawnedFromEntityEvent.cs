namespace Content.Shared.Storage;

[ByRefEvent]
public readonly record struct SpawnedFromEntityEvent(EntityUid Spawned);
