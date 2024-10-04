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
    [Dependency] private readonly CrashLandSystem _crashLand = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;

    protected override void LaunchEvacuationFTL(EntityUid grid, float crashLandChance, SoundSpecifier? launchSound)
    {
        base.LaunchEvacuationFTL(grid, crashLandChance, launchSound);

        var sound = EnsureComp<PlaySoundOnFTLStartComponent>(grid);
        sound.Sound = launchSound;
        Dirty(grid, sound);

        var shuttle = EnsureComp<ShuttleComponent>(grid);
        if (GetEvacuationProgress() < 100 &&
            crashLandChance > 0 &&
            _random.Prob(crashLandChance) &&
            _crashLand.TryGetCrashLandLocation(out var location))
        {
            _shuttle.FTLToCoordinates(grid, shuttle, location, Angle.Zero, hyperspaceTime: 3);
            return;
        }

        _shuttle.FTLToCoordinates(grid, shuttle, grid.ToCoordinates(), Angle.Zero, hyperspaceTime: 1_000_000);
    }
}
