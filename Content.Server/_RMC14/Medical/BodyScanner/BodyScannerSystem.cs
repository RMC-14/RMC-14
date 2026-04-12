using Content.Server._RMC14.RMCMedicalRecords;
using Content.Shared._RMC14.Medical.BodyScanner;
using Content.Shared._RMC14.RMCMedicalRecords;
using Robust.Server.Player;

namespace Content.Server._RMC14.Medical.BodyScanner;

public sealed class BodyScannerSystem : SharedBodyScannerSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly RMCMedicalRecordsSystem _rmcMedicalRecords = default!;

    protected override void OnConsoleScan(Entity<BodyScannerConsoleComponent> console, EntityUid occupant, EntityUid user)
    {
        _rmcMedicalRecords.UpdateMedicalRecordFromScan(occupant, console.Comp.DetailLevel);

        if (_player.TryGetSessionByEntity(user, out var session))
            RaiseNetworkEvent(new OpenStoredScanEvent(GetNetEntity(occupant)), session);
    }
}
