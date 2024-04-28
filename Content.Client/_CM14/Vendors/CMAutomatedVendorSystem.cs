using Content.Shared._CM14.Vendors;

namespace Content.Client._CM14.Vendors;

public sealed class CMAutomatedVendorSystem : SharedCMAutomatedVendorSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CMAutomatedVendorComponent, AfterAutoHandleStateEvent>(OnAfterHandleState);
    }

    private void OnAfterHandleState(Entity<CMAutomatedVendorComponent> vendor, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp(vendor, out UserInterfaceComponent? ui))
            return;

        foreach (var bui in ui.ClientOpenInterfaces.Values)
        {
            if (bui is CMAutomatedVendorBui vendorUi)
                vendorUi.Refresh();
        }
    }
}
