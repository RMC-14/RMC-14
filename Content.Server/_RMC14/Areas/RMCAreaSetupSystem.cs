using Content.Server.GameTicking.Events;
using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Spawners;

namespace Content.Server._RMC14.Areas;

public sealed class RMCAreaSetupSystem : EntitySystem
{
    [Dependency] private readonly AreaSystem _area = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
    }

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        var spawns = EntityQueryEnumerator<XenoLeaderSpawnPointComponent>();
        while (spawns.MoveNext(out var uid, out _))
        {
            if (!_area.TryGetArea(uid, out var area, out _))
                continue;

            _area.SetXenoHiveSetupRestriction(area.Value, null);
        }
    }
}
