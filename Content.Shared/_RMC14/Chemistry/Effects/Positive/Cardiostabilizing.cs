using System.Collections.Immutable;
using Content.Shared._RMC14.Chat;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Cardiostabilizing : RMCChemicalEffect
{
    private static readonly ImmutableArray<string> Emotes = ImmutableArray.Create("twitches", "blinks rapidly", "shivers");

    public override string Abbreviation => "CSL";

    protected override string? ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Dont merge just yet vro.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RMC14 Pain Reduction

        // TODO RMC14 allow breathing in crit instead of reducing asphyxiation
        var mobState = System<MobStateSystem>(args);
        if (mobState.IsCritical(args.TargetEntity))
        {
            TryHealDamageGroup(args, AirlossGroup, 1);
        }
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // M.make_jittery(5)
        var stun = System<SharedStunSystem>(args);
        stun.TryKnockdown(args.TargetEntity, TimeSpan.FromSeconds(40), true);
        stun.TryStun(args.TargetEntity, TimeSpan.FromSeconds(40), true);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (!IsHumanoid(args))
            return;

        // TODO RMC14 Heart Damage

        if (ProbHundred(5))
        {
            var emote = IoCManager.Resolve<IRobustRandom>().Pick(Emotes);
            var chat = System<SharedCMChatSystem>(args);
            chat.Emote(args.TargetEntity, emote);
        }
    }
}
