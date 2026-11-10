using Content.Shared._RMC14.Medical.Wounds;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.ChemEffectFlags;

public sealed class RMCChemEffectFlagsSystem : EntitySystem
{
    [Dependency] private readonly SharedStatusEffectsSystem _status = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public static readonly EntProtoId NoBleedingFlag = "RMCStatusEffectNoBleeding";

    private static readonly TimeSpan ResetInterval = TimeSpan.FromSeconds(16);
    private static readonly TimeSpan MetabolismGrace = TimeSpan.FromSeconds(1);

    public override void Initialize()
    {
        SubscribeLocalEvent<WoundedComponent, CMBleedAttemptEvent>(OnWoundedBleedAttempt);
    }

    private void OnWoundedBleedAttempt(Entity<WoundedComponent> ent, ref CMBleedAttemptEvent args)
    {
        if (_status.HasEffectComp<RMCNoBleedingStatusEffectComponent>(ent))
            args.Cancelled = true;
    }

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
}
