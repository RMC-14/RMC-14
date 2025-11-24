using Content.Shared._RMC14.Vendors;
using Content.Server.PowerCell;
using Content.Shared.PowerCell.Components;
using Robust.Server.GameObjects;

namespace Content.Server._RMC14.Vendors;

public sealed class CMAutomatedVendorSystem : SharedCMAutomatedVendorSystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;

    protected override void OnVendBui(Entity<CMAutomatedVendorComponent> vendor, ref CMVendorVendBuiMsg args)
    {
        base.OnVendBui(vendor, ref args);

        var msg = new CMVendorRefreshBuiMsg();
        _ui.ServerSendUiMessage(vendor.Owner, args.UiKey, msg, args.Actor);
    }

    protected override (float currentCharge, float maxCharge) GetBatteryCharge(EntityUid item, PowerCellSlotComponent powerCellSlot)
    {
        return _powerCell.TryGetBatteryFromSlot(item, out var battery, powerCellSlot)
            ? (battery.CurrentCharge, battery.MaxCharge)
            : (0, 0);
    }
}
