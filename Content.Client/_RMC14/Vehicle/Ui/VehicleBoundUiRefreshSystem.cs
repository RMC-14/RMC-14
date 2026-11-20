using Content.Client._RMC14.Vehicle.Supply;
using Content.Shared._RMC14.UserInterface;
using Content.Shared._RMC14.Vehicle;
using Content.Shared._RMC14.Vehicle.Supply;

namespace Content.Client._RMC14.Vehicle.Ui;

public sealed class VehicleBoundUiRefreshSystem : EntitySystem
{
    [Dependency] private readonly RMCUserInterfaceSystem _rmcUI = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<HardpointSlotsComponent, AfterAutoHandleStateEvent>(OnHardpointState);
        SubscribeLocalEvent<VehicleAmmoLoaderComponent, AfterAutoHandleStateEvent>(OnAmmoLoaderState);
        SubscribeLocalEvent<VehicleWeaponsSeatComponent, AfterAutoHandleStateEvent>(OnWeaponsSeatState);
        SubscribeLocalEvent<VehicleSupplyConsoleComponent, AfterAutoHandleStateEvent>(OnSupplyConsoleState);
    }

    private void OnHardpointState(Entity<HardpointSlotsComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        _rmcUI.RefreshUIs<HardpointBoundUserInterface>(ent.Owner);
    }

    private void OnAmmoLoaderState(Entity<VehicleAmmoLoaderComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        _rmcUI.RefreshUIs<VehicleAmmoLoaderBoundUserInterface>(ent.Owner);
    }

    private void OnWeaponsSeatState(Entity<VehicleWeaponsSeatComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        _rmcUI.RefreshUIs<VehicleWeaponsBoundUserInterface>(ent.Owner);
    }

    private void OnSupplyConsoleState(Entity<VehicleSupplyConsoleComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        _rmcUI.RefreshUIs<VehicleSupplyBui>(ent.Owner);
    }
}
