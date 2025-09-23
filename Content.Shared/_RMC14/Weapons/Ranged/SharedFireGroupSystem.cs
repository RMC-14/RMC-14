using Content.Shared.Hands.EntitySystems;
using Content.Shared.Timing;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Weapons.Ranged;

public sealed class SharedFireGroupSystem : EntitySystem
{
    [Dependency] private readonly UseDelaySystem _delay = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCFireGroupComponent, GunShotEvent>(OnGunShot);
        SubscribeLocalEvent<RMCFireGroupComponent, ShotAttemptedEvent>(OnShotAttempt);
    }

    private void OnGunShot(Entity<RMCFireGroupComponent> ent, ref GunShotEvent args)
    {
        var weapon = ent.Owner;
        var comp = ent.Comp;
        var user = args.User;

        foreach (var item in _hands.EnumerateHeld(user))
        {
            if (TryComp<RMCFireGroupComponent>(item, out var fireGroup))
            {
                if (item == weapon)
                    continue;

                if (fireGroup.Group != comp.Group)
                    continue;

                if (!TryComp(item, out UseDelayComponent? useDelay))
                    continue;

                var itemEnt = (item, useDelay);
                _delay.SetLength(itemEnt, comp.Delay, comp.UseDelayID);
                _delay.TryResetDelay(itemEnt, true, id: comp.UseDelayID);
            }
        }

        var userGroup = EnsureComp<RMCUserFireGroupComponent>(args.User);
        userGroup.LastFired[ent.Comp.Group] = _timing.CurTime;
        userGroup.LastGun[ent.Comp.Group] = ent;
        Dirty(args.User, userGroup);
    }

    private void OnShotAttempt(Entity<RMCFireGroupComponent> ent, ref ShotAttemptedEvent args)
    {
        if (args.Cancelled)
            return;

        if (TryComp(ent.Owner, out UseDelayComponent? useDelayComponent) &&
            _delay.IsDelayed((ent.Owner, useDelayComponent), ent.Comp.UseDelayID))
        {
            args.Cancel();
            return;
        }

        if (TryComp(args.User, out RMCUserFireGroupComponent? fireGroup) &&
            fireGroup.LastFired.TryGetValue(ent.Comp.Group, out var last) &&
            _timing.CurTime < last + ent.Comp.Delay &&
            fireGroup.LastGun.GetValueOrDefault(ent.Comp.Group) != ent.Owner)
        {
            args.Cancel();
        }
    }
}
