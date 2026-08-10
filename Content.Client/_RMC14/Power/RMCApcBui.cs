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
        _window.MainBreakerButton.OnPressed += _ => SendPredictedMessage(new RMCApcMainBreakerBuiMsg());
        _window.ChargeModeButton.OnPressed += _ => SendPredictedMessage(new RMCApcChargeModeBuiMsg());

        foreach (var channel in Enum.GetValues<RMCPowerChannel>())
        {
            var row = new RMCApcChannelRow();
            row.Label.SetMarkupPermissive(Label(GetChannelNameId(channel)));
            row.Auto.Text = Loc.GetString("rmc-apc-ui-button-auto");
            row.On.Text = Loc.GetString("rmc-apc-ui-button-on");
            row.Off.Text = Loc.GetString("rmc-apc-ui-button-off");
            row.Auto.OnPressed += _ => SendPredictedMessage(new RMCApcSetChannelBuiMsg(channel, RMCApcButtonState.Auto));
            row.On.OnPressed += _ => SendPredictedMessage(new RMCApcSetChannelBuiMsg(channel, RMCApcButtonState.On));
            row.Off.OnPressed += _ => SendPredictedMessage(new RMCApcSetChannelBuiMsg(channel, RMCApcButtonState.Off));
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

        _window.LockedLabel.SetMarkupPermissive(Loc.GetString(apc.Locked
            ? "rmc-apc-ui-locked"
            : "rmc-apc-ui-unlocked"));

        if (apc.MainBreakerButton)
        {
            _window.MainBreakerButton.Text = Loc.GetString("rmc-apc-ui-button-on");
            _window.MainBreakerButton.Pressed = true;
        }
        else
        {
            _window.MainBreakerButton.Text = Loc.GetString("rmc-apc-ui-button-off");
            _window.MainBreakerButton.Pressed = false;
        }

        _window.MainBreakerStatus.SetMarkupPermissive(apc.ExternalPower
            ? Green(Loc.GetString("rmc-apc-ui-external-power"))
            : Red(Loc.GetString("rmc-apc-ui-no-external-power"))
        );

        _window.PowerBar.MinValue = 0;
        _window.PowerBar.MaxValue = 1;
        _window.PowerBar.Value = apc.ChargePercentage;
        _window.PowerBarLabel.Text = Loc.GetString("rmc-apc-ui-percent", ("percent", (int)MathF.Round(apc.ChargePercentage * 100)));

        var chargeMode = apc.ChargeStatus switch
        {
            RMCApcChargeStatus.NotCharging => Red(Loc.GetString("rmc-apc-ui-not-charging")),
            RMCApcChargeStatus.Charging => Orange(Loc.GetString("rmc-apc-ui-charging")),
            RMCApcChargeStatus.FullCharge => Green(Loc.GetString("rmc-apc-ui-fully-charged")),
            _ => throw new ArgumentOutOfRangeException(),
        };

        _window.ChargeMode.SetMarkupPermissive(chargeMode);
        _window.ChargeModeButton.Text = apc.ChargeModeButton
            ? Loc.GetString("rmc-apc-ui-button-auto")
            : Loc.GetString("rmc-apc-ui-button-off");

        foreach (int channel in Enum.GetValues<RMCPowerChannel>())
        {
            var row = (RMCApcChannelRow) _window.Channels.GetChild(channel);
            SetButtons(row, apc.Channels[channel]);
            row.Auto.Disabled = apc.Locked;
            row.On.Disabled = apc.Locked;
            row.Off.Disabled = apc.Locked;
        }

        var multiplier = _config.GetCVar(RMCCVars.RMCPowerLoadMultiplier);
        var totalWatts = apc.Channels.Sum(c => c.Watts);
        _window.TotalLoadWatts.SetMarkupPermissive(Loc.GetString("rmc-apc-ui-total-load-watts", ("watts", totalWatts / multiplier)));

        _window.CoverButton.Text = apc.CoverLockedButton
            ? Loc.GetString("rmc-apc-ui-cover-engaged")
            : Loc.GetString("rmc-apc-ui-cover-disengaged");
        _window.CoverButton.Disabled = apc.Locked;
        _window.MainBreakerButton.Disabled = apc.Locked;
        _window.ChargeModeButton.Disabled = apc.Locked;
    }

    private string Label(string locId)
    {
        return $"[color=#5B88B0]{Loc.GetString(locId)}[/color]";
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
        row.Off.Pressed = channel.Button == RMCApcButtonState.Off;
        row.Watts.SetMarkupPermissive(Loc.GetString("rmc-apc-ui-channel-watts", ("watts", channel.Watts / multiplier)));
        row.Status.SetMarkupPermissive(channel.On
            ? Green(Loc.GetString("rmc-apc-ui-button-on"))
            : Red(Loc.GetString("rmc-apc-ui-button-off")));
    }

    private string GetChannelNameId(RMCPowerChannel channel)
    {
        return channel switch
        {
            RMCPowerChannel.Equipment => "rmc-apc-ui-channel-equipment",
            RMCPowerChannel.Lighting => "rmc-apc-ui-channel-lighting",
            RMCPowerChannel.Environment => "rmc-apc-ui-channel-environment",
            _ => throw new ArgumentOutOfRangeException(nameof(channel), channel, null),
        };
    }
}
