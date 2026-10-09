using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Mobs.Components;
using Robust.Shared.Player;

namespace Content.Server._RMC14.Rules.DistressSignal;

public sealed partial class CMDistressSignalRuleSystem
{
    private const float ForsakenGroundsideMultiplier = 1.0f;
    private const int ForsakenSpawnMin = 1;
    private const int ForsakenSpawnMax = 4;

    private static readonly TimeSpan ForsakenCheckEvery = TimeSpan.FromMinutes(1);

    private void CheckForsakenXenos(CMDistressSignalRuleComponent distress)
    {
        if (!distress.Hijack)
            return;

        var time = Timing.CurTime;
        if (distress.NextForsakenCheck != null && time < distress.NextForsakenCheck)
            return;

        distress.NextForsakenCheck = time + ForsakenCheckEvery;

        var groundsideMarines = 0;
        var marines = EntityQueryEnumerator<ActorComponent, MarineComponent, MobStateComponent, TransformComponent>();
        while (marines.MoveNext(out var marineId, out _, out _, out var mobState, out var xform))
        {
            if (_mobState.IsAlive(marineId, mobState) && _rmcPlanet.IsOnPlanet(xform))
                groundsideMarines++;
        }

        var groundsideXenos = 0;
        var xenos = EntityQueryEnumerator<ActorComponent, XenoComponent, MobStateComponent, TransformComponent>();
        while (xenos.MoveNext(out var xenoId, out _, out _, out var mobState, out var xform))
        {
            if (_mobState.IsAlive(xenoId, mobState) && _rmcPlanet.IsOnPlanet(xform))
                groundsideXenos++;
        }

        if (groundsideMarines <= groundsideXenos * ForsakenGroundsideMultiplier)
            return;

        var count = _random.Next(ForsakenSpawnMin, ForsakenSpawnMax + 1);
        _forsakenXeno.SpawnForsakenXenos(count);
    }
}
