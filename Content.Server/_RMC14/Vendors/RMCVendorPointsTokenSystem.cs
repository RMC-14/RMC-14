using Content.Shared._RMC14.Vendors;
using Content.Shared.Popups;
using Robust.Server.GameObjects;

namespace Content.Server._RMC14.Vendors;

/// <summary>
/// Redeems vendor point tokens into the user's point account for a matching automated vendor.
/// </summary>
public sealed class RMCVendorPointsTokenSystem : EntitySystem
{
    [Dependency] private readonly CMAutomatedVendorSystem _vendor = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CMAutomatedVendorComponent, RMCVendorPointsTokenInteractEvent>(OnVendorInteractUsing);
    }

    private void OnVendorInteractUsing(Entity<CMAutomatedVendorComponent> vendor, ref RMCVendorPointsTokenInteractEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp(args.Used, out RMCVendorPointsTokenComponent? token))
            return;

        if (vendor.Comp.PointsType != token.PointsType)
        {
            // Tokens are intentionally bound to one point pool so they cannot be spent at unrelated vendors.
            _popup.PopupEntity(Loc.GetString("rmc-vendor-points-token-wrong-vendor", ("token", args.Used), ("vendor", vendor)), vendor, args.User);
            return;
        }

        // Store redeemed points on the user so the existing CMAutomatedVendor UI and purchase flow can handle them.
        var user = EnsureComp<CMVendorUserComponent>(args.User);
        var current = user.ExtraPoints?.GetValueOrDefault(token.PointsType) ?? 0;
        _vendor.SetExtraPoints((args.User, user), token.PointsType, current + token.Points);

        _popup.PopupEntity(Loc.GetString("rmc-vendor-points-token-redeem", ("token", args.Used), ("vendor", vendor), ("points", token.Points)), vendor, args.User);
        _ui.ServerSendUiMessage(vendor.Owner, CMAutomatedVendorUI.Key, new CMVendorRefreshBuiMsg(), args.User);
        QueueDel(args.Used);
        args.Handled = true;
    }
}
