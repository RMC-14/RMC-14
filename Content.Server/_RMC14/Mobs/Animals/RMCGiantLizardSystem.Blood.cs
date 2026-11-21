using Content.Shared._RMC14.Body;
using Content.Shared._RMC14.Mobs.Animals;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCGiantLizardSystem
{
    [Dependency] private readonly SharedPuddleSystem _puddle = default!;
    [Dependency] private readonly SharedRMCBloodstreamSystem _rmcBloodstream = default!;

    private static readonly string[] BruteLikeDamageTypes =
    [
        "Brute",
        "Blunt",
        "Slash",
        "Piercing",
    ];

    private void TryStartBleedTrail(Entity<RMCGiantLizardComponent> ent, DamageSpecifier? damage)
    {
        if (damage == null)
            return;

        var brute = GetPositiveBruteLikeDamage(damage);
        if (brute < ent.Comp.BleedTrailDamageThreshold)
            return;

        var ticks = (int) MathF.Ceiling(brute.Float() / ent.Comp.BleedTrailDamageDivisor);
        ent.Comp.BleedTrailTicks = Math.Clamp(ent.Comp.BleedTrailTicks + ticks, 0, ent.Comp.BleedTrailMaxTicks);

        SpillLizardBlood(ent, damage.DamageDict.GetValueOrDefault("Piercing") > FixedPoint2.Zero);
    }

    private void UpdateBleedTrail(Entity<RMCGiantLizardComponent> ent)
    {
        if (ent.Comp.BleedTrailTicks <= 0 ||
            ent.Comp.NextBleedTrailAt > Timing.CurTime)
        {
            return;
        }

        ent.Comp.NextBleedTrailAt = Timing.CurTime + ent.Comp.BleedTrailCooldown;

        var large = ent.Comp.BleedTrailTicks >= ent.Comp.BleedTrailSmallTicks;
        ent.Comp.BleedTrailTicks--;
        SpillLizardBlood(ent, large);
    }

    private FixedPoint2 GetPositiveBruteLikeDamage(DamageSpecifier damage)
    {
        var total = FixedPoint2.Zero;
        foreach (var type in BruteLikeDamageTypes)
        {
            if (damage.DamageDict.TryGetValue(type, out var amount) &&
                amount > FixedPoint2.Zero)
            {
                total += amount;
            }
        }

        return total;
    }

    private bool SpillLizardBlood(Entity<RMCGiantLizardComponent> ent, bool large)
    {
        if (!_rmcBloodstream.TryGetBloodSolution(ent.Owner, out var bloodSolution) ||
            bloodSolution.Volume <= FixedPoint2.Zero)
        {
            return false;
        }

        var copy = new Solution(bloodSolution);
        var amount = large ? ent.Comp.BleedTrailLargeVolume : ent.Comp.BleedTrailSmallVolume;
        var spill = copy.SplitSolution(FixedPoint2.Min(amount, copy.Volume));
        if (spill.Volume <= FixedPoint2.Zero)
            return false;

        return _puddle.TrySpillAt(ent.Owner, spill, out _, sound: false);
    }
}
