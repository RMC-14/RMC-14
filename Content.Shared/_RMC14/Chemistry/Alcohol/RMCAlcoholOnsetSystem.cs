using Content.Shared.Chemistry.Reagent;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.Alcohol;

public sealed class RMCAlcoholOnsetSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public bool Accumulate(EntityUid uid, ProtoId<ReagentPrototype> reagent, float amount, float threshold)
    {
        var comp = EnsureComp<RMCAlcoholOnsetComponent>(uid);
        var time = _timing.CurTime;
        if (!comp.Reagents.TryGetValue(reagent, out var onset) || time - onset.LastUpdate > comp.ResetAfter)
        {
            onset = new RMCAlcoholOnset();
            comp.Reagents[reagent] = onset;
        }

        onset.Accumulated += amount;
        onset.Threshold = threshold;
        onset.LastUpdate = time;
        return onset.Accumulated >= onset.Threshold;
    }

    public bool IsPending(EntityUid uid, ProtoId<ReagentPrototype> reagent)
    {
        return TryComp(uid, out RMCAlcoholOnsetComponent? comp) &&
               comp.Reagents.TryGetValue(reagent, out var onset) &&
               onset.Accumulated < onset.Threshold;
    }
}
