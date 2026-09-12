using Content.Shared._RMC14.Mobs.Animals;
using Content.Shared.FixedPoint;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCGiantLizardSystem
{
    private void HealFraction(EntityUid uid, float fraction)
    {
        if (!DamageableQuery.TryComp(uid, out var damageable) ||
            !ThresholdsQuery.TryComp(uid, out var thresholds))
            return;

        var currentDamage = damageable.Damage.GetTotal();
        if (currentDamage <= FixedPoint2.Zero)
            return;

        FixedPoint2 maxHealth = FixedPoint2.Zero;
        foreach (var (threshold, state) in thresholds.Thresholds)
        {
            if (state == Content.Shared.Mobs.MobState.Dead && threshold > FixedPoint2.Zero)
            {
                maxHealth = threshold;
                break;
            }
        }

        if (maxHealth <= FixedPoint2.Zero)
            return;

        var healing = FixedPoint2.New(maxHealth.Float() * Math.Clamp(fraction, 0f, 1f));
        if (healing > currentDamage)
            healing = currentDamage;

        var healingRatio = healing.Float() / currentDamage.Float();
        var healingDamage = damageable.Damage * healingRatio;

        Damageable.TryChangeDamage(
            uid,
            -healingDamage,
            true,
            interruptsDoAfters: false,
            origin: uid);
    }
}
