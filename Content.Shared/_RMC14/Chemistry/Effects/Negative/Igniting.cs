using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Weapons.Ranged.Flamer;
using Content.Shared.Atmos.Components;
using Content.Shared.Damage;
using Content.Shared.EntityEffects;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Negative;

public sealed partial class Igniting : RMCChemicalEffect
{
    private const int UTNapthalIntensity = 30;
    private const int UTNapthalDuration = 20;
    private static readonly Color UTNapthalBurnColor = Color.FromHex("#EE6515");

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return "The chemical is capable of self-igniting on contact with most materials.";
    }

    protected override void Tick(DamageableSystem damageable, FixedPoint2 potency, EntityEffectReagentArgs args)
    {
        if (!args.EntityManager.TryGetComponent<FlammableComponent>(args.TargetEntity, out var flammable))
            return;

        var target = args.TargetEntity;
        var flammableSystem = args.EntityManager.System<SharedRMCFlammableSystem>();

        // cm13 igniting/process(): every tick while metabolizing, raises fire stacks by
        // potency*30 (additive doubles existing stacks if already higher) & ignites
        var boost = MathF.Max(flammable.FireStacks, ActualPotency * 30f);

        if (flammable.OnFire)
        {
            var fireDuration = flammable.Duration > 0 ? flammable.Duration : UTNapthalDuration;
            var stacks = MathF.Min(flammable.FireStacks + boost, MathF.Min(fireDuration, flammable.MaximumFireStacks));
            flammableSystem.AdjustStacks((target, flammable), stacks - flammable.FireStacks);
            return;
        }

        var intensity = UTNapthalIntensity;
        var duration = UTNapthalDuration;
        if (args.Reagent is { } reagent)
        {
            var stats = args.EntityManager.System<SharedRMCFlamerSystem>()
                .GetFireStats(reagent, UTNapthalIntensity, UTNapthalDuration);
            intensity = Math.Max(stats.Intensity, 1);
            duration = Math.Max(stats.Duration, 1);
        }

        var newStacks = MathF.Min(flammable.FireStacks + boost, duration);
        if (!flammableSystem.Ignite((target, flammable), intensity, duration, (int) newStacks))
            return;

        flammableSystem.ChangeBurnColor(target, args.Reagent?.SubstanceColor ?? UTNapthalBurnColor);

        var popup = args.EntityManager.System<SharedPopupSystem>();
        popup.PopupEntity(Loc.GetString("chem-igniting-burns"), target, target, PopupType.LargeCaution);
    }
}
