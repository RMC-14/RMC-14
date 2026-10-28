using Content.Shared._RMC14.Chemistry.SpeedModifier;
using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Stun;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Antispasmodic : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> AsphyxiationType = "Asphyxiation";

    private static readonly ProtoId<EmotePrototype> Yawn = "Yawn";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Relaxes smooth muscles and treats muscle spasms by blocking the neurotransmitter acetylcholine. High concentrations can cause respiratory failure and cardiac arrest.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var speedModifier = args.EntityManager.System<RMCReagentSpeedModifierSystem>();
        speedModifier.SetSpeedModifier(args.TargetEntity, $"{args.Reagent?.ID}:{nameof(Antispasmodic)}", ActualPotency);

        var random = IoCManager.Resolve<IRobustRandom>();
        if (random.Prob(0.05f))
        {
            var emote = args.EntityManager.System<SharedRMCEmoteSystem>();
            emote.TryEmoteWithChat(args.TargetEntity, Yawn, hideLog: true, forceEmote: true);
        }
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var speedModifier = args.EntityManager.System<RMCReagentSpeedModifierSystem>();
        speedModifier.SetSpeedModifier(args.TargetEntity, $"{args.Reagent?.ID}:{nameof(Antispasmodic)}Overdose", 2f * ActualPotency);

        if (IoCManager.Resolve<IRobustRandom>().Prob(0.05f))
            args.EntityManager.System<SharedPopupSystem>().PopupEntity(Loc.GetString("chem-antispasmodic-weak"), args.TargetEntity, args.TargetEntity, PopupType.SmallCaution);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        if (random.Prob(0.15f * (float) potency))
        {
            var knockOut = args.EntityManager.System<RMCSizeStunSystem>();
            knockOut.TryKnockOut(args.TargetEntity, TimeSpan.FromSeconds(ActualPotency * 2f));
        }

        var damage = new DamageSpecifier();
        damage.DamageDict[AsphyxiationType] = potency;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);

        if (random.Prob(0.025f))
            args.EntityManager.System<SharedPopupSystem>().PopupEntity(Loc.GetString("chem-antispasmodic-breathe"), args.TargetEntity, args.TargetEntity, PopupType.MediumCaution);

        // TODO RMC14: heart organ damage - 0.5 * potency heart damage per 2s tick
    }
}
