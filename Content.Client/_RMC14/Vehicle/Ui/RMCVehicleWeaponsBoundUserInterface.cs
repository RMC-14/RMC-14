using System;
using System.Numerics;
using Content.Shared._RMC14.Vehicle;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Vehicle.Ui;

public sealed class RMCVehicleWeaponsBoundUserInterface : BoundUserInterface
{
    private RMCVehicleWeaponsMenu? _menu;

    public RMCVehicleWeaponsBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        Logger.Info($"[RMCVehicleWeapons:UI] Open Owner={EntMan.ToPrettyString(Owner)}");
        _menu = new RMCVehicleWeaponsMenu();
        _menu.OnClose += Close;

        var metaQuery = EntMan.GetEntityQuery<MetaDataComponent>();
        if (metaQuery.TryGetComponent(Owner, out var metadata))
            _menu.Title = metadata.EntityName;

        _menu.OnSelect += slotId => SendMessage(new RMCVehicleWeaponsSelectMessage(slotId));
        _menu.OpenCenteredAt(new Vector2(0.1f, 0.9f));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        Logger.Info($"[RMCVehicleWeapons:UI] Dispose Owner={EntMan.ToPrettyString(Owner)}");
        if (_menu != null)
            _menu.OnClose -= Close;

        _menu?.Dispose();
        _menu = null;
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not RMCVehicleWeaponsUiState weaponsState)
        {
            Logger.Info($"[RMCVehicleWeapons:UI] UpdateState unexpected state {state?.GetType().Name ?? "null"}");
            return;
        }

        Logger.Info($"[RMCVehicleWeapons:UI] UpdateState entries={weaponsState.Hardpoints.Count}");
        _menu?.Update(weaponsState.Hardpoints);
    }
}
