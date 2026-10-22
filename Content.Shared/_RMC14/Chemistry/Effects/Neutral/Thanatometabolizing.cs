using Content.Shared._RMC14.Body;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.EntityEffects;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Thanatometabolizing : RMCChemicalEffect
{
    private static readonly ProtoId<DamageTypePrototype> AsphyxiationType = "Asphyxiation";

    private const float BloodVolumeNormal = 560f;
    private const float BloodVolumeOkay = 336f;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "Requires either low oxygen levels or low blood flow to function. Potency affects the efficiency of other properties in the mix.";
    }

    protected override bool ShouldCancel(EntityEffectReagentArgs args)
    {
        if (IsDead(args))
            return false;

        return GetOxyLoss(args) < 50f && GetBloodVolume(args) > BloodVolumeOkay;
    }

    protected override void ReagentEffectiveness(EntityEffectReagentArgs args, ref float effectiveness)
    {
        // cm13 adds this onto the base effectiveness of 1 while alive, but ORs it into 1 in the dead path
        if (IsDead(args))
            return;

        var severity = MathF.Max(GetOxyLoss(args) / 10f, (BloodVolumeNormal - GetBloodVolume(args)) / BloodVolumeNormal);
        effectiveness += Math.Clamp(severity * 0.1f * Potency, 0.1f, 1f);
    }

    private static bool IsDead(EntityEffectReagentArgs args)
    {
        return args.EntityManager.TryGetComponent<MobStateComponent>(args.TargetEntity, out var mobState) &&
               args.EntityManager.System<MobStateSystem>().IsDead(args.TargetEntity, mobState);
    }

    private static float GetOxyLoss(EntityEffectReagentArgs args)
    {
        if (!args.EntityManager.TryGetComponent<DamageableComponent>(args.TargetEntity, out var damageable))
            return 0f;

        return damageable.Damage.DamageDict.TryGetValue(AsphyxiationType, out var oxyLoss) ? (float) oxyLoss : 0f;
    }

    private static float GetBloodVolume(EntityEffectReagentArgs args)
    {
        var bloodstream = args.EntityManager.System<SharedRMCBloodstreamSystem>();
        return bloodstream.TryGetBloodSolution(args.TargetEntity, out var solution) ? (float) solution.Volume : BloodVolumeNormal;
    }
}
