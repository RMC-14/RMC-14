using Content.Shared._RMC14.Mobs.Animals;
using Content.Shared.Popups;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCRodentSystem
{
    private void UpdateSleepingRodent(Entity<RMCRodentBehaviorComponent> ent, TimeSpan now)
    {
        if (ent.Comp.SleepUntil <= now ||
            Random.Prob(ent.Comp.WakeChance))
        {
            WakeRodent(ent);
            return;
        }

        if (ent.Comp.NextSnuffleAt > now ||
            !Random.Prob(ent.Comp.SnuffleChance))
        {
            return;
        }

        ent.Comp.NextSnuffleAt = now + ent.Comp.SnuffleCooldown;
        Popup.PopupEntity(Loc.GetString("rmc-rodent-snuffles", ("rodent", ent.Owner)), ent.Owner);
    }

    private void SleepRodent(Entity<RMCRodentBehaviorComponent> ent)
    {
        ent.Comp.Sleeping = true;
        ent.Comp.SleepUntil = Timing.CurTime + RandomTime(ent.Comp.SleepDurationMin, ent.Comp.SleepDurationMax);
        ent.Comp.NextSnuffleAt = Timing.CurTime + RandomTime(TimeSpan.Zero, ent.Comp.SnuffleCooldown);

        StopMovement(ent.Owner);
        RMCNpc.SleepNPC(ent.Owner);
        UpdateRodentVisuals(ent);
    }

    private void WakeRodent(Entity<RMCRodentBehaviorComponent> ent, bool updateAppearance = true)
    {
        if (!ent.Comp.Sleeping)
            return;

        ent.Comp.Sleeping = false;
        ent.Comp.NextThinkAt = Timing.CurTime + ent.Comp.ThinkCooldown;
        RMCNpc.WakeNPC(ent.Owner);

        if (updateAppearance)
            UpdateRodentVisuals(ent);
    }

    private void UpdateRodentVisuals(Entity<RMCRodentBehaviorComponent> ent)
    {
        if (!MobState.IsAlive(ent.Owner))
            return;

        _appearance.SetData(ent.Owner,
            RMCRodentVisuals.State,
            ent.Comp.Sleeping ? ent.Comp.SleepingState : ent.Comp.AwakeState);
    }
}
