using Content.Shared._RMC14.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Igniting : RMCChemicalEffect
{
    public override string Abbreviation => "IGT";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Ignites the user with [color=red]{PotencyPerSecond * 30}[/color] fire stacks with 30 intensity and 20 duration.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var flammable = CompOrNull<FlammableComponent>(args);
        var flammableSys = System<SharedRMCFlammableSystem>(args);
        var stacks = flammable?.FireStacks ?? 0;
        flammableSys.AdjustStacks(args.TargetEntity, (int) Math.Max(stacks, (potency * 30).Float()));
    }
}
