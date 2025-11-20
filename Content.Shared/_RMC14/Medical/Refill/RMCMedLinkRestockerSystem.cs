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
            if (!TryComp(ent.Owner, out TransformComponent? xform))
                return;

            if (TryGetSupplyLink(ent.Owner, ent.Comp, xform))
            {
                args.PushMarkup(Loc.GetString("rmc-vending-machine-supply-link-connected"));
            }
        }
    }

    protected bool TryGetSupplyLink(EntityUid vendor, RMCMedLinkRestockerComponent restocker, TransformComponent xform)
    {
        if (!restocker.AllowSupplyLinkRestock)
            return false;

        if (!xform.Anchored)
            return false;

        var anchored = _rmcMap.GetAnchoredEntitiesEnumerator(vendor);
        while (anchored.MoveNext(out var anchoredId))
        {
            if (HasComp(anchoredId, typeof(CMMedicalSupplyLinkComponent)))
                return true;
        }

        return false;
    }
}
