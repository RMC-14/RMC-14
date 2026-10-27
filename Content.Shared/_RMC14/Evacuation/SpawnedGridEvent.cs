using Robust.Shared.Map.Components;

namespace Content.Shared._RMC14.Evacuation;

[ByRefEvent]
public readonly record struct SpawnedGridEvent(Entity<MapGridComponent> grid);
