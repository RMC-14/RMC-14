using Content.Server._RMC14.Dropship;
using Robust.Shared.Spawners;

namespace Content.Server._RMC14.Sentries;

public sealed class RMCSentrySystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<DropshipLandedOnPlanetEvent>(OnDropshipLandedOnPlanet);
    }

    private void OnDropshipLandedOnPlanet(ref DropshipLandedOnPlanetEvent ev)
    {
        var query = EntityQueryEnumerator<TimedDespawnOnLandingComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            EnsureComp<TimedDespawnComponent>(uid).Lifetime = comp.Lifetime;
            RemCompDeferred<TimedDespawnOnLandingComponent>(uid);
        }
    }
}
