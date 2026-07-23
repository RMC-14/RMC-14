using Content.Shared._RMC14.Chat;
using Content.Shared._RMC14.Drowsyness;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Jittering;
using Content.Shared.StatusEffect;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Neurotoxic : RMCChemicalEffect
{
    public override string Abbreviation => "NRT";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        // TODO RMC14 organ brain damage
        // TODO RMC14 apply neurotoxin
        // TODO RMC14 reaction mob
        return $"Overdoses cause the user to jitter, get drowsy, and drool.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (!IsHumanoid(args))
            return;

        // TODO RMC14 organ brain damage
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RMC14 organ brain damage
        var status = System<StatusEffectsSystem>(args);
        var time = TimeSpan.Zero;
        if (status.TryGetTime(args.TargetEntity, "Jitter", out var statusTime))
            time = statusTime.Value.Item2 - statusTime.Value.Item1;

        status.TryAddStatusEffect<JitteringComponent>(
            args.TargetEntity,
            "Jitter",
            TimeSpan.FromSeconds(FixedPoint2.Min(time.TotalSeconds + potency, potency * 3).Double()),
            true
        );

        if (ProbHundred(50))
        {
            if (TryComp(args, out DrowsynessComponent? drowsyness))
            {
                var drowsynessSys = System<DrowsynessSystem>(args);
                drowsynessSys.TryChange(args.TargetEntity, FixedPoint2.Min(drowsyness.Amount + potency, potency * 3));
            }
        }

        if (ProbHundred(10))
        {
            var chat = System<SharedCMChatSystem>(args);
            chat.Emote(args.TargetEntity, "drools");
        }
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RMC14 apply neurotoxin
    }
}
