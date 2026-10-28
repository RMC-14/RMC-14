using Content.Shared._RMC14.Chemistry.ChemEffectFlags;
using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Temperature;
using Content.Shared._RMC14.Stun;
using Content.Shared.Atmos;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Hypothermic : RMCChemicalEffect
{
    private static readonly ProtoId<ReagentPrototype> Cryoxadone = "CMCryoxadone";
    private static readonly ProtoId<ReagentPrototype> Clonexadone = "CMClonexadone";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Causes an endothermic reaction when metabolized, decreasing internal body temperature.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var random = IoCManager.Resolve<IRobustRandom>();
        if (random.Prob(0.05f))
        {
            var emote = args.EntityManager.System<SharedRMCEmoteSystem>();
            emote.TryEmoteWithChat(args.TargetEntity, "RMCShiver", hideLog: true, forceEmote: true);
        }

        var temperature = args.EntityManager.System<SharedRMCTemperatureSystem>();
        var current = temperature.GetTemperature(args.TargetEntity);

        if (current < Atmospherics.T0C &&
            args.Source is { } source &&
            (source.ContainsPrototype(Cryoxadone) || source.ContainsPrototype(Clonexadone)))
        {
            args.EntityManager.System<RMCChemEffectFlagsSystem>().TrySetFlag(args.TargetEntity, RMCChemEffectFlagsSystem.NoBleedingFlag);
        }

        temperature.ForceChangeTemperature(args.TargetEntity, MathF.Max(0f, current - (float) potency * 2f));
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var temperature = args.EntityManager.System<SharedRMCTemperatureSystem>();
        temperature.ForceChangeTemperature(args.TargetEntity, MathF.Max(0f, temperature.GetTemperature(args.TargetEntity) - (float) potency * 5f));

        var status = args.EntityManager.System<SharedStatusEffectsSystem>();
        var remaining = TimeSpan.Zero;
        if (status.TryGetTime(args.TargetEntity, "StatusEffectDrowsiness", out var time) && time.EndEffectTime is { } end)
        {
            var timing = IoCManager.Resolve<IGameTiming>();
            remaining = end - timing.CurTime;
        }

        var floor = TimeSpan.FromSeconds(30);
        if (remaining < floor)
            status.TrySetStatusEffectDuration(args.TargetEntity, "StatusEffectDrowsiness", floor);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var knockOut = args.EntityManager.System<RMCSizeStunSystem>();
        knockOut.TryKnockOut(args.TargetEntity, TimeSpan.FromSeconds(40));
    }
}
