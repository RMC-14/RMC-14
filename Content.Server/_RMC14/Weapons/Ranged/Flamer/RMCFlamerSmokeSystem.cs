using Content.Server.Fluids.EntitySystems;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.Weapons.Ranged.Flamer;
using Content.Shared.Chemistry.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Weapons.Ranged.Flamer;

public sealed class RMCFlamerSmokeSystem : EntitySystem
{
    private static readonly FixedPoint2 SmokeUnitsPerTile = FixedPoint2.New(35);
    private const float SmokeDuration = 30f;

    private const int SmokeSpread = 1;

    [Dependency] private readonly RMCReagentSystem _rmcReagent = default!;
    [Dependency] private readonly SmokeSystem _smoke = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;
        var chains = EntityQueryEnumerator<RMCFlamerChainComponent>();
        while (chains.MoveNext(out var uid, out var comp))
        {
            if (!comp.Smoke)
                continue;

            if (comp.Tiles.Count == 0)
            {
                QueueDel(uid);
                continue;
            }

            foreach (var tile in comp.Tiles)
            {
                if (time < tile.At)
                    continue;

                comp.Tiles.Remove(tile);
                var index = comp.TilesPlaced++;

                // cm13 skips the first tile entirely & shrinks the 2nd to a tiny burst
                if (index != 0 && _rmcReagent.TryIndex(comp.Reagent, out var reagent))
                {
                    var cloud = Spawn(comp.Spawn, tile.Coordinates);
                    var solution = new Solution(reagent.ID, SmokeUnitsPerTile);
                    var spread = index == 1 ? 0 : SmokeSpread;
                    _smoke.StartSmoke(cloud, solution, SmokeDuration, spread);
                }

                break;
            }
        }
    }
}
