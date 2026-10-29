using Content.Shared.Speech.EntitySystems;
using Content.Shared.StatusEffect;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.Disabilities;

public sealed class RMCNervousSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly SharedStutteringSystem _stuttering = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan StutterTime = TimeSpan.FromSeconds(20);

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<RMCNervousComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (time < comp.NextCheck)
                continue;

            comp.NextCheck = time + CheckInterval;

            if (!_random.Prob(0.1f))
                continue;

            if (_statusEffects.TryGetTime(uid, SharedStutteringSystem.StutterKey, out var stutter) &&
                stutter.Value.Item2 - time >= StutterTime)
            {
                continue;
            }

            _stuttering.DoStutter(uid, StutterTime, true);
        }
    }
}
