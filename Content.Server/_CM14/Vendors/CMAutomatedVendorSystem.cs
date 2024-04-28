using Content.Shared._CM14.Vendors;
using Robust.Server.GameObjects;

namespace Content.Server._CM14.Vendors;

public sealed class CMAutomatedVendorSystem : SharedCMAutomatedVendorSystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    protected override void OnVendBui(Entity<CMAutomatedVendorComponent> vendor, ref CMVendorVendBuiMessage args)
    {
        base.OnVendBui(vendor, ref args);

        var msg = new CMVendorRefreshBuiMessage();
        _ui.ServerSendUiMessage(vendor.Owner, args.UiKey, msg, args.Actor);
    }
}
