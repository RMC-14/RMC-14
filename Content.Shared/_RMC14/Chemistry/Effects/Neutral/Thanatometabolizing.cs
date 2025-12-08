using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Chemistry.Effects.Neutral;

public sealed partial class Thanatometabolizing : RMCChemicalEffect
{
    public override string Abbreviation => "TMB";

    protected override string ReagentEffectGuidebookText(IPrototypeManager prototype, IEntitySystemManager entSys)
    {
        return $"Requires the user to be dead, have 50 asphyxiation damage or more, or have 60% blood or less in order to metabolize.\n" +
               $"Modifies the effectiveness of the chemical by the inverse of the user's blood percentage, or their asphyxiation damage divided by 10, whichever one is bigger, multiplied by 0.1 and the potency of this property, clamped between 0.1 and 1.";
    }

    public override bool CanMetabolize(EntityUid target)
    {
        return CanFunction(target, out _, out _);
    }

    public override FixedPoint2 GetEffectiveness(EntityUid target)
    {
        if (!CanFunction(target, out var airlossDamage, out var bloodPercentage))
            return FixedPoint2.New(1);

        var inverseBlood = FixedPoint2.New(1) - FixedPoint2.New(bloodPercentage);
        return FixedPoint2.Clamp(FixedPoint2.Max(airlossDamage / 10f, inverseBlood) * 0.1 * Level, 0.1, 1);
    }

    private bool CanFunction(EntityUid target, out FixedPoint2 airlossDamage, out float bloodPercentage)
    {
        airlossDamage = FixedPoint2.Zero;
        bloodPercentage = 0;

        var entities = IoCManager.Resolve<IEntityManager>();
        var prototypes = IoCManager.Resolve<IPrototypeManager>();
        var mobStateSys = entities.System<MobStateSystem>();
        var isDead = mobStateSys.IsDead(target);
        if (entities.TryGetComponent(target, out DamageableComponent? damageable) &&
            prototypes.TryIndex(AirlossGroup, out var airlossProto) &&
            damageable.Damage.TryGetDamageInGroup(airlossProto, out var entAirlossDamage))
        {
            airlossDamage = entAirlossDamage;
        }

        bloodPercentage = entities.System<SharedBloodstreamSystem>().GetBloodLevelPercentage(target);
        return isDead || airlossDamage >= 50 || bloodPercentage <= 0.6;
    }
}
