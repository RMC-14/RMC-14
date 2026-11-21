using Content.Shared._RMC14.Mobs.Animals;
using Content.Shared.Damage;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCBatSystem
{
    private void OnMapInit(Entity<RMCBatHangingComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextCheckAt = Timing.CurTime + RandomTime(TimeSpan.Zero, ent.Comp.CheckCooldown);
        ConfigureIdleBlackboard(ent);
        StabilizeBat(ent.Owner);
    }

    private void OnDamageChanged(Entity<RMCBatHangingComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageIncreased)
            WakeBat(ent);
    }

    private void ConfigureIdleBlackboard(Entity<RMCBatHangingComponent> ent)
    {
        _npc.SetBlackboard(ent.Owner, "IdleRange", ent.Comp.IdleRange);
        _npc.SetBlackboard(ent.Owner, "MinimumIdleTime", ent.Comp.MinimumIdleTime);
        _npc.SetBlackboard(ent.Owner, "MaximumIdleTime", ent.Comp.MaximumIdleTime);
    }
}
