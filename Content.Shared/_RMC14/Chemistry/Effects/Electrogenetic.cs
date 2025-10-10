using Content.Shared._RMC14.Damage;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects;

public sealed partial class Electrogenetic : RMCChemicalEffect
{
    public static readonly ProtoId<DamageGroupPrototype> BruteGroup = "Brute";
    public static readonly ProtoId<DamageGroupPrototype> BurnGroup = "Burn";
    public static readonly ProtoId<DamageGroupPrototype> ToxinGroup = "Toxin";

    private readonly FixedPoint2 _healPerLevel = 10;
    public FixedPoint2 HealAmount => _healPerLevel * Potency;

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Heals [color=green]{HealAmount}[/color] brute, burn, and toxin damage when defibrillated.\n" +
               $"Removes 1u of this chemical from the solution when defibrillated.";
    }

    public DamageSpecifier CalculateHeal(DamageableSystem damageable, EntityUid target, IEntityManager entityManager)
    {
        var rmcDamageable = entityManager.System<SharedRMCDamageableSystem>();
        var heal = new DamageSpecifier();
        heal = rmcDamageable.DistributeHealingCached(target, BruteGroup, HealAmount, heal);
        heal = rmcDamageable.DistributeHealingCached(target, BurnGroup, HealAmount, heal);
        heal = rmcDamageable.DistributeHealingCached(target, ToxinGroup, HealAmount, heal);
        return heal;
    }
}
