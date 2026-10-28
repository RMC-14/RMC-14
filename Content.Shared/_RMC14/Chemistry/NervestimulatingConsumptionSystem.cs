using Content.Shared._RMC14.Chemistry.Effects.Positive;
using Content.Shared._RMC14.Chemistry.Reagent;
using Content.Shared._RMC14.StatusEffect;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Stunnable;
using Content.Shared.FixedPoint;

namespace Content.Shared._RMC14.Chemistry;

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
        var ev = new RMCStatusEffectTimeEvent("Stun", args.Time);
        RaiseLocalEvent(ent, ref ev);

        var reduction = FixedPoint2.New(MathF.Min((float) ev.Duration.TotalSeconds, 10f));
        if (reduction <= FixedPoint2.Zero)
            return;

        if (!_solutionContainer.ResolveSolution((ent.Owner, null), ent.Comp.ChemicalSolutionName, ref ent.Comp.ChemicalSolution, out var solution))
            return;

        foreach (var reagent in solution.Contents.ToArray())
        {
            if (!_rmcReagent.HasProperty<Nervestimulating>(reagent.Reagent.Prototype))
                continue;

            _solutionContainer.RemoveReagent(ent.Comp.ChemicalSolution.Value, reagent.Reagent, reduction);
        }
    }
}
