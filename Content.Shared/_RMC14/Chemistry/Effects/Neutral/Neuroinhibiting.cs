using Content.Shared._RMC14.Deafness;
using Content.Shared._RMC14.Mute;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.StatusEffect;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Neuroinhibiting : RMCChemicalEffect
{
    public override string Abbreviation => "NIH";
    public override int? MaxLevel => 7;

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        // TODO RMC14 organ damage brain
        // TODO RMC14 disability nervous
        return $"Causes blindness at a potency of 1 or higher, deafness at a potency of 2 or higher, and muteness at a potency of 3 or higher.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RMC14 resist neuro
        if (!IsHumanoid(args))
            return;

        if (potency > 1)
        {
            if (TryComp(args, out BlindableComponent? blindable))
            {
                var blindableSys = System<BlindableSystem>(args);
                blindableSys.AdjustEyeDamage((args.TargetEntity, blindable), blindable.MaxDamage);
            }
        }
        else
        {
            // TODO RMC14 nearsighted
        }

        if (potency > 2)
        {
            var deafnessSys = System<SharedDeafnessSystem>(args);
            deafnessSys.TryDeafen(args.TargetEntity, TimeSpan.FromSeconds(5), true);
        }

        if (potency > 3)
        {
            var status = System<StatusEffectsSystem>(args);
            status.TryAddStatusEffect<RMCMutedComponent>(
                args.TargetEntity,
                "RMCMuted",
                TimeSpan.FromSeconds(5),
                true
            );
        }
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RMC14 organ damage brain
        // TODO RMC14 disability nervous
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RMC14 organ damage brain
    }
}
