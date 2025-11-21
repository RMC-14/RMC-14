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

    private const float RestockIntervalSeconds = 30f; // PROCESSING_SUBSYSTEM_DEF(slowobj)
    private TimeSpan _nextRestock = TimeSpan.Zero;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        if (curTime < _nextRestock)
            return;

        _nextRestock = curTime + TimeSpan.FromSeconds(RestockIntervalSeconds);

        var roundDuration = _gameTicker.RoundDuration();
        var links = EntityQueryEnumerator<CMMedicalSupplyLinkComponent, TransformComponent>();
        while (links.MoveNext(out var linkUid, out var linkComp, out var linkXform))
        {
            if (!linkXform.Anchored)
                continue;

            ProcessLinkedRestocking((linkUid, linkComp), roundDuration);
        }
    }

    private void ProcessLinkedRestocking(Entity<CMMedicalSupplyLinkComponent> link, TimeSpan roundDuration)
    {
        var anchored = _rmcMap.GetAnchoredEntitiesEnumerator(link);

        while (anchored.MoveNext(out var anchoredId))
        {
            if (!TryComp<CMAutomatedVendorComponent>(anchoredId, out var vendorComp))
                continue;

            if (!TryComp<RMCMedLinkPortReceiverComponent>(anchoredId, out var portReceiver))
                continue;

            if (!portReceiver.AllowSupplyLinkRestock)
                continue;

            if (roundDuration.TotalMinutes < portReceiver.RestockMinimumRoundTime)
                continue;

            RestockVendorItems((anchoredId, vendorComp), portReceiver);
        }
    }

    private void RestockVendorItems(Entity<CMAutomatedVendorComponent> vendor, RMCMedLinkPortReceiverComponent portReceiver)
    {
        var restocked = false;
        foreach (var section in vendor.Comp.Sections)
        {
            foreach (var entry in section.Entries)
            {
                if (entry.Max is not { } max || entry.Amount >= max)
                    continue;

                if (!_random.Prob(portReceiver.RestockChancePerItem))
                    continue;

                entry.Amount++;
                restocked = true;
                _vendor.AmountUpdated(vendor, entry);
            }
        }

        if (restocked)
            Dirty(vendor);
    }
}
