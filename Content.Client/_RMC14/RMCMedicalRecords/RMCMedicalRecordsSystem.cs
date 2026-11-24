using Content.Client._RMC14.Medical.Scanner;
using Content.Shared._RMC14.RMCMedicalRecords;

namespace Content.Client._RMC14.RMCMedicalRecords;

public sealed class RMCMedicalRecordsSystem : SharedRMCMedicalRecordsSystem
{
    [ViewVariables]
    private HealthScannerWindow? _scanWindow;
    private HealthScannerUiData? _scanUiData;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeAllEvent<OpenStoredScanEvent>(OnOpenStoredScan);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _scanWindow?.Close();
        _scanWindow = null;
        _scanUiData = null;
    }

    private void OnOpenStoredScan(OpenStoredScanEvent ev)
    {
        var target = GetEntity(ev.Target);
        if (!TryGetMedicalRecord(target, out var record) || record.LastScanState is not { } scanState)
            return;

        _scanUiData ??= new HealthScannerUiData();

        if (_scanWindow == null)
        {
            _scanWindow = new HealthScannerWindow();
            _scanWindow.Title = Loc.GetString("rmc-health-analyzer-title");
        }

        if (!_scanWindow.IsOpen)
            _scanWindow.OpenCentered();

        _scanUiData.PopulateHealthScan(_scanWindow, scanState);
    }
}
