using Content.Client._RMC14.Vehicle.Supply;
using Content.Shared._RMC14.UserInterface;
using Content.Shared._RMC14.Vehicle;
using Content.Shared._RMC14.Vehicle.Supply;
using Content.Shared.Buckle.Components;
using Robust.Client.Player;

namespace Content.Client._RMC14.Vehicle.Ui;

public sealed class VehicleBoundUiRefreshSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly RMCUserInterfaceSystem _rmcUI = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HardpointSlotsComponent, AfterAutoHandleStateEvent>(OnHardpointState);
        SubscribeLocalEvent<VehicleAmmoLoaderComponent, AfterAutoHandleStateEvent>(OnAmmoLoaderState);
        SubscribeLocalEvent<VehicleHudSeatComponent, AfterAutoHandleStateEvent>(OnHudSeatState);
        SubscribeLocalEvent<VehicleSupplyConsoleComponent, AfterAutoHandleStateEvent>(OnSupplyConsoleState);
        SubscribeLocalEvent<VehicleSafetyComponent, AfterAutoHandleStateEvent>(OnSafetyState);
    }

    private void OnHardpointState(Entity<HardpointSlotsComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        _rmcUI.RefreshUIs<HardpointBoundUserInterface>(ent.Owner);
    }

    private void OnAmmoLoaderState(Entity<VehicleAmmoLoaderComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        _rmcUI.RefreshUIs<VehicleAmmoLoaderBoundUserInterface>(ent.Owner);
    }

    private void OnHudSeatState(Entity<VehicleHudSeatComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        _rmcUI.RefreshUIs<VehicleWeaponsBoundUserInterface>(ent.Owner);
    }

    private void OnSupplyConsoleState(Entity<VehicleSupplyConsoleComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        _rmcUI.RefreshUIs<VehicleSupplyBui>(ent.Owner);
    }

    private void OnSafetyState(Entity<VehicleSafetyComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (_player.LocalEntity is not { } user ||
            !TryComp(user, out BuckleComponent? buckle) ||
            buckle.BuckledTo is not { } seat ||
            !HasComp<VehicleHudSeatComponent>(seat))
        {
            return;
        }

        _rmcUI.RefreshUIs<VehicleWeaponsBoundUserInterface>(seat);
    }
}
