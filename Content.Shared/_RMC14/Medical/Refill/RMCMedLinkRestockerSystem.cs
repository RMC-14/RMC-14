using Content.Shared._RMC14.Map;
using Content.Shared.Examine;

namespace Content.Shared._RMC14.Medical.Refill;

public abstract class RMCMedLinkRestockerSystem : EntitySystem
{
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCMedLinkRestockerComponent, ExaminedEvent>(OnVendorRefillerExamined);
    }

    private void OnVendorRefillerExamined(Entity<RMCMedLinkRestockerComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(RMCMedLinkRestockerComponent)))
        {
            if (TryGetSupplyLink(ent))
            {
                args.PushMarkup(Loc.GetString("rmc-vending-machine-supply-link-connected"));
            }
        }
    }

    protected bool TryGetSupplyLink(Entity<RMCMedLinkRestockerComponent> vendor)
    {
        if (!vendor.Comp.AllowSupplyLinkRestock)
            return false;
        if (!TryComp<TransformComponent>(vendor, out var xform) || !xform.Anchored)
            return false;

        var anchored = _rmcMap.GetAnchoredEntitiesEnumerator(vendor);
        while (anchored.MoveNext(out var anchoredId))
        {
            if (HasComp<CMMedicalSupplyLinkComponent>(anchoredId))
                return true;
        }

        return false;
    }
}
