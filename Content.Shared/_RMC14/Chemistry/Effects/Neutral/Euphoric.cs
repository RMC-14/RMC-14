using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Stun;
using Content.Shared.ActionBlocker;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Euphoric : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> AsphyxiationType = "Asphyxiation";
    private static readonly ProtoId<EmotePrototype> CollapseEmote = "RMCCollapse";

    private static readonly ProtoId<EmotePrototype>[] Emotes =
    [
        "Laugh",
        "RMCGiggle",
        "RMCChuckle",
        "RMCGrin",
        "RMCSmile",
        "RMCTwitch",
    ];

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Causes the release of endorphins, resulting in intense excitement and happiness.";
    }

    // TODO RMC14: pain reduction - pain reduction of 40 * potency (only raises current reduction, at most
    // once per 10s), reset to 0 when reagent leaves

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        if (!random.Prob(0.05f * (float) potency))
            return;

        var emote = args.EntityManager.System<SharedRMCEmoteSystem>();
        emote.TryEmoteWithChat(args.TargetEntity, random.Pick(Emotes), hideLog: true, forceEmote: true);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        if (!random.Prob(0.05f * (float) potency))
            return;

        if (!args.EntityManager.System<ActionBlockerSystem>().CanEmote(args.TargetEntity))
            return;

        var emote = args.EntityManager.System<SharedRMCEmoteSystem>();
        emote.TryEmoteWithChat(args.TargetEntity, CollapseEmote, hideLog: true, forceEmote: true);

        args.EntityManager.System<RMCSizeStunSystem>().TryKnockOut(args.TargetEntity, TimeSpan.FromSeconds(4));
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[AsphyxiationType] = potency * 5f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);

        if (IoCManager.Resolve<IRobustRandom>().Prob(0.5f))
            args.EntityManager.System<SharedPopupSystem>().PopupEntity(Loc.GetString("chem-euphoric-cant-breathe"), args.TargetEntity, args.TargetEntity, PopupType.SmallCaution);
    }
}
