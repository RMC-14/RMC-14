using System.Linq;
using Content.Shared._RMC14.Chemistry.Effects.Positive;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Stunnable;
using Content.Shared.FixedPoint;

namespace Content.Shared._RMC14.Chemistry;

/// <summary>
///     cm13: /mob/living/proc/Stun and AdjustStun both burn through any Nervestimulating-carrying reagent
///     in the mob's blood every time it's stunned, by up to 10 units, proportional to the stun duration -
///     "reduce amount of NST stim in blood for every stun" (living_health_procs.dm). This is a balance cost
///     so a single dose can't grant lasting stun immunity: getting hit while under the influence burns
///     through the dose faster.
/// </summary>
public sealed class NervestimulatingConsumptionSystem : EntitySystem
{
    [Dependency] private readonly RMCReagentSystem _rmcReagent = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BloodstreamComponent, StunnedEvent>(OnStunned);
    }

    private void OnStunned(Entity<BloodstreamComponent> ent, ref StunnedEvent args)
    {
        // cm: nst_stim.volume += max(min((-1*amount)/10, 0), -10) amount is in deciseconds,
        // so 1 unit lost per second stunned, capped at 10 units
        var reduction = FixedPoint2.New(MathF.Min((float) args.Time.TotalSeconds, 10f));
        if (reduction <= FixedPoint2.Zero)
            return;

        if (!_solutionContainer.ResolveSolution((ent.Owner, null), ent.Comp.ChemicalSolutionName, ref ent.Comp.ChemicalSolution, out var solution))
            return;

        foreach (var reagent in solution.Contents.ToArray())
        {
            if (!_rmcReagent.TryIndex(reagent.Reagent, out var proto) || proto.Metabolisms is not { } metabolisms)
                continue;

            var hasNervestimulating = false;
            foreach (var (_, entry) in metabolisms)
            {
                foreach (var effect in entry.Effects)
                {
                    if (effect is not Nervestimulating)
                        continue;

                    hasNervestimulating = true;
                    break;
                }

                if (hasNervestimulating)
                    break;
            }

            if (!hasNervestimulating)
                continue;

            _solutionContainer.RemoveReagent(ent.Comp.ChemicalSolution.Value, reagent.Reagent, reduction);
        }
    }
}
