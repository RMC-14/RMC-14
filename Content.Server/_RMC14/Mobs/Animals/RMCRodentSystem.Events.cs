using Content.Shared._RMC14.Mobs.Animals;
using Content.Shared.Damage;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Robust.Shared.Physics.Events;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCRodentSystem
{
    private void OnMapInit(Entity<RMCRodentBehaviorComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.NextThinkAt = Timing.CurTime + RandomTime(TimeSpan.Zero, ent.Comp.ThinkCooldown);
        ent.Comp.SleepUntil = TimeSpan.Zero;
        UpdateRodentVisuals(ent);
    }

    private void OnDamageChanged(Entity<RMCRodentBehaviorComponent> ent, ref DamageChangedEvent args)
    {
        if (args.DamageIncreased)
            WakeRodent(ent);
    }

    private void OnMobStateChanged(Entity<RMCRodentBehaviorComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != Content.Shared.Mobs.MobState.Alive)
        {
            WakeRodent(ent, false);
            return;
        }

        UpdateRodentVisuals(ent);
    }

    private void OnStartCollide(Entity<RMCRodentBehaviorComponent> ent, ref StartCollideEvent args)
    {
        if (!MobState.IsAlive(ent.Owner))
            return;

        if (ent.Comp.Sleeping)
            WakeRodent(ent);

        if (ent.Comp.NextSqueakAt > Timing.CurTime ||
            !MobQuery.TryComp(args.OtherEntity, out var otherMob) ||
            !MobState.IsAlive(args.OtherEntity, otherMob) ||
            !Random.Prob(ent.Comp.SqueakOnCollideChance))
        {
            return;
        }

        ent.Comp.NextSqueakAt = Timing.CurTime + ent.Comp.SqueakCooldown;
        _audio.PlayPvs(ent.Comp.SqueakSound, ent.Owner);
        Popup.PopupEntity(Loc.GetString("rmc-rodent-squeaks", ("rodent", ent.Owner)), ent.Owner, args.OtherEntity);
    }
}
