using Content.Shared._RMC14.Chemistry.MoveDelay;
using Content.Shared._RMC14.Emote;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Hemolytic : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> AsphyxiationType = "Asphyxiation";

    private static readonly ProtoId<EmotePrototype>[] YawnGaspEmotes =
    [
        "Yawn",
        "Gasp",
    ];

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Causes intravascular hemolysis, destroying erythrocytes in the bloodstream.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (args.EntityManager.TryGetComponent<BloodstreamComponent>(args.TargetEntity, out var bloodstream))
        {
            var bloodstreamSystem = args.EntityManager.System<SharedBloodstreamSystem>();
            bloodstreamSystem.TryModifyBloodLevelQuiet((args.TargetEntity, bloodstream), -potency * 5f);
        }
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (args.EntityManager.TryGetComponent<BloodstreamComponent>(args.TargetEntity, out var bloodstream))
        {
            var bloodstreamSystem = args.EntityManager.System<SharedBloodstreamSystem>();
            bloodstreamSystem.TryModifyBloodLevelQuiet((args.TargetEntity, bloodstream), -potency * 8f);
        }

        var status = args.EntityManager.System<SharedStatusEffectsSystem>();
        var actualPotency = (float) ActualPotency;
        var cap = TimeSpan.FromSeconds(15d * actualPotency);
        var remaining = TimeSpan.Zero;
        if (status.TryGetTime(args.TargetEntity, "StatusEffectDrowsiness", out var time) && time.EndEffectTime is { } end)
        {
            var timing = IoCManager.Resolve<IGameTiming>();
            remaining = end - timing.CurTime;
        }

        var toAdd = TimeSpan.FromSeconds((double) potency);
        if (remaining + toAdd > cap)
            toAdd = cap - remaining;

        if (toAdd > TimeSpan.Zero)
            status.TryAddStatusEffectDuration(args.TargetEntity, "StatusEffectDrowsiness", toAdd);

        var moveDelay = args.EntityManager.System<RMCReagentMoveDelaySystem>();
        moveDelay.SetMoveDelay(args.TargetEntity, $"{args.Reagent?.ID}:{nameof(Hemolytic)}", ActualPotency);

        var random = IoCManager.Resolve<IRobustRandom>();
        if (random.Prob(0.05f))
        {
            var emote = args.EntityManager.System<SharedRMCEmoteSystem>();
            emote.TryEmoteWithChat(args.TargetEntity, random.Pick(YawnGaspEmotes), hideLog: true, ignoreActionBlocker: true, forceEmote: true);
        }
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var damage = new DamageSpecifier();
        damage.DamageDict[AsphyxiationType] = potency * 5f;
        damageable.TryChangeDamage(args.TargetEntity, damage, true, interruptsDoAfters: false);
    }
}
