using Content.Shared._RMC14.Power;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.Power;

[UsedImplicitly]
public sealed class RMCPowerMonitorBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private static readonly Color BlueColor = Color.FromHex("#7FAAD1");
    private static readonly Color GreenColor = Color.FromHex("#5AC229");
    private static readonly Color OrangeColor = Color.FromHex("#C99A29");
    private static readonly Color RedColor = Color.FromHex("#CE3E31");
    private static readonly Color InactiveColor = Color.FromHex("#9A9A9A");

    [ViewVariables]
    private RMCPowerMonitorWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<RMCPowerMonitorWindow>();
        Refresh();
    }

    public void Refresh()
    {
        if (_window is not { IsOpen: true } ||
            !EntMan.TryGetComponent(Owner, out RMCPowerMonitorComponent? monitor))
        {
            return;
        }

        _window.Title = Loc.GetString("rmc-power-monitor-title");
        if (!monitor.Connected)
        {
            SetDisconnected();
            return;
        }

        _window.Connection.Text = Loc.GetString("rmc-power-monitor-network", ("network", monitor.PowerNet));
        _window.ConnectionState.Text = Loc.GetString("rmc-power-monitor-connected");
        _window.ConnectionState.FontColorOverride = GreenColor;
        SetMetric(_window.AvailableValue, monitor.Stats.AvailableGeneration, BlueColor);
        SetMetric(_window.GenerationValue, monitor.Stats.Generation, GreenColor);
        SetMetric(_window.DemandValue, monitor.Stats.Demand, Color.White);
        SetMetric(_window.DeliveredValue,
            monitor.Stats.Delivered,
            monitor.Stats.Delivered + 0.01f >= monitor.Stats.Demand ? GreenColor : RedColor);
        SetMetric(_window.DeficitValue,
            monitor.Stats.Deficit,
            monitor.Stats.Deficit > 0.01f ? RedColor : GreenColor);
        SetMetric(_window.SurplusValue,
            monitor.Stats.Surplus,
            monitor.Stats.Surplus > 0.01f ? BlueColor : InactiveColor);

        UpdateApcs(monitor.Apcs);
        UpdateStorages(monitor.Storages);
    }

    private void SetDisconnected()
    {
        if (_window == null)
            return;

        _window.Connection.Text = Loc.GetString("rmc-power-monitor-network-unavailable");
        _window.ConnectionState.Text = Loc.GetString("rmc-power-monitor-disconnected");
        _window.ConnectionState.FontColorOverride = RedColor;
        SetEmptyMetric(_window.AvailableValue);
        SetEmptyMetric(_window.GenerationValue);
        SetEmptyMetric(_window.DemandValue);
        SetEmptyMetric(_window.DeliveredValue);
        SetEmptyMetric(_window.DeficitValue);
        SetEmptyMetric(_window.SurplusValue);
        UpdateApcs([]);
        UpdateStorages([]);
    }

    private void UpdateApcs(RMCPowerMonitorApc[] apcs)
    {
        if (_window == null)
            return;

        SetApcRowCount(_window.ApcRows, apcs.Length);
        for (var i = 0; i < apcs.Length; i++)
        {
            var apc = apcs[i];
            var row = (RMCPowerMonitorApcRow) _window.ApcRows.GetChild(i);
            row.Area.Text = apc.Area;
            SetChannelState(row.Equipment, apc.Equipment);
            SetChannelState(row.Lighting, apc.Lighting);
            SetChannelState(row.Environment, apc.Environment);
            row.Requested.Text = Power(apc.Requested);
            row.Delivered.Text = Power(apc.Delivered);
            row.Delivered.FontColorOverride = apc.Delivered + 0.01f >= apc.Requested
                ? GreenColor
                : RedColor;

            SetApcCell(row, apc);
        }

        _window.ApcEmpty.Visible = apcs.Length == 0;
        _window.ApcScroll.Visible = apcs.Length > 0;
        TabContainer.SetTabTitle(_window.ApcTab,
            Loc.GetString("rmc-power-monitor-apcs-count", ("count", apcs.Length)));
    }

    private void UpdateStorages(RMCPowerMonitorStorage[] storages)
    {
        if (_window == null)
            return;

        SetStorageRowCount(_window.StorageRows, storages.Length);
        for (var i = 0; i < storages.Length; i++)
        {
            var storage = storages[i];
            var row = (RMCPowerMonitorStorageRow) _window.StorageRows.GetChild(i);
            row.StorageName.Text = storages.Length > 1
                ? Loc.GetString("rmc-power-monitor-storage-numbered", ("name", storage.Name), ("number", i + 1))
                : storage.Name;

            var charge = storage.MaxCharge <= 0
                ? 0
                : Math.Clamp(storage.Charge / storage.MaxCharge, 0, 1);
            row.ChargeBar.Value = charge;
            row.ChargeLabel.Text = Loc.GetString("rmc-power-monitor-storage-charge",
                ("charge", Number(storage.Charge / 1_000_000)),
                ("maxCharge", Number(storage.MaxCharge / 1_000_000)),
                ("percent", Number(charge * 100)));
            row.ChargeLabel.FontColorOverride = ChargeColor(charge);

            SetStorageState(row, storage);
            SetStorageInput(row, storage);
            SetStorageOutput(row, storage);
        }

        _window.StorageEmpty.Visible = storages.Length == 0;
        _window.StorageScroll.Visible = storages.Length > 0;
        TabContainer.SetTabTitle(_window.StorageTab,
            Loc.GetString("rmc-power-monitor-storage-count", ("count", storages.Length)));
    }

    private void SetStorageState(RMCPowerMonitorStorageRow row, RMCPowerMonitorStorage storage)
    {
        if (storage.Output > 0.01f)
        {
            row.StorageState.Text = Loc.GetString("rmc-power-monitor-discharging");
            row.StorageState.FontColorOverride = OrangeColor;
        }
        else if (storage.Input > 0.01f)
        {
            row.StorageState.Text = Loc.GetString("rmc-power-monitor-charging");
            row.StorageState.FontColorOverride = GreenColor;
        }
        else
        {
            row.StorageState.Text = Loc.GetString("rmc-power-monitor-standby");
            row.StorageState.FontColorOverride = InactiveColor;
        }
    }

    private void SetStorageInput(RMCPowerMonitorStorageRow row, RMCPowerMonitorStorage storage)
    {
        var state = !storage.InputEnabled
            ? "rmc-power-monitor-disabled"
            : storage.InputState switch
            {
                RMCPowerStorageInputState.Full => "rmc-power-monitor-full",
                RMCPowerStorageInputState.Partial => "rmc-power-monitor-partial",
                _ => "rmc-power-monitor-idle",
            };
        row.InputState.Text = Loc.GetString(state);
        row.InputState.FontColorOverride = !storage.InputEnabled
            ? RedColor
            : storage.InputState == RMCPowerStorageInputState.Full
                ? GreenColor
                : storage.InputState == RMCPowerStorageInputState.Partial
                    ? OrangeColor
                    : InactiveColor;
        row.InputPower.Text = Loc.GetString("rmc-power-monitor-flow",
            ("actual", Power(storage.Input)),
            ("limit", Power(storage.InputLimit)));
    }

    private void SetStorageOutput(RMCPowerMonitorStorageRow row, RMCPowerMonitorStorage storage)
    {
        row.OutputState.Text = Loc.GetString(storage.OutputEnabled
            ? "rmc-power-monitor-enabled"
            : "rmc-power-monitor-disabled");
        row.OutputState.FontColorOverride = storage.OutputEnabled ? GreenColor : RedColor;
        row.OutputPower.Text = Loc.GetString("rmc-power-monitor-flow",
            ("actual", Power(storage.Output)),
            ("limit", Power(storage.OutputLimit)));
    }

    private void SetChannelState(Label label, RMCApcChannelVisualState state)
    {
        var (locId, color) = state switch
        {
            RMCApcChannelVisualState.ManualOff => ("rmc-power-monitor-off", RedColor),
            RMCApcChannelVisualState.AutoOff => ("rmc-power-monitor-auto-off", OrangeColor),
            RMCApcChannelVisualState.ManualOn => ("rmc-power-monitor-on", GreenColor),
            RMCApcChannelVisualState.AutoOn => ("rmc-power-monitor-auto-on", BlueColor),
            _ => throw new ArgumentOutOfRangeException(nameof(state), state, null),
        };
        label.Text = Loc.GetString(locId);
        label.FontColorOverride = color;
        label.ToolTip = Loc.GetString("rmc-power-monitor-channel-tooltip");
    }

    private void SetApcCell(RMCPowerMonitorApcRow row, RMCPowerMonitorApc apc)
    {
        if (!apc.HasCell)
        {
            row.CellBar.Value = 0;
            row.CellLabel.Text = Loc.GetString("rmc-power-monitor-no-cell");
            row.CellLabel.FontColorOverride = RedColor;
            row.CellBar.ToolTip = Loc.GetString("rmc-power-monitor-no-cell-tooltip");
            return;
        }

        var charge = Math.Clamp(apc.CellCharge, 0, 1);
        var (statusLocId, statusColor) = apc.ChargeStatus switch
        {
            RMCApcChargeStatus.NotCharging => ("rmc-power-monitor-cell-not-charging", ChargeColor(charge)),
            RMCApcChargeStatus.Charging => ("rmc-power-monitor-cell-charging", BlueColor),
            RMCApcChargeStatus.FullCharge => ("rmc-power-monitor-cell-full", GreenColor),
            _ => throw new ArgumentOutOfRangeException(nameof(apc.ChargeStatus), apc.ChargeStatus, null),
        };
        row.CellBar.Value = charge;
        row.CellLabel.Text = Loc.GetString("rmc-power-monitor-cell-state",
            ("percent", Number(charge * 100)),
            ("state", Loc.GetString(statusLocId)));
        row.CellLabel.FontColorOverride = statusColor;
        row.CellBar.ToolTip = Loc.GetString("rmc-power-monitor-cell-tooltip",
            ("state", Loc.GetString(statusLocId)));
    }

    private static void SetApcRowCount(Control container, int count)
    {
        TrimRows(container, count);
        while (container.ChildCount < count)
            container.AddChild(new RMCPowerMonitorApcRow());
    }

    private static void SetStorageRowCount(Control container, int count)
    {
        TrimRows(container, count);
        while (container.ChildCount < count)
            container.AddChild(new RMCPowerMonitorStorageRow());
    }

    private static void TrimRows(Control container, int count)
    {
        while (container.ChildCount > count)
            container.RemoveChild(container.GetChild(container.ChildCount - 1));
    }

    private static void SetMetric(Label label, float watts, Color color)
    {
        label.Text = Power(watts);
        label.FontColorOverride = color;
    }

    private static void SetEmptyMetric(Label label)
    {
        label.Text = "—";
        label.FontColorOverride = InactiveColor;
    }

    private static Color ChargeColor(float charge)
    {
        return charge switch
        {
            >= 0.5f => GreenColor,
            >= 0.25f => OrangeColor,
            _ => RedColor,
        };
    }

    private static string Power(float watts)
    {
        return $"{Number(watts / 1000)} kW";
    }

    private static string Number(float value)
    {
        return value.ToString("0.#");
    }
}
