using Content.Shared._RMC14.Overwatch;
using Content.Shared.Mobs;

namespace Content.Client._RMC14.Overwatch;

public readonly record struct OverwatchListEntry
{
    public readonly NetEntity Id;
    public readonly OverwatchLocation Location;
    public readonly OverwatchMarine? Marine;
    public readonly OverwatchTripodCamera? Camera;

    public bool IsCamera => Camera != null;

    public bool IsAlive => Camera != null || Marine is { State: MobState.Alive };

    public OverwatchListEntry(OverwatchMarine marine)
    {
        Id = marine.Id;
        Location = marine.Location;
        Marine = marine;
        Camera = null;
    }

    public OverwatchListEntry(OverwatchTripodCamera camera)
    {
        Id = camera.Id;
        Location = camera.Location;
        Marine = null;
        Camera = camera;
    }
}
