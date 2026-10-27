using Content.Shared._RMC14.Mobs.Animals;
using Content.Shared.FixedPoint;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCGiantLizardSystem
{
    private void UpdateTongueFlick(Entity<RMCGiantLizardComponent> ent)
    {
        var now = Timing.CurTime;
        if (ent.Comp.TongueVisible && ent.Comp.TongueFlickEndAt <= now)
        {
            ent.Comp.TongueVisible = false;
            _appearance.SetData(ent.Owner, RMCGiantLizardVisuals.Tongue, false);
        }

        if (ent.Comp.NextTongueFlickAt > now ||
            ent.Comp.Resting ||
            _standing.IsDown(ent.Owner) ||
            !MobState.IsAlive(ent.Owner))
        {
            return;
        }

        ent.Comp.NextTongueFlickAt = now + ent.Comp.TongueFlickCooldown;
        if (!Random.Prob(ent.Comp.TongueFlickChance))
            return;

        ShowTongueFlick(ent);
    }

    private void ShowTongueFlick(Entity<RMCGiantLizardComponent> ent)
    {
        ent.Comp.NextTongueFlickAt = Timing.CurTime + ent.Comp.TongueFlickCooldown;
        ent.Comp.TongueVisible = true;
        ent.Comp.TongueFlickEndAt = Timing.CurTime + ent.Comp.TongueFlickDuration;
        _appearance.SetData(ent.Owner, RMCGiantLizardVisuals.Tongue, true);
    }

    private void UpdateLizardVisuals(Entity<RMCGiantLizardComponent> ent)
    {
        var body = RMCGiantLizardBodyVisual.Running;
        if (!MobState.IsAlive(ent.Owner))
            body = RMCGiantLizardBodyVisual.Dead;
        else if (_standing.IsDown(ent.Owner))
            body = ent.Comp.Resting ? RMCGiantLizardBodyVisual.Sleeping : RMCGiantLizardBodyVisual.KnockedDown;
        else if (ent.Comp.Resting)
            body = RMCGiantLizardBodyVisual.Sleeping;

        _appearance.SetData(ent.Owner, RMCGiantLizardVisuals.Body, body);
        _appearance.SetData(ent.Owner, RMCGiantLizardVisuals.Wounds, GetWoundVisual(ent));
    }

    private RMCGiantLizardWoundVisual GetWoundVisual(Entity<RMCGiantLizardComponent> ent)
    {
        if (!MobState.IsAlive(ent.Owner) ||
            !DamageableQuery.TryComp(ent.Owner, out var damageable) ||
            !ThresholdsQuery.TryComp(ent.Owner, out var thresholds))
        {
            return RMCGiantLizardWoundVisual.None;
        }

        var maxHealth = 0f;
        foreach (var (threshold, state) in thresholds.Thresholds)
        {
            if (state == Content.Shared.Mobs.MobState.Dead && threshold > 0)
            {
                maxHealth = threshold.Float();
                break;
            }
        }

        if (maxHealth <= 0)
            return RMCGiantLizardWoundVisual.None;

        var healthFraction = 1f - damageable.Damage.GetTotal().Float() / maxHealth;
        if (healthFraction > ent.Comp.SmallWoundHealthFraction)
            return RMCGiantLizardWoundVisual.None;

        var big = healthFraction <= ent.Comp.BigWoundHealthFraction;
        if (_standing.IsDown(ent.Owner) && !ent.Comp.Resting)
            return big ? RMCGiantLizardWoundVisual.BigStun : RMCGiantLizardWoundVisual.SmallStun;

        if (ent.Comp.Resting)
            return big ? RMCGiantLizardWoundVisual.BigRest : RMCGiantLizardWoundVisual.SmallRest;

        return big ? RMCGiantLizardWoundVisual.Big : RMCGiantLizardWoundVisual.Small;
    }
}
