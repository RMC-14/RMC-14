using Content.Server._RMC14.RMCMedicalRecords;
using Content.Shared._RMC14.Medical.BodyScanner;
using Content.Shared._RMC14.RMCMedicalRecords;
using Robust.Server.Player;

namespace Content.Server._RMC14.Medical.BodyScanner;

public sealed class BodyScannerSystem : SharedBodyScannerSystem
{
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly RMCMedicalRecordsSystem _rmcMedicalRecords = default!;

    private readonly HashSet<EntityUid> _intersecting = [];

    protected override void OnConsoleScan(Entity<BodyScannerConsoleComponent> console, EntityUid occupant, EntityUid user)
    {
        _rmcMedicalRecords.UpdateMedicalRecordFromScan(occupant, console.Comp.DetailLevel);

        if (_player.TryGetSessionByEntity(user, out var session))
            RaiseNetworkEvent(new OpenStoredScanEvent(GetNetEntity(occupant)), session);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<OutsideBodyScannerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Chamber is not { } chamber)
            {
                RemCompDeferred<OutsideBodyScannerComponent>(uid);
                continue;
            }

            _intersecting.Clear();
            _entityLookup.GetEntitiesIntersecting(uid, _intersecting);

            if (!_intersecting.Contains(chamber))
                RemCompDeferred<OutsideBodyScannerComponent>(uid);
        }
    }
}
