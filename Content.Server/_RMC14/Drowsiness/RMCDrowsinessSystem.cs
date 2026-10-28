using Content.Shared._RMC14.Drowsiness;
using Content.Shared._RMC14.Stun;
using Content.Shared.Bed.Sleep;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Drowsiness;

public sealed class RMCDrowsinessSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RMCSizeStunSystem _sizeStun = default!;
    [Dependency] private readonly SharedStatusEffectsSystem _statusEffects = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<RMCDrowsinessStatusEffectComponent, StatusEffectComponent>();
        while (query.MoveNext(out var drowsiness, out var effect))
        {
            if (time < drowsiness.NextCheck)
                continue;

            drowsiness.NextCheck = time + drowsiness.CheckEvery;

            if (effect.AppliedTo is not { } target)
                continue;

            if (effect.EndEffectTime is { } end && end - time <= drowsiness.Threshold)
                continue;

            if (!_random.Prob(drowsiness.Chance))
                continue;

            _statusEffects.TryAddStatusEffectDuration(target, SleepingSystem.StatusEffectForcedSleeping, drowsiness.Sleep);
            _sizeStun.TryKnockOut(target, drowsiness.KnockOut);
        }
    }
}
