using Content.Shared._CM14.Medical.Refill;
using Content.Shared._CM14.Vendors;

namespace Content.Client._CM14.Vendors;

public sealed class CMAutomatedVendorSystem : SharedCMAutomatedVendorSystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CMAutomatedVendorComponent, AfterAutoHandleStateEvent>(OnRefresh);
        SubscribeLocalEvent<CMSolutionRefillerComponent, AfterAutoHandleStateEvent>(OnRefresh);
    }

    private void OnRefresh<T>(Entity<T> ent, ref AfterAutoHandleStateEvent args) where T : IComponent?
    {
        if (!TryComp(ent, out UserInterfaceComponent? ui))
            return;

        foreach (var bui in ui.ClientOpenInterfaces.Values)
        {
            if (bui is CMAutomatedVendorBui vendorUi)
                vendorUi.Refresh();
        }
    }
}
