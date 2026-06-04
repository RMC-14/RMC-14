using Content.Shared._RMC14.Mobs.Animals;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Humanoid;
using Content.Shared.Mobs.Components;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCGiantLizardSystem
{
    private void UpdateIdleRest(Entity<RMCGiantLizardComponent, TransformComponent> ent)
    {
        if (ent.Comp1.Resting)
        {
            StopMovement(ent.Owner);
            EnsureRestSleep((ent.Owner, ent.Comp1));
            TryHealRestingLizard((ent.Owner, ent.Comp1));
        }

        if (ent.Comp1.NextRestCheckAt > Timing.CurTime)
            return;

        ent.Comp1.NextRestCheckAt = Timing.CurTime + ent.Comp1.RestCheckCooldown;

        if (ent.Comp1.Resting && HasFriendlyMobOnTile(ent))
        {
            ent.Comp1.RestChance = 0;
            return;
        }

        var canStartResting = !WasRecentLizardTime(ent.Comp1.LastHitAt, ent.Comp1.CalmRestDelay) &&
                              !IsOnFire(ent.Owner);

        if (Random.Prob(ent.Comp1.RestChance / 100f))
        {
            if (ent.Comp1.Resting)
                WakeRest((ent.Owner, ent.Comp1));
            else if (canStartResting)
                StartRest((ent.Owner, ent.Comp1));

            ent.Comp1.RestChance = 0;
        }

        AddRestChance(ent.Comp1, Random.NextFloat(ent.Comp1.RestChanceGainMin, ent.Comp1.RestChanceGainMax));
    }

    private void StartRest(Entity<RMCGiantLizardComponent> ent)
    {
        StopMovement(ent.Owner);
        EnsureRestSleep(ent);

        ent.Comp.Resting = true;
        ent.Comp.NextRestHealAt = Timing.CurTime + ent.Comp.RestHealCooldown;
        UpdateLizardVisuals(ent);
    }

    private void WakeRest(Entity<RMCGiantLizardComponent> ent)
    {
        var wasResting = ent.Comp.Resting;
        ent.Comp.Resting = false;
        ent.Comp.RestChance = 0;

        if (ent.Comp.SleepingForRest)
        {
            RMCNpc.WakeNPC(ent.Owner);
            ent.Comp.SleepingForRest = false;
        }

        if (!wasResting)
            return;

        UpdateLizardVisuals(ent);
    }

    private void EnsureRestSleep(Entity<RMCGiantLizardComponent> ent)
    {
        if (ent.Comp.SleepingForRest)
            return;

        RMCNpc.SleepNPC(ent.Owner);
        ent.Comp.SleepingForRest = true;
    }

    private void TryHealRestingLizard(Entity<RMCGiantLizardComponent> ent)
    {
        if (ent.Comp.NextRestHealAt > Timing.CurTime)
            return;

        ent.Comp.NextRestHealAt = Timing.CurTime + ent.Comp.RestHealCooldown;
        if (IsOnFire(ent.Owner) ||
            WasRecentLizardTime(ent.Comp.LastHitAt, ent.Comp.CalmRestDelay))
        {
            return;
        }

        HealFraction(ent.Owner, ent.Comp.RestHealFraction);
        UpdateLizardVisuals(ent);
    }

    private void AddRestChance(RMCGiantLizardComponent comp, float amount)
    {
        if (amount <= 0)
            return;

        comp.RestChance = Math.Clamp(comp.RestChance + amount, 0f, comp.RestChanceMax);
    }

    private bool HasFriendlyMobOnTile(Entity<RMCGiantLizardComponent, TransformComponent> ent)
    {
        var coords = Transform.GetMapCoordinates((ent.Owner, ent.Comp2));
        foreach (var mob in Lookup.GetEntitiesInRange<MobStateComponent>(coords, 0.45f))
        {
            if (mob.Owner == ent.Owner ||
                !MobState.IsAlive(mob.Owner, mob.Comp) ||
                !IsCarbonLikeMob(mob.Owner) ||
                !Faction.IsEntityFriendly(ent.Owner, mob.Owner))
            {
                continue;
            }

            return true;
        }

        return false;
    }

    private bool IsCarbonLikeMob(EntityUid uid)
    {
        return HasComp<HumanoidAppearanceComponent>(uid) ||
               HasComp<XenoComponent>(uid);
    }
}
