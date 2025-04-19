using System.Linq;
using Content.Client.Message;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Power;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;

namespace Content.Client._RMC14.Power;

[UsedImplicitly]
public sealed class RMCApcBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private readonly IConfigurationManager _config = default!;

    private static readonly Color BlueBackgroundColor = Color.FromHex("#3E6189");
    private static readonly Color GreenBackgroundColor = Color.FromHex("#1B9638");
    private static readonly Color GreenColor = Color.FromHex("#5AC229");
    private static readonly Color OrangeColor = Color.FromHex("#C99A29");
    private static readonly Color RedColor = Color.FromHex("#CE3E31");

    [ViewVariables]
    private RMCApcWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<RMCApcWindow>();

        _window.CoverButton.OnPressed += _ => SendPredictedMessage(new RMCApcCoverBuiMsg());

        foreach (var channel in Enum.GetValues<RMCPowerChannel>())
        {
            var row = new RMCApcChannelRow();
            row.Label.SetMarkupPermissive($"[color=#5B88B0]{channel}:[/color]");
            _window.Channels.AddChild(row);
        }

        Refresh();
    }

    public void Refresh()
    {
        if (_window is not { IsOpen: true })
            return;

        if (!EntMan.TryGetComponent(Owner, out RMCApcComponent? apc))
            return;

        var lockedMsg = apc.Locked
            ? "[italic]Swipe an ID card or dogtags to unlock this interface.[/italic]"
            : "[italic]Swipe an ID card or dogtags to lock this interface.[/italic]";
        _window.LockedLabel.SetMarkupPermissive(lockedMsg);

        _window.PowerStatusLabel.SetMarkupPermissive(Header("Power Status"));
        _window.PowerChannelsLabel.SetMarkupPermissive(Header("Power Channels"));
        _window.MiscLabel.SetMarkupPermissive(Header("Misc"));

        _window.MainBreakerButton.Text = apc.MainBreakerButton ? "On" : "Off";
        if (apc.MainBreakerButton)
        {
            _window.MainBreakerButton.Text = "On";
            _window.MainBreakerButton.Pressed = true;
        }
        else
        {
            _window.MainBreakerButton.Text = "Off";
            _window.MainBreakerButton.Pressed = false;
        }

        _window.MainBreakerStatus.SetMarkupPermissive(apc.ExternalPower
            ? Green("[ External Power ]")
            : Red("[ No External Power ]")
        );

        _window.PowerBar.MinValue = 0;
        _window.PowerBar.MaxValue = 1;
        _window.PowerBar.Value = apc.ChargePercentage;
        _window.PowerBarLabel.Text = $"{apc.ChargePercentage * 100:F0}%";

        var chargeMode = apc.ChargeStatus switch
        {
            RMCApcChargeStatus.NotCharging => Red("[ Not Charging ]"),
            RMCApcChargeStatus.Charging => Orange("[ Charging ]"),
            RMCApcChargeStatus.FullCharge => Green("[ Fully Charged ]"),
            _ => throw new ArgumentOutOfRangeException(),
        };

        _window.ChargeMode.SetMarkupPermissive(chargeMode);
        _window.ChargeModeButton.Text = apc.ChargeModeButton ? "Auto" : "Off";

        foreach (int channel in Enum.GetValues<RMCPowerChannel>())
        {
            var row = (RMCApcChannelRow) _window.Channels.GetChild(channel);
            SetButtons(row, apc.Channels[channel]);
            row.Auto.OnPressed += _ => SendPredictedMessage(new RMCApcSetChannelBuiMsg((RMCPowerChannel) channel, RMCApcButtonState.Auto));
            // row.Off.OnPressed += _ => SendPredictedMessage(new RMCApcSetChannelBuiMsg((RMCPowerChannel) channel, RMCApcButtonState.Off));
            row.Off.Visible = false;
        }

        var multiplier = _config.GetCVar(RMCCVars.RMCPowerLoadMultiplier);
        var totalWatts = apc.Channels.Sum(c => c.Watts);
        _window.TotalLoadWatts.SetMarkupPermissive($"[bold]{totalWatts / multiplier} W[/bold]");

        _window.CoverButton.Text = apc.CoverLockedButton ? "Engaged" : "Disengaged";
        _window.CoverButton.Disabled = apc.Locked;
    }

    private string Header(string header)
    {
        return $"[bold]{header}[/bold]";
    }

    private string Green(string str)
    {
        return $"[color={GreenColor.ToHex()}]{str}[/color]";
    }

    private string Orange(string str)
    {
        return $"[color={OrangeColor.ToHex()}]{str}[/color]";
    }

    private string Red(string str)
    {
        return $"[color={RedColor.ToHex()}]{str}[/color]";
    }

    private void SetButtons(RMCApcChannelRow row, RMCApcChannel channel)
    {
        var multiplier = _config.GetCVar(RMCCVars.RMCPowerLoadMultiplier);
        row.Auto.Pressed = channel.Button == RMCApcButtonState.Auto;
        row.On.Pressed = channel.Button == RMCApcButtonState.On;
        row.On.Visible = false; // TODO RMC14
        row.Off.Pressed = channel.Button == RMCApcButtonState.Off;
        row.Watts.SetMarkupPermissive($"{channel.Watts / multiplier} W");
        row.Status.SetMarkupPermissive(channel.On ? $"{Green("On")}" : $"{Red("Off")}");
    }
}
