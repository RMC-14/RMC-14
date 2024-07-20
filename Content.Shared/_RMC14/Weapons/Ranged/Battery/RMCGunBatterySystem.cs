using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Systems;

namespace Content.Shared._RMC14.Weapons.Ranged.Battery;

public sealed class RMCGunBatterySystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    private EntityQuery<GunDrainBatteryOnShootComponent> _gunDrainBatteryQuery;

    public override void Initialize()
    {
        _gunDrainBatteryQuery = GetEntityQuery<GunDrainBatteryOnShootComponent>();

        SubscribeLocalEvent<GunDrainBatteryOnShootComponent, AttemptShootEvent>(OnDrainBatteryAttemptShoot);
    }

    private void OnDrainBatteryAttemptShoot(Entity<GunDrainBatteryOnShootComponent> ent, ref AttemptShootEvent args)
    {
        if (args.Cancelled || ent.Comp.Powered)
            return;

        args.Cancelled = true;
        _popup.PopupClient(Loc.GetString("rmc-low-power"), args.User, args.User, PopupType.MediumCaution);
    }

    public void SetPowered(Entity<GunDrainBatteryOnShootComponent> gun, bool powered)
    {
        gun.Comp.Powered = powered;
        Dirty(gun);

        if (!gun.Comp.Powered)
        {
            var ev = new GunUnpoweredEvent();
            RaiseLocalEvent(gun, ref ev);
        }
    }

    public void RefreshBatteryDrain(Entity<GunDrainBatteryOnShootComponent?> gun)
    {
        if (!_gunDrainBatteryQuery.Resolve(gun, ref gun.Comp, false))
            return;

        var ev = new GunGetBatteryDrainEvent(gun.Comp.BaseDrain);
        RaiseLocalEvent(gun, ref ev);

        gun.Comp.Drain = ev.Drain;
        Dirty(gun);
    }
}
