using Content.Server.GameTicking;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Medical.Refill;
using Content.Shared._RMC14.Vendors;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Medical.Refill;

public sealed class RMCMedicalSupplyLinkSystem : SharedMedicalSupplyLinkSystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedCMAutomatedVendorSystem _vendor = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var roundDuration = _gameTicker.RoundDuration();

        var vendors = EntityQueryEnumerator<RMCMedLinkPortReceiverComponent, CMAutomatedVendorComponent, TransformComponent>();
        while (vendors.MoveNext(out var vendorUid, out var portReceiver, out var vendorComp, out var vendorXform))
        {
            if (!portReceiver.AllowSupplyLinkRestock)
                continue;

            if (!vendorXform.Anchored)
                continue;

            if (curTime < portReceiver.NextRestock)
                continue;

            if (roundDuration.TotalMinutes < portReceiver.RestockMinimumRoundTime)
                continue;

            if (!IsConnectedToSupplyLink(vendorUid))
                continue;

            portReceiver.NextRestock = curTime + TimeSpan.FromSeconds(portReceiver.RestockIntervalSeconds);
            RestockVendorItems((vendorUid, vendorComp), portReceiver);
        }
    }

    private bool IsConnectedToSupplyLink(EntityUid vendor)
    {
        var anchored = _rmcMap.GetAnchoredEntitiesEnumerator(vendor);
        while (anchored.MoveNext(out var anchoredId))
        {
            if (HasComp<CMMedicalSupplyLinkComponent>(anchoredId))
                return true;
        }

        return false;
    }

    private void RestockVendorItems(Entity<CMAutomatedVendorComponent> vendor, RMCMedLinkPortReceiverComponent portReceiver)
    {
        var restocked = false;
        foreach (var section in vendor.Comp.Sections)
        {
            foreach (var entry in section.Entries)
            {
                if (entry.Max is not { } max)
                    continue;

                if (!_random.Prob(portReceiver.RestockChancePerItem))
                    continue;
                // When at max, try to complete any partial stacks instead of increasing amount
                if (entry.Amount >= max)
                {
                    if (_vendor.TryClearPartialStack(vendor, entry))
                        restocked = true;

                    continue;
                }

                entry.Amount++;
                restocked = true;
                _vendor.AmountUpdated(vendor, entry);
            }
        }

        if (restocked)
            Dirty(vendor);
    }
}
