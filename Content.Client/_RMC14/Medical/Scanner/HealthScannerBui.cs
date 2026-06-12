using Content.Shared._RMC14.Medical.Scanner;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._RMC14.Medical.Scanner;

[UsedImplicitly]
public sealed class HealthScannerBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private HealthScannerWindow? _window;
    private HealthScannerUiData? _scanUiData;

    protected override void Open()
    {
        base.Open();
        _scanUiData ??= new HealthScannerUiData();

        if (State is HealthScannerBuiState state)
            UpdateState(state);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is HealthScannerBuiState uiState)
            UpdateState(uiState);
    }

    private void UpdateState(HealthScannerBuiState uiState)
    {
        _scanUiData ??= new HealthScannerUiData();

        if (_window == null)
        {
            _window = this.CreateWindow<HealthScannerWindow>();
            _window.Title = Loc.GetString("rmc-health-analyzer-title");
        }

        _scanUiData.PopulateHealthScan(_window, uiState.ScanState);
    }
}
