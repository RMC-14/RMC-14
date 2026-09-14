using Content.Shared._RMC14.Body;
using Content.Shared._RMC14.Botany;
using Content.Shared._RMC14.Damage;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.StatusEffectNew;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Positive;

public sealed partial class Antitoxic : RMCChemicalEffect
{
    private static readonly ProtoId<DamageGroupPrototype> ToxinGroup = "Toxin";
    private static readonly ProtoId<DamageGroupPrototype> GeneticGroup = "Genetic";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        var healing = PotencyPerSecond * 2;
        return $"Heals [color=green]{healing}[/color] toxin damage and removes [color=green]0.125[/color] units of toxic chemicals from the bloodstream.\n" +
               $"Overdoses cause eye damage.\n" +
               $"Critical overdoses cause 30 seconds of drowsiness.";
    }

    protected override void TickHydroTray(Entity<RMCPlantComponent> plant, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (plant.Comp.Tray is not { } trayUid || GetTray(args.EntityManager, plant) is not { } tray || tray.Toxins <= 0)
            return;

        args.EntityManager.System<SharedRMCPlantTraySystem>()
            .SetToxins((trayUid, tray), MathF.Max(0f, tray.Toxins - 1.5f * Potency));
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var rmcDamageable = args.EntityManager.System<SharedRMCDamageableSystem>();
        var healing = rmcDamageable.DistributeHealingCached(args.TargetEntity, ToxinGroup, potency * 2f);

        // TODO RMC14 remove genetic heal once other meds are in for genetic damage
        healing = rmcDamageable.DistributeHealingCached(args.TargetEntity, GeneticGroup, potency * 2f, healing);
        damageable.TryChangeDamage(args.TargetEntity, healing, true, interruptsDoAfters: false);

        var bloodstream = args.EntityManager.System<SharedRMCBloodstreamSystem>();
        bloodstream.RemoveBloodstreamToxins(args.TargetEntity, 0.125f);
    }

    protected override void TickOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        // TODO RMC14: damge eye organ instead
        var blinding = args.EntityManager.System<BlindableSystem>();
        blinding.AdjustEyeDamage(args.TargetEntity, (int) MathF.Round((float) potency));
    }

    protected override void TickCriticalOverdose(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        var status = args.EntityManager.System<SharedStatusEffectsSystem>();
        status.TryAddStatusEffectDuration(args.TargetEntity, "StatusEffectDrowsiness", TimeSpan.FromSeconds(30));
    }
}
