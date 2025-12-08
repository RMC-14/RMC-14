using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Excreting : RMCChemicalEffect
{
    public override string Abbreviation => "EXT";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Removes {FixedPoint2.New(Level * 2):F2} units of every reagent in the bloodstream";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (args.SolutionEnt is not { } solution)
            return;

        var solutionSys = args.EntityManager.System<SharedSolutionContainerSystem>();
        solutionSys.RemoveEachReagent(solution, FixedPoint2.New(Level * 2));
    }
}
