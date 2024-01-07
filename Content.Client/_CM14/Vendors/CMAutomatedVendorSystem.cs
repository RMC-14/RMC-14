using Content.Shared._CM14.Vendors;
using Robust.Client.GameObjects;

namespace Content.Client._CM14.Vendors;

public sealed class CMAutomatedVendorSystem : SharedCMAutomatedVendorSystem
{
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CMAutomatedVendorComponent, AfterAutoHandleStateEvent>(OnAfterHandleState);
    }

    private void OnAfterHandleState(Entity<CMAutomatedVendorComponent> vendor, ref AfterAutoHandleStateEvent args)
    {
        if (!TryComp(vendor, out UserInterfaceComponent? ui))
            return;

        foreach (var bui in ui.OpenInterfaces.Values)
        {
            if (bui is CMAutomatedVendorBui vendorUi)
                vendorUi.Refresh();
        }
    }
}
