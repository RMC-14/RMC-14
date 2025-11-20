using Content.Server.GameTicking;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Medical.Refill;
using Content.Shared._RMC14.Vendors;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Medical.Refill;

public sealed class RMCMedicalSupplyLinkSystem : Shared._RMC14.Medical.Refill.RMCMedicalSupplyLinkSystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedCMAutomatedVendorSystem _vendor = default!;

    private TimeSpan _nextRestock = TimeSpan.Zero;
    private const float RestockInterval = 30f; // Restock every 30 seconds PROCESSING_SUBSYSTEM_DEF(slowobj)

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        if (curTime < _nextRestock)
            return;

        _nextRestock = curTime + TimeSpan.FromSeconds(RestockInterval);

        var roundDuration = _gameTicker.RoundDuration();
        var links = EntityQueryEnumerator<CMMedicalSupplyLinkComponent, TransformComponent>();
        while (links.MoveNext(out var linkUid, out _, out var linkXform))
        {
            if (!linkXform.Anchored)
                continue;

            var anchored = _rmcMap.GetAnchoredEntitiesEnumerator(linkUid);
            while (anchored.MoveNext(out var anchoredId))
            {
                if (!TryComp<CMAutomatedVendorComponent>(anchoredId, out var vendorComp))
                    continue;

                if (!TryComp<RMCMedLinkPortReceiverComponent>(anchoredId, out var restocker))
                    continue;

                if (!restocker.AllowSupplyLinkRestock)
                    continue;

                if (roundDuration.TotalMinutes < restocker.RestockMinimumRoundTime)
                    continue;

                RestockVendorItems((anchoredId, vendorComp));
            }
        }
    }

    private void RestockVendorItems(Entity<CMAutomatedVendorComponent> vendor)
    {
        var restocked = false;
        const float restockChance = 0.2f; // 20% chance to restock each item per check

        foreach (var section in vendor.Comp.Sections)
        {
            foreach (var entry in section.Entries)
            {
                if (entry.Max is not { } max || entry.Amount >= max)
                    continue;

                if (!_random.Prob(restockChance))
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
