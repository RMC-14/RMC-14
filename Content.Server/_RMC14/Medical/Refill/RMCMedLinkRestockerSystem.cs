using Content.Server.GameTicking;
using Content.Shared._RMC14.Medical.Refill;
using Content.Shared._RMC14.Vendors;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.Medical.Refill;

public sealed class RMCMedLinkRestockerSystem : Shared._RMC14.Medical.Refill.RMCMedLinkRestockerSystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedCMAutomatedVendorSystem _vendor = default!;

    private TimeSpan _nextRestock = TimeSpan.Zero;
    private const float RestockInterval = 30f; // 30 Seconds PROCESSING_SUBSYSTEM_DEF(slowobj)

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        if (curTime < _nextRestock)
            return;

        _nextRestock = curTime + TimeSpan.FromSeconds(RestockInterval);

        var roundDuration = _gameTicker.RoundDuration();
        var vendors = EntityQueryEnumerator<RMCMedLinkRestockerComponent, CMAutomatedVendorComponent, TransformComponent>();
        while (vendors.MoveNext(out var uid, out var restocker, out var vendor, out var xform))
        {
            if (!restocker.AllowSupplyLinkRestock)
                continue;
            if (!xform.Anchored)
                continue;
            if (roundDuration.TotalMinutes < restocker.RestockMinimumRoundTime)
                continue;
            if (!TryGetSupplyLink((uid, restocker)))
                continue;

            RestockVendorItems((uid, vendor));
        }
    }

    private void RestockVendorItems(Entity<CMAutomatedVendorComponent> vendor)
    {
        var changed = false;
        const float restockChance = 0.2f; // 20% chance to restock each item per check

        foreach (var section in vendor.Comp.Sections)
        {
            foreach (var entry in section.Entries)
            {
                if (entry.Max is not { } max || entry.Amount >= max)
                    continue;
                if (entry.Box != null)
                    continue;
                if (!_random.Prob(restockChance))
                    continue;

                entry.Amount++;
                changed = true;
                _vendor.AmountUpdated(vendor, entry);
            }
        }

        if (changed)
            Dirty(vendor);
    }
}
