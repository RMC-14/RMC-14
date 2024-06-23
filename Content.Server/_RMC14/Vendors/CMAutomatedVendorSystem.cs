using Content.Shared._RMC14.Vendors;
using Robust.Server.GameObjects;

namespace Content.Server._RMC14.Vendors;

public sealed class CMAutomatedVendorSystem : SharedCMAutomatedVendorSystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    protected override void OnVendBui(Entity<CMAutomatedVendorComponent> vendor, ref CMVendorVendBuiMsg args)
    {
        base.OnVendBui(vendor, ref args);

        var msg = new CMVendorRefreshBuiMsg();
        _ui.ServerSendUiMessage(vendor.Owner, args.UiKey, msg, args.Actor);
    }
}
