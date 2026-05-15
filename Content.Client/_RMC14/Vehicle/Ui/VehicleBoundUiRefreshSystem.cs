using System;
using Content.Client._RMC14.Vehicle.Supply;
using Content.Shared._RMC14.Vehicle;
using Content.Shared._RMC14.Vehicle.Supply;
using Content.Shared.UserInterface;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Vehicle.Ui;

public sealed class VehicleBoundUiRefreshSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<HardpointSlotsComponent, AfterAutoHandleStateEvent>(OnHardpointState);
        SubscribeLocalEvent<VehicleAmmoLoaderComponent, AfterAutoHandleStateEvent>(OnAmmoLoaderState);
        SubscribeLocalEvent<VehicleWeaponsSeatComponent, AfterAutoHandleStateEvent>(OnWeaponsSeatState);
        SubscribeLocalEvent<VehicleSupplyConsoleComponent, AfterAutoHandleStateEvent>(OnSupplyConsoleState);
    }

    private void OnHardpointState(Entity<HardpointSlotsComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        Refresh<HardpointBoundUserInterface>(ent.Owner, static bui => bui.Refresh(), nameof(HardpointBoundUserInterface));
    }

    private void OnAmmoLoaderState(Entity<VehicleAmmoLoaderComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        Refresh<VehicleAmmoLoaderBoundUserInterface>(ent.Owner, static bui => bui.Refresh(), nameof(VehicleAmmoLoaderBoundUserInterface));
    }

    private void OnWeaponsSeatState(Entity<VehicleWeaponsSeatComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        Refresh<VehicleWeaponsBoundUserInterface>(ent.Owner, static bui => bui.Refresh(), nameof(VehicleWeaponsBoundUserInterface));
    }

    private void OnSupplyConsoleState(Entity<VehicleSupplyConsoleComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        Refresh<VehicleSupplyBui>(ent.Owner, static bui => bui.Refresh(), nameof(VehicleSupplyBui));
    }

    private void Refresh<TBui>(EntityUid uid, Action<TBui> refresh, string uiName) where TBui : BoundUserInterface
    {
        try
        {
            if (!TryComp(uid, out UserInterfaceComponent? ui))
                return;

            foreach (var bui in ui.ClientOpenInterfaces.Values)
            {
                if (bui is TBui typed)
                    refresh(typed);
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error refreshing {uiName}\n{e}");
        }
    }
}
