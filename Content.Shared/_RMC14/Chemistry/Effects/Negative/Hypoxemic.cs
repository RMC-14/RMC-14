using Content.Shared._RMC14.Emote;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Hypoxemic : RMCChemicalEffect
{
    private static readonly ProtoId<EmotePrototype> GaspEmote = "Gasp";

    public override string Abbreviation => "HPX";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Deals [color=red]{PotencyPerSecond * 2}[/color] airloss damage and causes the victim to gasp for air.\n" +
               $"Overdoses cause [color=red]{PotencyPerSecond}[/color] brute, [color=red]{PotencyPerSecond}[/color] toxin, and [color=red]{PotencyPerSecond * 5}[/color] airloss damage.\n" +
               $"Critical overdoses cause [color=red]{PotencyPerSecond * 5}[/color] brute and [color=red]{PotencyPerSecond * 2}[/color] toxin damage.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        TryChangeDamage(args, AsphyxiationType, potency * 2);

        var emoteSystem = System<SharedRMCEmoteSystem>(args);
        if (ProbHundred(10))
            emoteSystem.TryEmoteWithChat(args.TargetEntity, GaspEmote, hideLog: true, ignoreActionBlocker: true, forceEmote: true);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        TryChangeDamage(args, BluntType, potency);
        TryChangeDamage(args, PoisonType, potency);
        TryChangeDamage(args, AsphyxiationType, potency * 5);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        TryChangeDamage(args, BluntType, potency * 5);
        TryChangeDamage(args, PoisonType, potency * 2);
    }
}
