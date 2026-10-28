using Content.Shared._RMC14.Chemistry.Buildup;
using Content.Shared._RMC14.Stun;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Disrupting : RMCChemicalEffect
{
    public override bool ReactsOnTouch => true;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Disrupts certain neurological processes related to communication in animals.";
    }

    // TODO RMC14: brain damage overdose - potency brain organ damage per 2s tick

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (IsReaction(args))
        {
            HandleReaction(args);
            return;
        }

        var random = IoCManager.Resolve<IRobustRandom>();
        if (random.Prob(0.5f))
        {
            var popup = args.EntityManager.System<SharedPopupSystem>();
            popup.PopupEntity(Loc.GetString("chem-disrupting-quiet"), args.TargetEntity, args.TargetEntity);
        }
    }

    private void HandleReaction(EntityEffectReagentArgs args)
    {
        var entities = args.EntityManager;
        var target = args.TargetEntity;
        if (!entities.HasComponent<XenoComponent>(target))
            return;

        var volume = (float) args.Quantity;
        var magnitude = volume * (float) ActualPotency * 1.2f;
        var interference = entities.System<RMCHivemindInterferenceSystem>();
        interference.AddBuildup(target, magnitude, 1f, 90f);
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var knockOut = args.EntityManager.System<RMCSizeStunSystem>();
        knockOut.TryKnockOut(args.TargetEntity, TimeSpan.FromSeconds(ActualPotency * 2f));
    }
}
