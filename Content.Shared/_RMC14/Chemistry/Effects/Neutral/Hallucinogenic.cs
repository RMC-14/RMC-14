using System.Collections.Immutable;
using Content.Shared._RMC14.Chat;
using Content.Shared._RMC14.Stun;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Jittering;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Hallucinogenic : RMCChemicalEffect
{
    private static readonly EntProtoId<StatusEffectComponent> Druggy = "StatusEffectSeeingRainbow";
    private static readonly ImmutableArray<string> Emotes = ImmutableArray.Create("twitches", "drools", "moans", "giggles");

    public override string Abbreviation => "HLG";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        // TODO RMC14 hallucination
        var hallucinating = Potency > 2
            ? ". Powerful enough to also cause jittering and vivid hallucinations."
            : ".";

        return $"Causes perceptions of psychedelic colors and euphoria{hallucinating}\n" +
               $"Overdoses cause jitteriness.\n" +
               $"Critical overdoses cause [color=red]40[/color] seconds of paralysis";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (ProbHundred(5))
        {
            var emote = IoCManager.Resolve<IRobustRandom>().Pick(Emotes);
            var chat = System<SharedCMChatSystem>(args);
            chat.Emote(args.TargetEntity, emote);
        }

        if (Potency > 2)
        {
            // TODO RMC14 hallucination
            var jitter = System<SharedJitteringSystem>(args);
            jitter.DoJitter(args.TargetEntity, TimeSpan.FromSeconds(2), true, 5, 10);
        }

        var status = System<SharedStatusEffectsSystem>(args);
        status.TryGetTime(args.TargetEntity, Druggy, out var statusTime);

        var curDruggy = statusTime.EndEffectTime?.TotalSeconds ?? 0;
        var druggyTime = FixedPoint2.Min(curDruggy + potency, potency * 10).Double();
        status.TryAddStatusEffectDuration(args.TargetEntity, Druggy, TimeSpan.FromSeconds(druggyTime));
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RMC14 random step
        // TODO RMC14 hallucination
        var jitter = System<SharedJitteringSystem>(args);
        jitter.DoJitter(args.TargetEntity, TimeSpan.FromSeconds(2), true, 5, 10);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RMC14 organ damage brain
        var knockOut = System<RMCSizeStunSystem>(args);
        knockOut.TryKnockOut(args.TargetEntity, TimeSpan.FromSeconds(40), true);
    }
}
