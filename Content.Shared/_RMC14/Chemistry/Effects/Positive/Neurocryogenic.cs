using Content.Shared._RMC14.Chemistry.Buildup;
using Content.Shared._RMC14.Medical.Unrevivable;
using Content.Shared._RMC14.Temperature;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Humanoid;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Neurocryogenic : RMCChemicalEffect
{
    public override bool ReactsOnTouch => true;

    private const float CryoLiquidThreshold = 210f;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Causes a temporal freeze of neurological processes in the brain, preserving it for long periods of time.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (IsReaction(args))
        {
            HandleReaction(args);
            return;
        }

        var entities = args.EntityManager;
        var target = args.TargetEntity;

        var random = IoCManager.Resolve<IRobustRandom>();
        if (random.Prob(0.1f))
            entities.System<SharedPopupSystem>().PopupEntity(Loc.GetString("chem-neurocryogenic-brain-freeze"), target, target);

        var stun = entities.System<SharedStunSystem>();
        stun.TryKnockdown(target, TimeSpan.FromSeconds(40), true);
        stun.TryStun(target, TimeSpan.FromSeconds(40), true);
    }

    protected override bool TickDead(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (!args.EntityManager.HasComponent<HumanoidAppearanceComponent>(args.TargetEntity))
            return false;

        var unrevivable = args.EntityManager.System<RMCUnrevivableSystem>();
        unrevivable.AddRevivableTime(args.TargetEntity, TimeSpan.FromSeconds((double) potency * 5));
        return true;
    }

    private void HandleReaction(EntityEffectReagentArgs args)
    {
        var entities = args.EntityManager;
        var target = args.TargetEntity;

        var isXenoHuman = entities.HasComponent<XenoComponent>(target) ||
            entities.HasComponent<HumanoidAppearanceComponent>(target);
        if (!isXenoHuman)
            return;

        var volume = (float) args.Quantity;
        var magnitude = (float) ActualPotency * volume * 0.5f;
        var buildup = entities.System<RMCSpeedBuildupSystem>();
        buildup.AddBuildup(target, magnitude, dissipationRate: 0.4f, maxBuildup: 10f, increaseSpeed: false);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var temperature = args.EntityManager.System<SharedRMCTemperatureSystem>();
        var newTemperature = MathF.Max(CryoLiquidThreshold, temperature.GetTemperature(args.TargetEntity) - (float) potency * 5f);
        temperature.ForceChangeTemperature(args.TargetEntity, newTemperature);
    }

    // TODO RMC14: critical brain damage - 2.5 * potency * delta_time (5 * potency per 2s tick)
}
