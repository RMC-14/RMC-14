using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Oculotoxic : RMCChemicalEffect
{
    public override string Abbreviation => "OCT";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        // TODO RMC14 organ eye damage
        return $"Overdoses cause the user to go [color=red]blind[/color].";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (!IsHumanoid(args))
            return;

        // TODO RMC14 organ eye damage
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (TryComp(args, out BlindableComponent? blindable))
        {
            var blindableSys = System<BlindableSystem>(args);
            blindableSys.AdjustEyeDamage((args.TargetEntity, blindable), blindable.MaxDamage);
        }
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RM14 organ brain damage
    }
}
