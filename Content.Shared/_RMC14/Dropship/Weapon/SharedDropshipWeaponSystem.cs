using Content.Shared._RMC14.Map;
using Content.Shared.Shuttles.Components;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using static Content.Shared._RMC14.Dropship.Weapon.DropshipTerminalWeaponsScreen;

namespace Content.Shared._RMC14.Dropship.Weapon;

public abstract class SharedDropshipWeaponSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDropshipSystem _dropship = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        Subs.BuiEvents<DropshipTerminalWeaponsComponent>(DropshipTerminalWeaponsUi.Key,
            subs =>
            {
                subs.Event<DropshipTerminalWeaponsChangeScreenMsg>(OnWeaponsChangeScreenMsg);
                subs.Event<DropshipTerminalWeaponsChooseWeaponMsg>(OnWeaponsChooseWeaponMsg);
                subs.Event<DropshipTerminalWeaponsFireMsg>(OnWeaponsFireMsg);
                subs.Event<DropshipTerminalWeaponsExitMsg>(OnWeaponsExitMsg);
                subs.Event<DropshipTerminalWeaponsCancelMsg>(OnWeaponsCancelMsg);
                subs.Event<DropshipTerminalWeaponsAdjustOffsetMsg>(OnWeaponsAdjustOffset);
            });
    }

    private void OnWeaponsChangeScreenMsg(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsChangeScreenMsg args)
    {
        if (!Enum.IsDefined(args.Screen))
            return;

        ref var screen = ref args.First ? ref ent.Comp.ScreenOne : ref ent.Comp.ScreenTwo;
        screen.State = args.Screen;

        if (args.Screen == StrikeWeapon)
            screen.Weapon = null;

        Dirty(ent);
        RefreshWeaponsUI(ent);
    }

    private void OnWeaponsChooseWeaponMsg(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsChooseWeaponMsg args)
    {
        if (!TryGetEntity(args.Weapon, out var weapon) ||
            !_dropship.IsWeaponAttached(weapon.Value))
        {
            return;
        }

        ref var screen = ref args.First ? ref ent.Comp.ScreenOne : ref ent.Comp.ScreenTwo;
        screen.Weapon = args.Weapon;

        if (screen.State == Equip)
            screen.State = SelectingWeapon;
        else if (screen.State == StrikeWeapon)
            screen.State = Target;

        Dirty(ent);
        RefreshWeaponsUI(ent);
    }

    private void OnWeaponsFireMsg(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsFireMsg args)
    {
        ref var screen = ref args.First ? ref ent.Comp.ScreenOne : ref ent.Comp.ScreenTwo;
        if (screen.Weapon is not { } netWeapon)
            return;

        if (!TryGetEntity(netWeapon, out var weapon) ||
            !TryComp(weapon, out DropshipWeaponComponent? weaponComp))
        {
            screen.Weapon = null;
            Dirty(ent);
            return;
        }

        if (!_dropship.TryGetGridDropship(weapon.Value, out var dropship) ||
            !HasComp<FTLComponent>(dropship))
        {
            return;
        }

        if (ent.Comp.Target is not { } netTarget)
            return;

        if (!TryGetEntity(netTarget, out var target))
        {
            ent.Comp.Target = null;
            Dirty(ent);
            return;
        }

        if (!weaponComp.FireInTransport && !HasComp<DropshipInFlyByComponent>(dropship))
            return;

        var time = _timing.CurTime;
        if (time < weaponComp.NextFireAt)
            return;

        if (!TryGetWeaponAmmo((weapon.Value, weaponComp), out var ammo))
            return;

        if (ammo.Comp.Rounds < ammo.Comp.RoundsPerShot)
            return;

        ammo.Comp.Rounds -= ammo.Comp.RoundsPerShot;
        Dirty(ammo);

        weaponComp.NextFireAt = time + weaponComp.FireDelay;

        Dirty(weapon.Value, weaponComp);
    }

    private void OnWeaponsExitMsg(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsExitMsg args)
    {
        ref var screen = ref args.First ? ref ent.Comp.ScreenOne : ref ent.Comp.ScreenTwo;
        screen.State = Main;

        Dirty(ent);
        RefreshWeaponsUI(ent);
    }

    private void OnWeaponsCancelMsg(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsCancelMsg args)
    {
        ref var screen = ref args.First ? ref ent.Comp.ScreenOne : ref ent.Comp.ScreenTwo;
        screen.State = screen.State switch
        {
            Strike or StrikeWeapon => Target,
            _ => screen.State,
        };

        Dirty(ent);
        RefreshWeaponsUI(ent);
    }

    private void OnWeaponsAdjustOffset(Entity<DropshipTerminalWeaponsComponent> ent, ref DropshipTerminalWeaponsAdjustOffsetMsg args)
    {
        if (!args.Direction.IsCardinal())
            return;

        var adjust = args.Direction.ToIntVec();
        var newOffset = ent.Comp.Offset + adjust;
        var limit = ent.Comp.OffsetLimit;
        newOffset = new Vector2i(
            Math.Clamp(-limit.X, newOffset.X, limit.X),
            Math.Clamp(-limit.Y, newOffset.Y, limit.Y)
        );

        ent.Comp.Offset = newOffset;
        Dirty(ent);
        RefreshWeaponsUI(ent);
    }

    protected virtual void RefreshWeaponsUI(Entity<DropshipTerminalWeaponsComponent> terminal)
    {
    }

    public bool TryGetWeaponAmmo(Entity<DropshipWeaponComponent?> weapon, out Entity<DropshipAmmoComponent> ammo)
    {
        ammo = default;
        if (!Resolve(weapon, ref weapon.Comp, false))
            return false;

        if (!_container.TryGetContainingContainer((weapon, null), out var container) ||
            !TryComp(container.Owner, out DropshipWeaponPointComponent? point) ||
            !_container.TryGetContainer(container.Owner, point.AmmoContainerSlotId, out var ammoContainer))
        {
            return false;
        }

        foreach (var contained in ammoContainer.ContainedEntities)
        {
            if (!TryComp(contained, out DropshipAmmoComponent? ammoComp))
                continue;

            ammo = (contained, ammoComp);
            return true;
        }

        return false;
    }

    public int GetWeaponRounds(Entity<DropshipWeaponComponent?> weapon)
    {
        if (TryGetWeaponAmmo(weapon, out var ammo))
            return ammo.Comp.Rounds;

        return 0;
    }
}
