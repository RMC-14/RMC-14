using Content.Shared.Hands.EntitySystems;
using Content.Shared.Timing;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared._RMC14.Weapons.Ranged;

public sealed class SharedFireGroupSystem : EntitySystem
{
    [Dependency] private readonly UseDelaySystem _delay = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCFireGroupComponent, GunShotEvent>(OnGunShot);
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
    }
}