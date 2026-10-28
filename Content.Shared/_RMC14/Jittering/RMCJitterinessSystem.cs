using Content.Shared._RMC14.Standing;
using Content.Shared.Jittering;
using Content.Shared.Mobs.Systems;
using Content.Shared.StatusEffect;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Jittering;

public sealed class RMCJitterinessSystem : EntitySystem
{
    private static readonly ProtoId<StatusEffectPrototype> JitterStatus = "Jitter";

    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly StatusEffectsSystem _status = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public void MakeJittery(EntityUid uid, float amount)
    {
        if (_net.IsClient || _mobState.IsDead(uid))
            return;

        var isNew = !HasComp<RMCJitterinessComponent>(uid);
        var comp = EnsureComp<RMCJitterinessComponent>(uid);
        if (isNew)
            comp.NextDecay = _timing.CurTime + comp.DecayEvery;

        comp.Jitteriness = Math.Clamp(comp.Jitteriness + amount, 0f, comp.Max);
        UpdateJitter((uid, comp));
    }

    public float GetJitteriness(EntityUid uid)
    {
        return TryComp<RMCJitterinessComponent>(uid, out var comp) ? comp.Jitteriness : 0f;
    }

    public void SetJitteriness(EntityUid uid, float value)
    {
        var current = GetJitteriness(uid);
        if (value > current)
            MakeJittery(uid, value - current);
        else if (value < current)
            ReduceJitteriness(uid, current - value);
    }

    public void ReduceJitteriness(EntityUid uid, float amount)
    {
        if (_net.IsClient || !TryComp<RMCJitterinessComponent>(uid, out var comp))
            return;

        comp.Jitteriness = MathF.Max(comp.Jitteriness - amount, 0f);
        if (comp.Jitteriness <= 0f)
            RemCompDeferred<RMCJitterinessComponent>(uid);
    }

    public void ClearJitteriness(EntityUid uid)
    {
        if (_net.IsClient)
            return;

        RemCompDeferred<RMCJitterinessComponent>(uid);
    }

    private void UpdateJitter(Entity<RMCJitterinessComponent> ent)
    {
        if (ent.Comp.Jitteriness <= ent.Comp.VisibleThreshold)
            return;

        var duration = ent.Comp.DecayEvery + TimeSpan.FromSeconds(0.5);
        var time = _timing.CurTime;
        if (_status.TryGetTime(ent, JitterStatus, out var existing) && existing.Value.Item2 - time >= duration)
            return;

        _jitter.DoJitter(ent, duration, true, ent.Comp.Jitteriness, forceValueChange: true);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<RMCJitterinessComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (time < comp.NextDecay)
                continue;

            comp.NextDecay = time + comp.DecayEvery;

            var resting = TryComp(uid, out RMCRestComponent? rest) && rest.Resting;
            comp.Jitteriness = MathF.Max(comp.Jitteriness - (resting ? comp.RestingDecay : comp.Decay), 0f);
            if (comp.Jitteriness <= 0f)
            {
                RemCompDeferred<RMCJitterinessComponent>(uid);
                continue;
            }

            UpdateJitter((uid, comp));
        }
    }
}
