using Content.Shared._RMC14.Mobs.Animals;
using Content.Shared.FixedPoint;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCGiantLizardSystem
{
    private bool IsLowHealth(EntityUid uid, RMCGiantLizardComponent comp)
    {
        return TryGetHealthFraction(uid, comp, out var healthFraction) &&
               healthFraction <= comp.LowHealthRetreatFraction;
    }

    private bool TryGetHealthFraction(EntityUid uid, RMCGiantLizardComponent comp, out float healthFraction)
    {
        healthFraction = 1f;
        if (!DamageableQuery.TryComp(uid, out var damageable) ||
            !ThresholdsQuery.TryComp(uid, out var thresholds))
        {
            return false;
        }

        foreach (var (threshold, state) in thresholds.Thresholds)
        {
            if (state != Content.Shared.Mobs.MobState.Dead || threshold <= 0)
                continue;

            healthFraction = Math.Clamp(1f - damageable.Damage.GetTotal().Float() / threshold.Float(), 0f, 1f);
            return true;
        }

        return false;
    }
}
