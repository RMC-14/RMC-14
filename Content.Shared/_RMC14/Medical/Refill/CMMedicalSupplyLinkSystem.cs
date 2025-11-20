using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Vendors;
using Content.Shared.Examine;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Medical.Refill;

public abstract class CMMedicalSupplyLinkSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CMMedicalSupplyLinkComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CMAutomatedVendorComponent, AnchorStateChangedEvent>(OnVendorAnchorChanged);
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

    private void OnMapInit(Entity<CMMedicalSupplyLinkComponent> ent, ref MapInitEvent args)
    {
        UpdateLinkState(ent);
    }

    private void OnVendorAnchorChanged(Entity<CMAutomatedVendorComponent> vendor, ref AnchorStateChangedEvent args)
    {
        var anchored = _rmcMap.GetAnchoredEntitiesEnumerator(vendor);
        while (anchored.MoveNext(out var anchoredId))
        {
            if (!TryComp<CMMedicalSupplyLinkComponent>(anchoredId, out var link))
                continue;
            UpdateLinkState((anchoredId, link), args.Anchored);
            return;
        }
    }

    private void UpdateLinkState(Entity<CMMedicalSupplyLinkComponent> link, bool? playAnimation = null)
    {
        var hasVendor = false;
        var anchored = _rmcMap.GetAnchoredEntitiesEnumerator(link);
        while (anchored.MoveNext(out var anchoredId))
        {
            if (!HasComp<CMAutomatedVendorComponent>(anchoredId))
                continue;
            hasVendor = true;
            break;
        }

        var baseState = link.Comp.BaseState;
        if (playAnimation != null)
        {
            if (playAnimation.Value)
            {
                _appearance.SetData(link, CMMedicalSupplyLinkVisuals.State, $"{baseState}_clamping");
                var animationLength = 2.6f;
                link.Comp.UpdateStateAt = _timing.CurTime + TimeSpan.FromSeconds(animationLength);
                Dirty(link);
            }
            else
            {
                _appearance.SetData(link, CMMedicalSupplyLinkVisuals.State, $"{baseState}_unclamping");
                var animationLength = 2.6f;
                link.Comp.UpdateStateAt = _timing.CurTime + TimeSpan.FromSeconds(animationLength);
                Dirty(link);
            }
        }
        else
        {
            _appearance.SetData(link, CMMedicalSupplyLinkVisuals.State, hasVendor ? $"{baseState}_clamped" : $"{baseState}_unclamped");
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<CMMedicalSupplyLinkComponent>();
        while (query.MoveNext(out var uid, out var link))
        {
            if (link.UpdateStateAt is not { } updateAt)
                continue;

            if (curTime < updateAt)
                continue;

            link.UpdateStateAt = null;
            Dirty(uid, link);
            UpdateLinkState((uid, link));
        }
    }
}
