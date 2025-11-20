using Content.Shared._RMC14.Animations;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Vendors;
using Content.Shared.Examine;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Medical.Refill;

public abstract class RMCMedicalSupplyLinkSystem : EntitySystem
{
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly SharedRMCAnimationSystem _animation = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CMMedicalSupplyLinkComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<CMAutomatedVendorComponent, AnchorStateChangedEvent>(OnVendorAnchorChanged);
        SubscribeLocalEvent<RMCMedLinkPortReceiverComponent, ExaminedEvent>(OnVendorRefillerExamined);
    }

    private void OnMapInit(Entity<CMMedicalSupplyLinkComponent> ent, ref MapInitEvent args)
    {
        UpdateLinkState(ent, playAnimation: false);
    }

    private void OnVendorRefillerExamined(Entity<RMCMedLinkPortReceiverComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(RMCMedLinkPortReceiverComponent)))
        {
            if (!TryComp(ent.Owner, out TransformComponent? xform))
                return;

            if (TryGetSupplyLink(ent.Owner, ent.Comp, xform))
            {
                args.PushMarkup(Loc.GetString("rmc-vending-machine-supply-link-connected"));
            }
        }
    }

    protected bool TryGetSupplyLink(EntityUid vendor, RMCMedLinkPortReceiverComponent portReceiver, TransformComponent xform)
    {
        if (!portReceiver.AllowSupplyLinkRestock)
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

    private void OnVendorAnchorChanged(Entity<CMAutomatedVendorComponent> vendor, ref AnchorStateChangedEvent args)
    {
        var anchored = _rmcMap.GetAnchoredEntitiesEnumerator(vendor);
        while (anchored.MoveNext(out var anchoredId))
        {
            if (!TryComp<CMMedicalSupplyLinkComponent>(anchoredId, out var link))
                continue;

            UpdateLinkState((anchoredId, link), args.Anchored, playAnimation: true);
            return;
        }
    }

    private void UpdateLinkState(Entity<CMMedicalSupplyLinkComponent> link, bool? portConnected = null, bool playAnimation = true)
    {
        if (portConnected == null)
        {
            var found = false;
            var anchored = _rmcMap.GetAnchoredEntitiesEnumerator(link);
            while (anchored.MoveNext(out var anchoredId))
            {
                if (!HasComp<CMAutomatedVendorComponent>(anchoredId))
                    continue;
                found = true;
                break;
            }
            portConnected = found;
        }

        var previousState = link.Comp.PortConnected;
        link.Comp.PortConnected = portConnected.Value;

        if (playAnimation && previousState != portConnected.Value)
        {
            var animationState = portConnected.Value
                ? $"{link.Comp.BaseState}_clamping"
                : $"{link.Comp.BaseState}_unclamping";
            var finalState = portConnected.Value
                ? $"{link.Comp.BaseState}_clamped"
                : $"{link.Comp.BaseState}_unclamped";

            var animationRsi = new SpriteSpecifier.Rsi(
                new ResPath("_RMC14/Structures/Machines/Medical/medilink.rsi"),
                animationState);
            var defaultRsi = new SpriteSpecifier.Rsi(
                new ResPath("_RMC14/Structures/Machines/Medical/medilink.rsi"),
                finalState);

            _animation.Flick((link.Owner, null), animationRsi, defaultRsi, "base");
        }

        Dirty(link);
    }
}
