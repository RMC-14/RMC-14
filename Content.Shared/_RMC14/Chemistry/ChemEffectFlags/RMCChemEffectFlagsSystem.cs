using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.ChemEffectFlags;

public sealed class RMCChemEffectFlagsSystem : EntitySystem
{
    [Dependency] private readonly SharedBloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly SharedStatusEffectsSystem _status = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public static readonly EntProtoId NoBleedingFlag = "RMCStatusEffectNoBleeding";

    private static readonly TimeSpan ResetInterval = TimeSpan.FromSeconds(16);
    private static readonly TimeSpan MetabolismGrace = TimeSpan.FromSeconds(1);

    public bool TrySetFlag(EntityUid target, EntProtoId flag)
    {
        var now = _timing.CurTime;
        var flags = EnsureComp<RMCChemEffectFlagsComponent>(target);
        if (now >= flags.ResetAt)
        {
            flags.ResetAt = now + ResetInterval;
            Dirty(target, flags);
        }

        if (_status.TryGetTime(target, flag, out var time) && time.EndEffectTime >= flags.ResetAt)
            return false;

        return _status.TrySetStatusEffectDuration(target, flag, flags.ResetAt - now + MetabolismGrace);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<RMCNoBleedingStatusEffectComponent, StatusEffectComponent>();
        while (query.MoveNext(out _, out _, out var effect))
        {
            if (effect.AppliedTo is not { } target ||
                !TryComp(target, out BloodstreamComponent? bloodstream) ||
                bloodstream.BleedAmount <= 0)
            {
                continue;
            }

            _bloodstream.TryModifyBleedAmount((target, bloodstream), -bloodstream.BleedAmount);
        }
    }
}
