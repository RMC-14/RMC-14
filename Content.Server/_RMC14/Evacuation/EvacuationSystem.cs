using System.Numerics;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Systems;
using Content.Shared._RMC14.CrashLand;
using Content.Shared._RMC14.Evacuation;
using Content.Shared._RMC14.Shuttles;
using Content.Shared.Coordinates;
using Robust.Shared.Audio;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Evacuation;

public sealed class EvacuationSystem : SharedEvacuationSystem
{
    [Dependency] private readonly SharedCrashLandSystem _crashLand = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;

    private EntityQuery<EvacuationDoorComponent> _evacuationDoorQuery;

    private readonly HashSet<Entity<EvacuationDoorComponent>> _doors = new();

    public override void Initialize()
    {
        base.Initialize();
        _evacuationDoorQuery = GetEntityQuery<EvacuationDoorComponent>();
    }

    protected override void LaunchEvacuationFTL(EntityUid grid, float crashLandChance, SoundSpecifier? launchSound)
    {
        base.LaunchEvacuationFTL(grid, crashLandChance, launchSound);

        var sound = EnsureComp<PlaySoundOnFTLStartComponent>(grid);
        sound.Sound = launchSound;
        Dirty(grid, sound);

        var gridTransform = Transform(grid);
        var children = gridTransform.ChildEnumerator;
        while (children.MoveNext(out var child))
        {
            if (_evacuationDoorQuery.TryComp(child, out var door))
            {
                door.Locked = true;
                Dirty(child, door);

                _doors.Clear();
                _entityLookup.GetEntitiesInRange(child.ToCoordinates(), 2.5f, _doors);
                foreach (var nearbyDoor in _doors)
                {
                    nearbyDoor.Comp.Locked = true;
                    Dirty(nearbyDoor);
                }
            }
        }

        var shuttle = EnsureComp<ShuttleComponent>(grid);
        if (GetEvacuationProgress() < 100 &&
            crashLandChance > 0 &&
            _random.Prob(crashLandChance) &&
            _crashLand.TryGetCrashLandLocation(out var location))
        {
            children = gridTransform.ChildEnumerator;
            while (children.MoveNext(out var child))
            {
                if (_evacuationDoorQuery.TryComp(child, out var door))
                {
                    door.Locked = false;
                    Dirty(child, door);
                }
            }

            _shuttle.FTLToCoordinates(grid, shuttle, location.Offset(new Vector2(-0.5f, -0.5f)), Angle.Zero, hyperspaceTime: 3);
            return;
        }

        _shuttle.FTLToCoordinates(grid, shuttle, grid.ToCoordinates(), Angle.Zero, hyperspaceTime: 1_000_000);
    }
}
